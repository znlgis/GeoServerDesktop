using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Configuration;
using GeoServerDesktop.GeoServerClient.Http;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>
/// 本地 HttpListener 假 GeoServer 服务器：随机 127.0.0.1 端口，按可写字段 Status/ResponseBody
/// 应答每个请求，并记录收到的方法/路径/请求体/关键请求头。
/// </summary>
internal sealed class LocalHttpServer : IDisposable
{
    private readonly HttpListener _listener = new();
    private readonly CancellationTokenSource _cts = new();

    public static readonly bool Supported = Probe();

    /// <summary>应答状态码（每个请求实时读取，可在请求之间修改）。</summary>
    public int Status = 200;
    /// <summary>应答体。</summary>
    public string ResponseBody = "{}";

    public sealed record Received(string Method, string PathAndQuery, string Body, string? Authorization, string? Accept, string? ContentType);

    public readonly List<Received> Requests = new();

    public string BaseUrl { get; }

    private static bool Probe()
    {
        // 探测当前环境是否允许 HttpListener（Windows 非管理员可能 Access Denied）
        try
        {
            var l = new HttpListener();
            l.Prefixes.Add($"http://127.0.0.1:{GetFreePort()}/");
            l.Start();
            l.Stop();
            return true;
        }
        catch { return false; }
    }

    private static int GetFreePort()
    {
        var tl = new TcpListener(IPAddress.Loopback, 0);
        tl.Start();
        int port = ((IPEndPoint)tl.LocalEndpoint).Port;
        tl.Stop();
        return port;
    }

    public LocalHttpServer(string urlPrefix = "")
    {
        int port = GetFreePort();
        BaseUrl = $"http://127.0.0.1:{port}{urlPrefix}";
        _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        _listener.Start();
        _ = Task.Run(LoopAsync);
    }

    private async Task LoopAsync()
    {
        while (!_cts.IsCancellationRequested && _listener.IsListening)
        {
            HttpListenerContext ctx;
            try { ctx = await _listener.GetContextAsync(); }
            catch { return; } // Stop/Close 期间中止

            try
            {
                string body;
                using (var sr = new StreamReader(ctx.Request.InputStream, Encoding.UTF8))
                    body = await sr.ReadToEndAsync();

                lock (Requests)
                {
                    Requests.Add(new Received(
                        ctx.Request.HttpMethod,
                        ctx.Request.Url.PathAndQuery,
                        body,
                        ctx.Request.Headers["Authorization"],
                        ctx.Request.Headers["Accept"],
                        ctx.Request.ContentType));
                }

                var bytes = Encoding.UTF8.GetBytes(ResponseBody);
                ctx.Response.StatusCode = Status;
                ctx.Response.ContentType = "application/json";
                ctx.Response.ContentLength64 = bytes.Length;
                await ctx.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            }
            catch { /* 客户端中断等，忽略 */ }
            finally { try { ctx.Response.Close(); } catch { } }
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        try { _listener.Stop(); } catch { }
        try { _listener.Close(); } catch { }
    }
}

/// <summary>GeoServerHttpClient 真实 HTTP 行为测试（C 组 + B 组错误路径）。</summary>
public class GeoServerHttpClientTests
{
    private static readonly FieldInfo HttpClientProp =
        typeof(GeoServerHttpClient).GetField("_httpClient", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo BaseUrlField =
        typeof(GeoServerHttpClient).GetField("_baseUrl", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static HttpClient Inner(GeoServerHttpClient c) => (HttpClient)HttpClientProp.GetValue(c)!;
    private static string Base(GeoServerHttpClient c) => (string)BaseUrlField.GetValue(c)!;

    private static GeoServerClientOptions Opts(string url, string user = "admin", string pass = "geoserver", int timeout = 30) =>
        new() { BaseUrl = url, Username = user, Password = pass, TimeoutSeconds = timeout };

    // ---------- 构造/选项 ----------

    [Fact]
    public void Ctor_NullOptions_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new GeoServerHttpClient(null!));
        Assert.Equal("options", ex.ParamName);
    }

    [Fact]
    public void Ctor_BlankBaseUrl_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => new GeoServerHttpClient(new GeoServerClientOptions { BaseUrl = "   " }));
        Assert.Contains("BaseUrl is required", ex.Message);
    }

    [Fact]
    public void Ctor_TrimsAllTrailingSlashesFromBaseUrl()
    {
        // BaseUrl 尾斜杠全部裁掉（TrimEnd('/') 去多重）
        using var c = new GeoServerHttpClient(Opts("http://localhost:8080/geoserver///"));
        Assert.Equal("http://localhost:8080/geoserver", Base(c));
    }

    [Fact]
    public void Ctor_SetsTimeoutFromOptionsAndDefaultIs30()
    {
        using var c17 = new GeoServerHttpClient(Opts("http://h", timeout: 17));
        Assert.Equal(TimeSpan.FromSeconds(17), Inner(c17).Timeout);

        var o = Opts("http://h");
        Assert.Equal(30, o.TimeoutSeconds); // 默认值（GeoServerClientOptions 源码）
        using var c30 = new GeoServerHttpClient(o);
        Assert.Equal(TimeSpan.FromSeconds(30), Inner(c30).Timeout);
    }

    [Fact]
    public void Ctor_SetsExactBasicAuthHeaderForKnownCredentials()
    {
        // FIXED-E31：凭证按 UTF-8 编码（原 Encoding.ASCII 会把非 ASCII 字符打成 '?'）。
        // 纯 ASCII 的 admin:geoserver base64 值不变（UTF-8 与 ASCII 对 0-127 同码）。
        using var c = new GeoServerHttpClient(Opts("http://h"));
        var auth = Inner(c).DefaultRequestHeaders.Authorization;
        Assert.NotNull(auth);
        Assert.Equal("Basic", auth!.Scheme);
        Assert.Equal("YWRtaW46Z2Vvc2VydmVy", auth.Parameter);
    }

    [Fact]
    public void Ctor_NonAsciiPassword_EncodedAsUtf8_FIXED_E31()
    {
        // FIXED-E31：UTF-8("ä") = C3 A4 → base64 "w5Q"；ASCII 旧行为会得到单字节 '?'(3F)。
        using var c = new GeoServerHttpClient(Opts("http://h", user: "a", pass: "ä"));
        var auth = Inner(c).DefaultRequestHeaders.Authorization;
        // ASCII 旧行为会打成 "a:?" → base64 "YTo/"，与期望值不等，即回归该缺陷
        Assert.Equal(
            Convert.ToBase64String(Encoding.UTF8.GetBytes("a:ä")),
            auth!.Parameter);
    }

    [Fact]
    public void Ctor_BlankUsername_OmitsAuthHeader()
    {
        using var c = new GeoServerHttpClient(Opts("http://h", user: ""));
        Assert.Null(Inner(c).DefaultRequestHeaders.Authorization);
    }

    [Fact]
    public void Ctor_AddsJsonAcceptHeader()
    {
        // KNOWN-ISSUE E41【已修复 + 回归固化】：默认 Accept 原为单一 application/json，导致
        // GeoServer 3.x POST /rest/styles 因 handler 选择 500 "No such style handler"。产品已改为
        // `application/json, text/plain;q=0.9`（实测 curl 201）。此处固化两个头值均在、且含 text/plain。
        using var c = new GeoServerHttpClient(Opts("http://h"));
        var accepts = Inner(c).DefaultRequestHeaders.Accept;
        Assert.Contains(accepts, h => h.MediaType == "application/json");
        Assert.Contains(accepts, h => h.MediaType == "text/plain");
    }

    // ---------- 请求/响应（真实 HttpListener） ----------

    [Fact]
    public async Task GetAsync_ReturnsBodyVerbatim()
    {
        if (!LocalHttpServer.Supported) return; // 环境不支持 HttpListener 时跳过（见文件头注释）
        using var server = new LocalHttpServer("/geoserver");
        server.ResponseBody = "{\"ok\":true}";
        using var c = new GeoServerHttpClient(Opts(server.BaseUrl + "/"));

        var result = await c.GetAsync("/rest/about/version.json");

        Assert.Equal("{\"ok\":true}", result);
        // BaseUrl 尾斜杠已裁剪 + path 前导斜杠拼接：无双斜杠
        Assert.Equal("/geoserver/rest/about/version.json", server.Requests[0].PathAndQuery);
        Assert.Equal("GET", server.Requests[0].Method);
    }

    [Fact]
    public async Task GetAsync_PathWithoutLeadingSlashAndNullPath_BuildUrls()
    {
        if (!LocalHttpServer.Supported) return;
        using var server = new LocalHttpServer("/geoserver");
        using var c = new GeoServerHttpClient(Opts(server.BaseUrl));

        await c.GetAsync("rest/x");      // 无首斜杠
        await c.GetAsync(null!);         // null path → {base}/（BuildUrl 现状）

        Assert.Equal("/geoserver/rest/x", server.Requests[0].PathAndQuery);
        Assert.Equal("/geoserver/", server.Requests[1].PathAndQuery);
    }

    [Fact]
    public async Task PostAsync_ForwardsBodyContentTypeAndDefaultHeaders()
    {
        if (!LocalHttpServer.Supported) return;
        using var server = new LocalHttpServer();
        using var c = new GeoServerHttpClient(Opts(server.BaseUrl));

        await c.PostAsync("/rest/workspaces",
            new StringContent("{\"workspace\":{\"name\":\"t\"}}", Encoding.UTF8, "application/json"));

        var r = server.Requests[0];
        Assert.Equal("POST", r.Method);
        Assert.Equal("{\"workspace\":{\"name\":\"t\"}}", r.Body);
        Assert.StartsWith("application/json", r.ContentType!);
        Assert.Equal("Basic YWRtaW46Z2Vvc2VydmVy", r.Authorization); // 精确头值
        // E41 已修复回归固化：出站请求须同时携带 application/json 与 text/plain（低优先级）。
        Assert.Contains("application/json", r.Accept!);
        Assert.Contains("text/plain", r.Accept!);
    }

    [Fact]
    public async Task PutAndDelete_ForwardCorrectVerbs()
    {
        if (!LocalHttpServer.Supported) return;
        using var server = new LocalHttpServer();
        using var c = new GeoServerHttpClient(Opts(server.BaseUrl));

        await c.PutAsync("/rest/x", new StringContent("s"));
        await c.DeleteAsync("/rest/x");

        Assert.Equal("PUT", server.Requests[0].Method);
        Assert.Equal("DELETE", server.Requests[1].Method);
    }

    // ---------- 错误路径（B 组）：404/400/500 ----------

    [Fact]
    public async Task GetAsync_404_ThrowsGeoServerRequestExceptionCarryingStatusAndBody()
    {
        if (!LocalHttpServer.Supported) return;
        using var server = new LocalHttpServer();
        server.Status = 404;
        server.ResponseBody = "No such workspace: topp";
        using var c = new GeoServerHttpClient(Opts(server.BaseUrl));

        var ex = await Assert.ThrowsAsync<GeoServerRequestException>(() => c.GetAsync("/rest/workspaces/topp.json"));

        Assert.Equal(404, ex.StatusCode);
        Assert.Equal("No such workspace: topp", ex.ResponseContent);
        // 消息格式固定：含数字码与枚举名
        Assert.Equal("GeoServer request failed with status code 404 (NotFound)", ex.Message);
    }

    [Fact]
    public async Task PostAsync_400_ThrowsWithMultiLineBody()
    {
        if (!LocalHttpServer.Supported) return;
        using var server = new LocalHttpServer();
        using var c = new GeoServerHttpClient(Opts(server.BaseUrl));
        server.Status = 400;
        server.ResponseBody = "<h1>Bad Request</h1><p>workspace exists</p>";

        var ex = await Assert.ThrowsAsync<GeoServerRequestException>(
            () => c.PostAsync("/rest/workspaces", new StringContent("{}")));

        Assert.Equal(400, ex.StatusCode);
        Assert.Contains("Bad Request", ex.ResponseContent);
        Assert.Contains("(BadRequest)", ex.Message);
    }

    [Fact]
    public async Task DeleteAsync_500_ThrowsAndBodyPreserved()
    {
        if (!LocalHttpServer.Supported) return;
        using var server = new LocalHttpServer();
        using var c = new GeoServerHttpClient(Opts(server.BaseUrl));
        server.Status = 500;
        server.ResponseBody = "{\"errors\":{\"string\":\"internal\"}}";

        var ex = await Assert.ThrowsAsync<GeoServerRequestException>(() => c.DeleteAsync("/rest/layers/x"));

        Assert.Equal(500, ex.StatusCode);
        Assert.Equal("{\"errors\":{\"string\":\"internal\"}}", ex.ResponseContent);
    }

    [Fact]
    public async Task Non2xx_But201_IsSuccessAndReturnsBody()
    {
        // 2xx 判定（IsSuccessStatusCode）：201 不抛
        if (!LocalHttpServer.Supported) return;
        using var server = new LocalHttpServer();
        server.Status = 201;
        server.ResponseBody = "created";
        using var c = new GeoServerHttpClient(Opts(server.BaseUrl));

        Assert.Equal("created", await c.PostAsync("/rest/x", new StringContent("")));
    }

    // ---------- GeoServerRequestException 直接构造断言 ----------

    [Fact]
    public void Exception_Ctor_StoresReadOnlyProps()
    {
        var ex = new GeoServerRequestException("msg", 409, "conflict body");
        Assert.Equal(409, ex.StatusCode);
        Assert.Equal("conflict body", ex.ResponseContent);
        Assert.Equal("msg", ex.Message);
        Assert.IsAssignableFrom<Exception>(ex);
    }

    [Fact]
    public void Exception_CtorWithInner_StoresAll()
    {
        var inner = new InvalidOperationException("io");
        var ex = new GeoServerRequestException("msg", 503, "down", inner);
        Assert.Equal(503, ex.StatusCode);
        Assert.Equal("down", ex.ResponseContent);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var c = new GeoServerHttpClient(Opts("http://localhost:1"));
        c.Dispose();
        c.Dispose(); // 双重 Dispose 不抛（Dispose(bool) 有 _disposed 门闩）
    }
}
