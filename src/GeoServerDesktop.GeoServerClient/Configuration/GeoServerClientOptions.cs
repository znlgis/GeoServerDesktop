namespace GeoServerDesktop.GeoServerClient.Configuration
{
    /// <summary>
    /// GeoServer REST API 客户端的配置选项
    /// </summary>
    public class GeoServerClientOptions
    {
        /// <summary>
        /// GeoServer 实例的基础 URL（例如 http://localhost:8080/geoserver）
        /// </summary>
        public string BaseUrl { get; set; }

        /// <summary>
        /// 基本身份验证的用户名
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// 基本身份验证的密码
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// HTTP 请求超时时间（秒）（默认值：30）
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;
    }
}
