using System;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// 从 GeoServer 日志位置配置（logging.xml 的 location / GEOSERVER_LOG_LOCATION）
    /// 推导 data_dir 内的 REST 资源路径，用于通过 /rest/resource 读取日志文件内容。
    /// </summary>
    /// <remarks>
    /// /rest/resource/{path} 的路径相对于数据目录（data_dir）。日志位置通常是
    /// data_dir 内（或等于）的绝对路径（如 /data/geoserver/data_dir/logs/geoserver.log），
    /// 也可能以相对形式（logs/geoserver.log）直接出现。
    /// </remarks>
    public static class LogResourcePathResolver
    {
        /// <summary>GeoServer 未显式配置日志位置时的默认资源路径（data_dir 内）。</summary>
        public const string DefaultLogResourcePath = "logs/geoserver.log";

        /// <summary>
        /// 将日志文件位置解析为 data_dir 相对资源路径（如 "logs/geoserver.log"）。
        /// 位置无法映射到数据目录的 logs 段（无法通过 REST 访问）时返回 null。
        /// </summary>
        /// <param name="logLocation">日志位置（绝对路径如 /data/geoserver/data_dir/logs/geoserver.log，或相对路径如 logs/geoserver.log）</param>
        /// <returns>data_dir 相对资源路径；无法解析时返回 null</returns>
        public static string Resolve(string logLocation)
        {
            if (string.IsNullOrWhiteSpace(logLocation))
                return null;

            var normalized = logLocation.Replace('\\', '/').Trim();

            // 带目录的路径（绝对或相对）：取最后一个 "/logs/" 之后的部分作为 data_dir 相对路径
            var idx = normalized.LastIndexOf("/logs/", StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                var relative = normalized.Substring(idx + 1); // 保留 "logs/..." 原样
                return relative.Length > "logs/".Length ? relative : null;
            }

            // 相对路径（相对 data_dir）
            if (normalized.StartsWith("logs/", StringComparison.OrdinalIgnoreCase))
                return normalized.Length > "logs/".Length ? normalized : null;

            return null;
        }
    }
}
