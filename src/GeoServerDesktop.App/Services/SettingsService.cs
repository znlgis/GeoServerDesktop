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
        /// 初始化 SettingsService 类的新实例
        /// </summary>
        public SettingsService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "GeoServerDesktop");
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
