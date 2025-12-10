using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace GeoServerDesktop.App.Services
{
    /// <summary>
    /// Service for managing application settings stored in JSON
    /// </summary>
    public class SettingsService : ISettingsService
    {
        private readonly string _settingsPath;

        /// <summary>
        /// Initializes a new instance of the SettingsService class
        /// </summary>
        public SettingsService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "GeoServerDesktop");
            Directory.CreateDirectory(appFolder);
            _settingsPath = Path.Combine(appFolder, "settings.json");
        }

        /// <summary>
        /// Loads settings from storage
        /// </summary>
        /// <returns>Settings object</returns>
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
        /// Saves settings to storage
        /// </summary>
        /// <param name="settings">Settings to save</param>
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
