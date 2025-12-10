using System.Threading.Tasks;

namespace GeoServerDesktop.App.Services
{
    /// <summary>
    /// 用于管理应用程序设置的接口
    /// </summary>
    public interface ISettingsService
    {
        /// <summary>
        /// 从存储中加载设置
        /// </summary>
        /// <returns>设置对象</returns>
        Task<AppSettings> LoadSettingsAsync();

        /// <summary>
        /// 将设置保存到存储
        /// </summary>
        /// <param name="settings">要保存的设置</param>
        Task SaveSettingsAsync(AppSettings settings);
    }

    /// <summary>
    /// 应用程序设置
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// 最后使用的 GeoServer 基础 URL
        /// </summary>
        public string? LastBaseUrl { get; set; }

        /// <summary>
        /// 最后使用的用户名
        /// </summary>
        public string? LastUsername { get; set; }

        /// <summary>
        /// 是否记住连接设置
        /// </summary>
        public bool RememberConnection { get; set; }

        /// <summary>
        /// 窗口宽度
        /// </summary>
        public double WindowWidth { get; set; } = 1200;

        /// <summary>
        /// 窗口高度
        /// </summary>
        public double WindowHeight { get; set; } = 800;
    }
}
