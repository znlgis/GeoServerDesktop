using System;

namespace GeoServerDesktop.GeoServerClient.Http
{
    /// <summary>
    /// 当 GeoServer REST API 请求失败时抛出的异常
    /// </summary>
    public class GeoServerRequestException : Exception
    {
        /// <summary>
        /// 响应的 HTTP 状态码
        /// </summary>
        public int StatusCode { get; }

        /// <summary>
        /// 失败请求的响应内容
        /// </summary>
        public string ResponseContent { get; }

        /// <summary>
        /// 初始化 GeoServerRequestException 类的新实例
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="statusCode">HTTP 状态码</param>
        /// <param name="responseContent">响应内容</param>
        public GeoServerRequestException(string message, int statusCode, string responseContent)
            : base(message)
        {
            StatusCode = statusCode;
            ResponseContent = responseContent;
        }

        /// <summary>
        /// 使用内部异常初始化 GeoServerRequestException 类的新实例
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="statusCode">HTTP 状态码</param>
        /// <param name="responseContent">响应内容</param>
        /// <param name="innerException">内部异常</param>
        public GeoServerRequestException(string message, int statusCode, string responseContent, Exception innerException)
            : base(message, innerException)
        {
            StatusCode = statusCode;
            ResponseContent = responseContent;
        }
    }
}
