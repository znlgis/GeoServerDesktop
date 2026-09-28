using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Configuration;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Services;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Unit
{
    /// <summary>
    /// E46 错误原因摘要：GeoServer 并不总是返回自身的 JSON/XML——容器层（Tomcat）会直接给出 HTML 错误页，
    /// 早前把整页 HTML 截断 200 字塞进 Message，用户既读不懂也丢掉了真正的原因。
    /// 这里用一个最小服务子类离线验证三种形态的摘要行为。
    /// </summary>
    public class ServiceDescribeTests
    {
        private sealed class ProbeService : ServiceBase
        {
            public ProbeService(IGeoServerHttpClient http) : base(http) { }

            /// <summary>把任意异常摘要成一句原因。</summary>
            public static string Summarize(Exception ex) => Describe(ex);

            /// <summary>触发一次 GET 让真实异常路径跑起来。</summary>
            public async Task<string> BoomAsync()
            {
                try { await Http.GetAsync("/rest/nope.json"); return null; }
                catch (Exception ex) { return Describe(ex); }
            }
        }

        private sealed class ThrowingClient : IGeoServerHttpClient
        {
            private readonly Exception _ex;
            public ThrowingClient(Exception ex) { _ex = ex; }
            public Task<string> GetAsync(string path, System.Threading.CancellationToken ct = default) => throw _ex;
            public Task<string> PostAsync(string path, System.Net.Http.HttpContent content, System.Threading.CancellationToken ct = default) => throw _ex;
            public Task<string> PutAsync(string path, System.Net.Http.HttpContent content, System.Threading.CancellationToken ct = default) => throw _ex;
            public Task<string> DeleteAsync(string path, System.Threading.CancellationToken ct = default) => throw _ex;
            public void Dispose() { }
        }

        [Fact]
        public static void Summarize_TomcatHtmlPage_ExtractsMessageLine()
        {
            var html = "<!doctype html><html lang=\"en\"><head><title>HTTP Status 400 \u2013 Bad Request</title>"
                + "<style>body{color:red}</style></head><body><h1>HTTP Status 400 \u2013 Bad Request</h1>"
                + "<p><b>Type</b> Status Report</p>"
                + "<p><b>Message</b> Trying to create new feature type inside the store, but no attributes were specified</p>"
                + "<p><b>Description</b> The server cannot process the request</p><hr /></body></html>";
            var ex = new GeoServerRequestException("fail", 400, html);

            string msg = ProbeService.Summarize(ex);

            Assert.StartsWith("HTTP 400", msg);
            Assert.Contains("no attributes were specified", msg);
            Assert.DoesNotContain("<html", msg, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("<p>", msg, StringComparison.Ordinal);
            Assert.DoesNotContain("color:red", msg, StringComparison.Ordinal);   // 样式与页头不得混进原因
        }

        [Fact]
        public static void Summarize_GeoServerJson_ExtractsDetail()
        {
            var json = "{\"exception\":\"BadRequestException\",\"detail\":\"Access to workspaces is denied\","
                + "\"message\":\"forbidden\"}";
            string msg = ProbeService.Summarize(new GeoServerRequestException("x", 403, json));

            Assert.Contains("Access to workspaces is denied", msg);
            Assert.DoesNotContain("detail", msg, StringComparison.Ordinal);
        }

        [Fact]
        public static void Summarize_GeoServerJson_DecodesEscapes()
        {
            string msg = ProbeService.Summarize(new GeoServerRequestException("x", 400,
                "{\"detail\":\"Failed to parse \\\"bbox\\\" value\\nsecond line\"}"));

            Assert.Contains("Failed to parse \"bbox\" value", msg);
            Assert.DoesNotContain("\\n", msg, StringComparison.Ordinal);      // 转义要还原
            Assert.DoesNotContain("\n", msg, StringComparison.Ordinal);       // 摘要必须单行
        }

        [Fact]
        public static void Summarize_OgcXml_ExtractsExceptionText()
        {
            string msg = ProbeService.Summarize(new GeoServerRequestException("x", 400,
                "<ows:ExceptionReport xmlns:ows=\"http://www.opengis.net/ows/1.1\" version=\"2.0.0\">"
                + "<ows:Exception exceptionCode=\"InvalidParameterValue\" locator=\"typeName\">"
                + "<ows:ExceptionText>Feature type probe:layer unknown</ows:ExceptionText>"
                + "</ows:Exception></ows:ExceptionReport>"));

            Assert.Contains("Feature type probe:layer unknown", msg);
            Assert.DoesNotContain("ExceptionReport", msg, StringComparison.Ordinal);
        }

        [Fact]
        public static void Summarize_EmptyOrMissingBody_KeepsStatusCodeOnly()
        {
            Assert.Equal("HTTP 500", ProbeService.Summarize(new GeoServerRequestException("x", 500, null)));
            Assert.Equal("HTTP 502", ProbeService.Summarize(new GeoServerRequestException("x", 502, "   ")));
        }

        [Fact]
        public static void Summarize_NonGeoServerException_KeepsOriginalMessage()
        {
            string msg = ProbeService.Summarize(new InvalidOperationException("boom"));
            Assert.Equal("boom", msg);
        }

        [Fact]
        public static void Summarize_VeryLongBody_IsTruncatedWithEllipsis()
        {
            string msg = ProbeService.Summarize(new GeoServerRequestException("x", 500,
                "<html><body><p><b>Message</b> " + new string('x', 4000) + "</p></body></html>"));
            Assert.True(msg.Length <= 320, "摘要长度应受控，实测 " + msg.Length);
            Assert.EndsWith("\u2026", msg, StringComparison.Ordinal);
        }

        [Fact]
        public async Task ServicePath_RealException_IsSummarizedForCaller()
        {
            var http = new ThrowingClient(new GeoServerRequestException("x", 400,
                "<html><body><p><b>Message</b> no attributes were specified</p></body></html>"));
            var svc = new ProbeService(http);

            string msg = await svc.BoomAsync();

            Assert.Contains("no attributes were specified", msg);
            Assert.DoesNotContain("<body>", msg, StringComparison.Ordinal);
        }

        [Fact]
        public static void Factory_StillConstructsServices() =>
            Assert.NotNull(new GeoServerClientFactory(new GeoServerClientOptions
            {
                BaseUrl = "http://localhost:1/geoserver",
                Username = "admin",
                Password = "x",
            }).CreateAboutService());
    }
}
