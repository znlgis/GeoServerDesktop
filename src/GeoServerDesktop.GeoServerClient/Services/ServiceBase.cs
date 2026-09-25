using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// 服务基类（M5 架构收敛）：集中 REST 样板——HTTP 客户端持有与空值校验、URL 片段转义、
    /// JSON 请求体构造（统一走 <see cref="GeoServerJson.Request"/> 的 null-省略语义）、
    /// 响应反序列化与包装体解包。
    ///
    /// 设计约束（公共 API 兼容红线）：
    /// ① 本基类只做"样板归一"，不改变任何派生服务的公开方法签名、请求 URL、请求体字节与响应解析结果；
    ///    转换前后由 L1 的 URL/请求体/解析三元断言与 L2 真实服务端契约测试共同守护。
    /// ② <see cref="JsonContent"/> 使用 GeoServerJson.Request（NullValueHandling.Ignore）——这是
    ///    GeoServer 3.x XStream 反序列化的必需形态（显式 null 会覆盖目录对象，实测致零维 coverage /
    ///    ReferenceConverter NPE 500）。<see cref="JsonContentRaw"/> 保留默认序列化设置，
    ///    仅用于历史上即以默认设置发送的端点（扩展未装、无实测基线，不擅自变更语义）。
    /// ③ 属性而非字段暴露客户端（<c>Http</c>），以规避 protected 字段命名规则并统一读取入口。
    /// </summary>
    public abstract class ServiceBase
    {
        /// <summary>
        /// 初始化 ServiceBase 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        protected ServiceBase(IGeoServerHttpClient httpClient)
        {
            Http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <summary>底层 GeoServer REST HTTP 客户端。</summary>
        protected IGeoServerHttpClient Http { get; }

        // ---------- URL ----------

        /// <summary>URL 路径片段转义（等价于 <c>Uri.EscapeDataString</c>，含 null 抛异常的原语义）。</summary>
        /// <param name="value">待转义片段。</param>
        /// <returns>转义后的片段。</returns>
        protected static string Esc(string value)
        {
            return Uri.EscapeDataString(value);
        }

        // ---------- 请求体 ----------

        /// <summary>构造 JSON 请求体（省略 null 字段；GeoServer 3.x 必需的请求形态）。</summary>
        /// <param name="payload">已含根包装的请求对象。</param>
        /// <returns>JSON 内容。</returns>
        protected static HttpContent JsonContent(object payload)
        {
            return JsonContent(payload, Formatting.None);
        }

        /// <summary>构造 JSON 请求体（指定格式化）。</summary>
        /// <param name="payload">已含根包装的请求对象。</param>
        /// <param name="formatting">序列化格式。</param>
        /// <returns>JSON 内容。</returns>
        protected static HttpContent JsonContent(object payload, Formatting formatting)
        {
            var json = JsonConvert.SerializeObject(payload, formatting, GeoServerJson.Request);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        /// <summary>序列化为 JSON 文本（省略 null 字段）——供需要自行组合请求体字符串的端点使用。</summary>
        /// <param name="payload">待序列化对象。</param>
        /// <returns>JSON 文本。</returns>
        protected static string ToJson(object payload)
        {
            return JsonConvert.SerializeObject(payload, GeoServerJson.Request);
        }

        /// <summary>构造 JSON 请求体（Newtonsoft 默认设置，保留 null 字段）。</summary>
        /// <param name="payload">已含根包装的请求对象。</param>
        /// <returns>JSON 内容。</returns>
        protected static HttpContent JsonContentRaw(object payload)
        {
            var json = JsonConvert.SerializeObject(payload);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        /// <summary>构造指定媒体类型的文本请求体（SLD/XML 等）。</summary>
        /// <param name="text">文本内容。</param>
        /// <param name="mediaType">媒体类型。</param>
        /// <returns>文本内容。</returns>
        protected static HttpContent TextContent(string text, string mediaType)
        {
            return new StringContent(text, Encoding.UTF8, mediaType);
        }

        /// <summary>构造二进制请求体（shapefile 包 / 栅格 / 字体上传）。</summary>
        /// <param name="bytes">字节内容。</param>
        /// <param name="mediaType">媒体类型。</param>
        /// <returns>二进制内容。</returns>
        protected static HttpContent BytesContent(byte[] bytes, string mediaType)
        {
            var content = new ByteArrayContent(bytes);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mediaType);
            return content;
        }

        // ---------- 动词简写（内容型请求由调用方 using 释放） ----------

        /// <summary>GET 原始响应文本。</summary>
        /// <param name="path">REST 相对路径。</param>
        /// <returns>响应文本。</returns>
        protected Task<string> GetAsync(string path)
        {
            return Http.GetAsync(path);
        }

        /// <summary>GET 并反序列化为目标类型（默认 Newtonsoft 设置，与历史行为一致）。</summary>
        /// <param name="path">REST 相对路径。</param>
        /// <returns>反序列化结果（响应为 <c>null</c>/空时返回 default）。</returns>
        protected async Task<T> GetJsonAsync<T>(string path)
        {
            var response = await Http.GetAsync(path);
            return response == null ? default(T) : JsonConvert.DeserializeObject<T>(response);
        }

        /// <summary>GET 并解包装（把"响应→包装体→目标模型"两步收敛为一步）。</summary>
        /// <param name="path">REST 相对路径。</param>
        /// <param name="unwrap">从包装体取目标模型的函数。</param>
        /// <returns>解包后的模型。</returns>
        protected async Task<TResult> GetWrappedAsync<TWrapper, TResult>(string path, Func<TWrapper, TResult> unwrap)
        {
            var wrapper = await GetJsonAsync<TWrapper>(path);
            return unwrap(wrapper);
        }

        /// <summary>反序列化响应文本（保留调用方原有解析语义的显式入口）。</summary>
        /// <param name="json">响应文本。</param>
        /// <returns>反序列化结果。</returns>
        protected static T Parse<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }

        /// <summary>POST JSON 并忽略响应体。</summary>
        /// <param name="path">REST 相对路径。</param>
        /// <param name="payload">已含根包装的请求对象。</param>
        /// <returns>表示异步操作的任务。</returns>
        protected Task PostJsonAsync(string path, object payload)
        {
            return PostJsonAsync(path, payload, rawNulls: false);
        }

        /// <summary>POST JSON（可选保留 null 字段）并忽略响应体。</summary>
        /// <param name="path">REST 相对路径。</param>
        /// <param name="payload">已含根包装的请求对象。</param>
        /// <param name="rawNulls">true 时使用 Newtonsoft 默认设置（保留显式 null）。</param>
        /// <returns>表示异步操作的任务。</returns>
        protected async Task PostJsonAsync(string path, object payload, bool rawNulls)
        {
            using (var content = rawNulls ? JsonContentRaw(payload) : JsonContent(payload))
            {
                await Http.PostAsync(path, content);
            }
        }

        /// <summary>PUT JSON 并忽略响应体。</summary>
        /// <param name="path">REST 相对路径。</param>
        /// <param name="payload">已含根包装的请求对象。</param>
        /// <returns>表示异步操作的任务。</returns>
        protected Task PutJsonAsync(string path, object payload)
        {
            return PutJsonAsync(path, payload, rawNulls: false);
        }

        /// <summary>PUT JSON（可选保留 null 字段）并忽略响应体。</summary>
        /// <param name="path">REST 相对路径。</param>
        /// <param name="payload">已含根包装的请求对象。</param>
        /// <param name="rawNulls">true 时使用 Newtonsoft 默认设置（保留显式 null）。</param>
        /// <returns>表示异步操作的任务。</returns>
        protected async Task PutJsonAsync(string path, object payload, bool rawNulls)
        {
            using (var content = rawNulls ? JsonContentRaw(payload) : JsonContent(payload))
            {
                await Http.PutAsync(path, content);
            }
        }

        /// <summary>DELETE 并忽略响应体。</summary>
        /// <param name="path">REST 相对路径（含查询串）。</param>
        /// <returns>表示异步操作的任务。</returns>
        protected Task DeleteAsync(string path)
        {
            return Http.DeleteAsync(path);
        }

        /// <summary>POST 非 JSON 请求体（SLD/XML/二进制上传），发送后释放内容。</summary>
        /// <param name="path">REST 相对路径。</param>
        /// <param name="content">请求内容（由 <see cref="TextContent"/>/<see cref="BytesContent"/> 构造）。</param>
        /// <returns>表示异步操作的任务。</returns>
        protected async Task PostContentAsync(string path, HttpContent content)
        {
            using (content)
            {
                await Http.PostAsync(path, content);
            }
        }

        /// <summary>PUT 非 JSON 请求体（SLD/XML/二进制上传），发送后释放内容。</summary>
        /// <param name="path">REST 相对路径。</param>
        /// <param name="content">请求内容。</param>
        /// <returns>表示异步操作的任务。</returns>
        protected async Task PutContentAsync(string path, HttpContent content)
        {
            using (content)
            {
                await Http.PutAsync(path, content);
            }
        }

        /// <summary>布尔查询参数的 GeoServer 形态（小写 true/false）。</summary>
        /// <param name="value">布尔值。</param>
        /// <returns>"true" 或 "false"。</returns>
        protected static string Bool(bool value)
        {
            return value ? "true" : "false";
        }
    }
}
