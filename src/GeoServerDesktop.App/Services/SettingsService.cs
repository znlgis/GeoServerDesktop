using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace GeoServerDesktop.App.Services
{
    /// <summary>
    /// 用于管理存储在 JSON 中的应用程序设置的服务
    /// </summary>
    public class SettingsService : ISettingsService
    {
        private readonly string _settingsPath;

        /// <summary>
        /// 【TESTABILITY】设置目录注入点（进程级）：供无头测试将 settings.json 重定向到临时目录，
        /// 避免触碰用户真实 %APPDATA%/GeoServerDesktop。为 null/空时保持原有默认行为（写入 %APPDATA%）。
        /// 这是本类为可测试性所做的唯一最小改动，产品运行时不设置该值。
        /// </summary>
        public static string? SettingsDirectoryOverride { get; set; }

        /// <summary>
        /// 初始化 SettingsService 类的新实例
        /// </summary>
        public SettingsService()
        {
            // 【TESTABILITY】若存在目录覆盖则使用之，否则维持原有 %APPDATA% 行为。
            var appFolder = string.IsNullOrWhiteSpace(SettingsDirectoryOverride)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GeoServerDesktop")
                : SettingsDirectoryOverride!;
            Directory.CreateDirectory(appFolder);
            _settingsPath = Path.Combine(appFolder, "settings.json");
        }

        /// <summary>
        /// 从存储中加载设置
        /// </summary>
        /// <returns>设置对象</returns>
        public async Task<AppSettings> LoadSettingsAsync()
        {
            if (!File.Exists(_settingsPath))
            {
                return new AppSettings();
            }

            try
            {
                var json = await File.ReadAllTextAsync(_settingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        /// <summary>
        /// 将设置保存到存储
        /// </summary>
        /// <param name="settings">要保存的设置</param>
        public async Task SaveSettingsAsync(AppSettings settings)
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(_settingsPath, json);
        }
    }
}
