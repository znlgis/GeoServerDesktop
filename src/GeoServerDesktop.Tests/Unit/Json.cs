using Newtonsoft.Json.Linq;

namespace GeoServerDesktop.Tests.Unit
{
    /// <summary>
    /// 单元测试辅助：解析被 RecordingFakeClient 捕获的请求体 JSON。
    /// 去除可能的 UTF-8 BOM 前缀后交给 JObject.Parse。
    /// </summary>
    internal static class Json
    {
        public static JObject P(string body)
        {
            Assert.NotNull(body);
            return JObject.Parse(body!.TrimStart('\uFEFF'));
        }
    }
}
