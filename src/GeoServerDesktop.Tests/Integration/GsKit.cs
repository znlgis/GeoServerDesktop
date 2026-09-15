using System;
using System.Linq;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.Tests.Infrastructure;

namespace GeoServerDesktop.Tests.Integration
{
    /// <summary>
    /// 集成测试共用小工具：状态码断言、连接参数读取、best-effort 兜底清理。
    /// 清理一律走 CleanupTracker 的裸 HttpClient（独立于被测客户端）。
    /// </summary>
    public static class GsKit
    {
        /// <summary>异常是否为 GeoServerRequestException 且状态码匹配。</summary>
        public static bool IsRequest(Exception? e, int code) =>
            e is GeoServerRequestException gre && gre.StatusCode == code;

        /// <summary>异常是否为 Newtonsoft.Json 的解析异常族（JsonReaderException / JsonSerializationException 等）。</summary>
        public static bool IsJsonNet(Exception? e) => e is Newtonsoft.Json.JsonException;

        /// <summary>断言异常为 GeoServerRequestException 且状态码为期望值，返回该异常供进一步断言。</summary>
        public static GeoServerRequestException AssertRequest(Exception? e, int code, string what)
        {
            var gre = Assert.IsType<GeoServerRequestException>(e);
            Assert.True(gre.StatusCode == code,
                $"{what}: 期望状态码 {code}，实际 {gre.StatusCode}，响应体前 300 字符: {Trim(gre.ResponseContent)}");
            return gre;
        }

        public static string Trim(string? s) =>
            string.IsNullOrEmpty(s) ? "<null>" : (s!.Length <= 300 ? s : s.Substring(0, 300));

        /// <summary>读取存储连接参数值（key 与 GeoServer 返回一致，如 "url"/"host"/"database"）。</summary>
        public static string? GetParam(ConnectionParameters? cp, string key) =>
            cp?.Entries?.FirstOrDefault(x => x.Key == key)?.Value;

        /// <summary>登记 + 兜底删除工作空间（级联其下存储/图层/工作空间级样式与图层组）。吞异常（终局孤儿扫描负责）。</summary>
        public static async Task WipeWorkspaceAsync(GeoServerFixture fx, string workspace)
        {
            fx.Cleanup.TrackWorkspace(workspace);
            try { await fx.Cleanup.DeleteAsync("/rest/workspaces/" + Uri.EscapeDataString(workspace) + "?recurse=true"); }
            catch { /* best-effort */ }
        }

        /// <summary>兜底删除全局样式（被引用时会失败——先解绑再删）。吞异常。</summary>
        public static async Task WipeStyleAsync(GeoServerFixture fx, string style)
        {
            fx.Cleanup.TrackStyle(style);
            try { await fx.Cleanup.DeleteAsync("/rest/styles/" + Uri.EscapeDataString(style) + "?purge=true"); }
            catch { }
        }

        /// <summary>兜底删除全局图层组。吞异常。</summary>
        public static async Task WipeLayerGroupAsync(GeoServerFixture fx, string lg)
        {
            fx.Cleanup.TrackLayerGroup(lg);
            try { await fx.Cleanup.DeleteAsync("/rest/layergroups/" + Uri.EscapeDataString(lg)); }
            catch { }
        }

        /// <summary>兜底删除命名空间。吞异常。</summary>
        public static async Task WipeNamespaceAsync(GeoServerFixture fx, string prefix)
        {
            try { await fx.Cleanup.DeleteAsync("/rest/namespaces/" + Uri.EscapeDataString(prefix)); }
            catch { }
        }

        /// <summary>裸 GET 状态码（不经被测客户端），用于"资源已不存在(404)"之外的独立复核。</summary>
        public static Task<int?> RawStatusAsync(GeoServerFixture fx, string path) => fx.Cleanup.GetStatusCodeAsync(path);

        /// <summary>从 featureType 资源名得到 GeoServer 惯用的 qualified layer 名（ws:layer 在 layer 资源上即 name）。</summary>
        public static string LayerPath(string workspace, string layer) =>
            $"/rest/workspaces/{workspace}/layers/{layer}.json";

        // 注：曾有裸两步建样式的 RawCreateStyleAsync 绕行——因产品默认 Accept 单一 application/json 使
        // POST /rest/styles 500 "No such style handler"（E41）。产品已修复为 Accept: application/json,
        // text/plain;q=0.9（实测 201），StyleLayerIT 现直接走 CreateStyleAsync/CreateWorkspaceStyleAsync
        // 完整 CRUD，该绕行已随之移除。

        /// <summary>
        /// 构造可安全 PUT 的 FeatureType 请求体：实测 3.0.1 中 namespace/store 引用字段为 null 会触发
        /// XStream ReferenceConverter NPE（500 "NamespaceInfoImpl"），必须全部填实（href 用空串占位）。
        /// </summary>
        public static FeatureType FilledFt(string ws, string store, string ftName, string nativeName, string title, string srs)
            => new FeatureType
            {
                Name = ftName,
                NativeName = nativeName,
                Namespace = new NamespaceReference { Name = ws, Href = "" },
                Store = new StoreReference { Class = "dataStore", Name = ws + ":" + store, Href = "" },
                Title = title,
                Abstract = "",
                Keywords = new KeywordInfo { Keywords = Array.Empty<string>() },
                NativeCRS = "",
                Srs = srs,
                NativeBoundingBox = new BoundingBox { MinX = 0, MaxX = 1, MinY = 0, MaxY = 1, Crs = srs },
                LatLonBoundingBox = new BoundingBox { MinX = -180, MaxX = 180, MinY = -90, MaxY = 90, Crs = "EPSG:4326" },
                Enabled = true,
                Href = "",
            };

        /// <summary>同 FilledFt：Coverage 的 PUT 请求体（store @class=coverageStore）。</summary>
        public static Coverage FilledCov(string ws, string store, string covName, string nativeName, string title, string srs)
            => new Coverage
            {
                Name = covName,
                NativeName = nativeName,
                Namespace = new NamespaceReference { Name = ws, Href = "" },
                Store = new StoreReference { Class = "coverageStore", Name = ws + ":" + store, Href = "" },
                Title = title,
                Description = "",
                Abstract = "",
                Keywords = new KeywordInfo { Keywords = Array.Empty<string>() },
                NativeCRS = "",
                Srs = srs,
                NativeBoundingBox = new BoundingBox { MinX = 0, MaxX = 1, MinY = 0, MaxY = 1, Crs = srs },
                LatLonBoundingBox = new BoundingBox { MinX = -180, MaxX = 180, MinY = -90, MaxY = 90, Crs = "EPSG:4326" },
                Enabled = true,
                Href = "",
            };
    }
}
