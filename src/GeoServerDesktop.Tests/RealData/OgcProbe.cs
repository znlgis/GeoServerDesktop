using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using GeoServerDesktop.Tests.Infrastructure;

namespace GeoServerDesktop.Tests.RealData
{
    /// <summary>
    /// 独立“curl 通道”：用裸 HttpClient 直取 OGC 服务与 REST，不经被测 GeoServerClient，
    /// 保证服务面校验与清理路径独立于被测代码。所有集成检查函数（RealDataChecks）走此通道。
    /// </summary>
    public static class OgcProbe
    {
        private static readonly HttpClient Http = Build();

        private static HttpClient Build()
        {
            var c = new HttpClient { Timeout = TimeSpan.FromSeconds(180) };
            // OGC 公共端点无需鉴权；GWC /rest 需要——统一带 Basic（无害）。
            c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.ASCII.GetBytes(TestEnv.User + ":" + TestEnv.Pass)));
            return c;
        }

        public readonly struct Resp
        {
            public int Status { get; }
            public byte[] Bytes { get; }
            public string ContentType { get; }
            public Resp(int s, byte[] b, string ct) { Status = s; Bytes = b; ContentType = ct; }
            public string Text => Bytes == null ? null : Encoding.UTF8.GetString(Bytes);
            public bool Ok => Status >= 200 && Status < 300;
        }

        public static Resp Get(string url)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                using var resp = Http.Send(req);
                var bytes = resp.Content != null ? resp.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult() : Array.Empty<byte>();
                var ct = resp.Content?.Headers?.ContentType?.MediaType ?? "";
                return new Resp((int)resp.StatusCode, bytes, ct);
            }
            catch (Exception ex)
            {
                return new Resp(0, Encoding.UTF8.GetBytes("EX:" + ex.Message), "");
            }
        }

        public static Resp Post(string url, string body, string contentType = "application/xml")
        {
            try
            {
                using var content = new StringContent(body ?? "", Encoding.UTF8, contentType);
                using var resp = Http.PostAsync(url, content).GetAwaiter().GetResult();
                var bytes = resp.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                var ct = resp.Content.Headers?.ContentType?.MediaType ?? "";
                return new Resp((int)resp.StatusCode, bytes, ct);
            }
            catch (Exception ex)
            {
                return new Resp(0, Encoding.UTF8.GetBytes("EX:" + ex.Message), "");
            }
        }

        public static Resp Put(string url, string body, string contentType = "application/xml")
        {
            try
            {
                using var content = new StringContent(body ?? "", Encoding.UTF8, contentType);
                using var req = new HttpRequestMessage(HttpMethod.Put, url) { Content = content };
                using var resp = Http.Send(req);
                var bytes = resp.Content?.ReadAsByteArrayAsync().GetAwaiter().GetResult() ?? Array.Empty<byte>();
                var ct = resp.Content?.Headers?.ContentType?.MediaType ?? "";
                return new Resp((int)resp.StatusCode, bytes, ct);
            }
            catch (Exception ex)
            {
                return new Resp(0, Encoding.UTF8.GetBytes("EX:" + ex.Message), "");
            }
        }

        public static Resp Delete(string url)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Delete, url);
                using var resp = Http.Send(req);
                var bytes = resp.Content?.ReadAsByteArrayAsync().GetAwaiter().GetResult() ?? Array.Empty<byte>();
                var ct = resp.Content?.Headers?.ContentType?.MediaType ?? "";
                return new Resp((int)resp.StatusCode, bytes, ct);
            }
            catch (Exception ex)
            {
                return new Resp(0, Encoding.UTF8.GetBytes("EX:" + ex.Message), "");
            }
        }

        // ---- URL 组装（OGC 端点公共，无鉴权）----
        // 实测（GeoServer 3.0.1）：已移除工作空间级 OGC 端点 /geoserver/{ws}/ows（GET 返回 JSON 404 "No endpoint"），
        // 仅全局 /geoserver/wfs、/wms、/wcs 可用（/{ws}/wms 等亦无）。故所有服务面请求走全局端点 +
        // typeName/layer 带 "ws:" 前缀限定。
        public static string Wfs(params string[] kv) => TestEnv.OgcUrl("wfs") + "?service=WFS&" + Qs(kv);
        public static string Wms(params string[] kv) => TestEnv.OgcUrl("wms") + "?service=WMS&" + Qs(kv);
        public static string Wcs(params string[] kv) => TestEnv.OgcUrl("wcs") + "?service=WCS&" + Qs(kv);
        public static string GwcWmts(params string[] kv) => TestEnv.RestBase + "/gwc/service/wmts?" + Qs(kv);
        public static string GwcRest(string path) => TestEnv.RestBase + "/gwc/rest/" + path;

        public static string Qs(string[] kv)
        {
            var sb = new StringBuilder();
            foreach (var s in kv)
            {
                int eq = s.IndexOf('=');
                if (eq < 0) continue;
                var k = s.Substring(0, eq);
                var v = s.Substring(eq + 1);
                if (sb.Length > 0) sb.Append('&');
                sb.Append(Uri.EscapeDataString(k)).Append('=').Append(Uri.EscapeDataString(v));
            }
            return sb.ToString();
        }
    }
}
