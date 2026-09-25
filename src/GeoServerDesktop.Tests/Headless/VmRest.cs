using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using GeoServerDesktop.Tests.Infrastructure;

namespace GeoServerDesktop.Tests.Headless
{
    /// <summary>
    /// 独立裸 REST 助手：用 System.Net.Http + System.Text.Json 直接访问 GeoServer，
    /// 完全不经过被测 GeoServerClient 库，用于对 VM/服务层结果做交叉校验（独立解析）。
    /// 期望值必须来自服务器原始响应，禁止从被测代码回抄。
    /// </summary>
    internal static class VmRest
    {
        private static readonly HttpClient Http = Build();

        private static HttpClient Build()
        {
            var c = new HttpClient { Timeout = TimeSpan.FromSeconds(90) };
            c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.ASCII.GetBytes(TestEnv.User + ":" + TestEnv.Pass)));
            c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return c;
        }

        private static string Url(string path)
        {
            var b = TestEnv.RestBase.TrimEnd('/');
            return b + (path.StartsWith("/") ? path : "/" + path);
        }

        /// <summary>GET 原始响应体；出错返回 null。</summary>
        public static string? Get(string path)
        {
            try
            {
                using var resp = Http.GetAsync(Url(path)).GetAwaiter().GetResult();
                return resp.IsSuccessStatusCode ? resp.Content.ReadAsStringAsync().GetAwaiter().GetResult() : null;
            }
            catch { return null; }
        }

        /// <summary>GET 状态码；网络异常返回 0。</summary>
        public static int GetStatus(string path)
        {
            try
            {
                using var resp = Http.GetAsync(Url(path)).GetAwaiter().GetResult();
                return (int)resp.StatusCode;
            }
            catch { return 0; }
        }

        /// <summary>POST JSON，返回状态码。</summary>
        public static int PostJson(string path, string json)
        {
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var resp = Http.PostAsync(Url(path), content).GetAwaiter().GetResult();
            return (int)resp.StatusCode;
        }

        /// <summary>DELETE，返回状态码。</summary>
        public static int Delete(string path)
        {
            using var resp = Http.DeleteAsync(Url(path)).GetAwaiter().GetResult();
            return (int)resp.StatusCode;
        }

        /// <summary>
        /// 独立解析 version.json：取 @name=="GeoServer" 的资源，返回其 Version（字符串化，兼容字符串/数字两种形态）。
        /// </summary>
        public static string? GeoServerVersion(string versionJson)
        {
            if (string.IsNullOrEmpty(versionJson)) return null;
            using var doc = JsonDocument.Parse(versionJson);
            if (!doc.RootElement.TryGetProperty("about", out var about) ||
                !about.TryGetProperty("resource", out var resources))
                return null;
            foreach (var r in resources.EnumerateArray())
            {
                var name = r.TryGetProperty("@name", out var n) ? n.GetString() : null;
                if (name == "GeoServer" && r.TryGetProperty("Version", out var v))
                {
                    return v.ValueKind == JsonValueKind.String ? v.GetString() : v.GetRawText();
                }
            }
            return null;
        }

        /// <summary>独立统计全部工作空间数量（workspaces.workspace 数组长度；读取/解析失败返回 null）。</summary>
        public static int? WorkspaceCount()
        {
            var j = Get("/rest/workspaces.json");
            if (string.IsNullOrEmpty(j)) return null;
            using var doc = JsonDocument.Parse(j);
            return CountArray(doc.RootElement, "workspaces", "workspace");
        }

        /// <summary>独立统计全部图层数量（layers.layer 数组长度；读取/解析失败返回 null）。</summary>
        public static int? LayerCount()
        {
            var j = Get("/rest/layers.json");
            if (string.IsNullOrEmpty(j)) return null;
            using var doc = JsonDocument.Parse(j);
            return CountArray(doc.RootElement, "layers", "layer");
        }

        private static int? CountArray(JsonElement root, string outer, string inner)
        {
            if (!root.TryGetProperty(outer, out var o)) return null;
            if (!o.TryGetProperty(inner, out var arr) || arr.ValueKind != JsonValueKind.Array) return 0;
            return arr.GetArrayLength();
        }

        /// <summary>独立解析 WFS settings.json 的 maxFeatures（数字）。</summary>
        public static long? WfsMaxFeatures(string wfsJson)
        {
            if (string.IsNullOrEmpty(wfsJson)) return null;
            using var doc = JsonDocument.Parse(wfsJson);
            if (doc.RootElement.TryGetProperty("wfs", out var wfs) &&
                wfs.TryGetProperty("maxFeatures", out var mf) &&
                mf.ValueKind == JsonValueKind.Number && mf.TryGetInt64(out var val))
                return val;
            return null;
        }

        /// <summary>独立解析 WMS settings.json 的 name/enabled。</summary>
        public static (string? name, bool? enabled) WmsNameEnabled(string wmsJson)
        {
            if (string.IsNullOrEmpty(wmsJson)) return (null, null);
            using var doc = JsonDocument.Parse(wmsJson);
            if (!doc.RootElement.TryGetProperty("wms", out var wms)) return (null, null);
            string? name = wms.TryGetProperty("name", out var n) ? n.GetString() : null;
            bool? enabled = wms.TryGetProperty("enabled", out var e)
                ? (e.ValueKind == JsonValueKind.True ? true : e.ValueKind == JsonValueKind.False ? false : (bool?)null)
                : null;
            return (name, enabled);
        }

        /// <summary>工作空间是否存在（GET .json → 200）。</summary>
        public static bool WorkspaceExists(string name) =>
            GetStatus("/rest/workspaces/" + Uri.EscapeDataString(name) + ".json") == 200;

        /// <summary>样式是否存在。</summary>
        public static bool StyleExists(string name) =>
            GetStatus("/rest/styles/" + Uri.EscapeDataString(name) + ".json") == 200;

        /// <summary>用户是否存在（GeoServer 3.0.1 用户“详情 .json”端点 404，改查列表 users.json 是否含该 userName）。</summary>
        public static bool UserInList(string name)
        {
            var j = Get("/rest/security/usergroup/users.json") ?? "";
            return j.Contains("\"userName\":\"" + name + "\"") || j.Contains("\"userName\": \"" + name + "\"");
        }

        /// <summary>用户 enabled 状态：GeoServer 3.0.1 无可用“详情”端点，返回 null（无法独立校验，测试改为断言列表/状态语义）。</summary>
        public static bool? UserEnabled(string name) => null;

        /// <summary>工作空间图层是否存在。</summary>
        public static bool LayerExists(string ws, string layer) =>
            GetStatus($"/rest/workspaces/{Uri.EscapeDataString(ws)}/layers/{Uri.EscapeDataString(layer)}.json") == 200;

        /// <summary>独立解析图层详情 defaultStyle 的 name/href（读取/解析失败返回 (null, null)）。</summary>
        public static (string? name, string? href) LayerDefaultStyleRef(string ws, string layer)
        {
            var j = Get($"/rest/workspaces/{Uri.EscapeDataString(ws)}/layers/{Uri.EscapeDataString(layer)}.json");
            if (string.IsNullOrEmpty(j)) return (null, null);
            using var doc = JsonDocument.Parse(j);
            if (doc.RootElement.TryGetProperty("layer", out var l) && l.TryGetProperty("defaultStyle", out var ds))
            {
                var name = ds.TryGetProperty("name", out var n) ? n.GetString() : null;
                var href = ds.TryGetProperty("href", out var h) ? h.GetString() : null;
                return (name, href);
            }
            return (null, null);
        }

        /// <summary>要素类型（resource）是否存在。</summary>
        public static bool FeatureTypeExists(string ws, string store, string ft) =>
            GetStatus($"/rest/workspaces/{Uri.EscapeDataString(ws)}/datastores/{Uri.EscapeDataString(store)}/featuretypes/{Uri.EscapeDataString(ft)}.json") == 200;

        /// <summary>存储是否存在。</summary>
        public static bool DataStoreExists(string ws, string store) =>
            GetStatus($"/rest/workspaces/{Uri.EscapeDataString(ws)}/datastores/{Uri.EscapeDataString(store)}.json") == 200;

        /// <summary>独立解析存储的 type 字段（缺失即 null）。</summary>
        public static string? DataStoreType(string ws, string store)
        {
            var j = Get($"/rest/workspaces/{Uri.EscapeDataString(ws)}/datastores/{Uri.EscapeDataString(store)}.json");
            if (string.IsNullOrEmpty(j)) return "__ABSENT__";
            using var doc = JsonDocument.Parse(j);
            if (doc.RootElement.TryGetProperty("dataStore", out var ds) && ds.TryGetProperty("type", out var t) &&
                t.ValueKind == JsonValueKind.String)
                return t.GetString();
            return null; // 无 type 字段（E40 原状：namespace-only 存储被 201 接受但没有种类）
        }

        /// <summary>独立解析存储 GET JSON 的指定标量字符串字段（无则 null）。</summary>
        private static string? DataStoreStringField(string ws, string store, string field)
        {
            var j = Get($"/rest/workspaces/{Uri.EscapeDataString(ws)}/datastores/{Uri.EscapeDataString(store)}.json");
            if (string.IsNullOrEmpty(j)) return null;
            using var doc = JsonDocument.Parse(j);
            if (doc.RootElement.TryGetProperty("dataStore", out var ds) && ds.TryGetProperty(field, out var v) &&
                v.ValueKind == JsonValueKind.String)
                return v.GetString();
            return null;
        }

        /// <summary>独立解析存储的 description 字段。</summary>
        public static string? DataStoreDescription(string ws, string store) =>
            DataStoreStringField(ws, store, "description");

        /// <summary>独立解析存储 enabled 标志（GET 失败/缺失返回 null）。</summary>
        public static bool? DataStoreEnabled(string ws, string store)
        {
            var j = Get($"/rest/workspaces/{Uri.EscapeDataString(ws)}/datastores/{Uri.EscapeDataString(store)}.json");
            if (string.IsNullOrEmpty(j)) return null;
            using var doc = JsonDocument.Parse(j);
            if (doc.RootElement.TryGetProperty("dataStore", out var ds) && ds.TryGetProperty("enabled", out var e))
            {
                if (e.ValueKind == JsonValueKind.True) return true;
                if (e.ValueKind == JsonValueKind.False) return false;
            }
            return null;
        }

        /// <summary>
        /// 独立解析存储 connectionParameters 中指定 @key 的 entry 值（{"@key":k,"$":v} 形态）。
        /// </summary>
        public static string? DataStoreConnectionParam(string ws, string store, string key)
        {
            var j = Get($"/rest/workspaces/{Uri.EscapeDataString(ws)}/datastores/{Uri.EscapeDataString(store)}.json");
            if (string.IsNullOrEmpty(j)) return null;
            using var doc = JsonDocument.Parse(j);
            if (!doc.RootElement.TryGetProperty("dataStore", out var ds) ||
                !ds.TryGetProperty("connectionParameters", out var cp) ||
                !cp.TryGetProperty("entry", out var entries))
                return null;
            foreach (var en in entries.EnumerateArray())
            {
                var k = en.TryGetProperty("@key", out var kk) ? kk.GetString() : null;
                if (k == key && en.TryGetProperty("$", out var vv))
                {
                    return vv.ValueKind == JsonValueKind.String ? vv.GetString() : vv.GetRawText();
                }
            }
            return null;
        }
    }
}
