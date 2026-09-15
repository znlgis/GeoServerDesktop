using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Http
{
    /// <summary>
    /// GeoServer REST 请求/响应 JSON 序列化的共享设置。
    /// 根因修复（零维 coverage / XStream NPE 500 / 类型推断跳过等同族缺陷）：
    /// 请求体默认会把 null 属性序列化为显式 JSON null，GeoServer 3.x 的 XStream 反序列化
    /// 会把 null 覆盖到目录对象上（如 "srs":null → 零维 coverage、"store":null → ReferenceConverter NPE 500、
    /// "type":null → 跳过存储类型推断）。请求统一省略 null 字段；响应解析保持默认行为。
    /// 注意：仅用于请求序列化，勿用于 DeserializeObject。
    /// </summary>
    internal static class GeoServerJson
    {
        /// <summary>
        /// 请求体序列化设置：null 字段一律省略（NullValueHandling.Ignore）。
        /// </summary>
        public static readonly JsonSerializerSettings Request = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };
    }
}
