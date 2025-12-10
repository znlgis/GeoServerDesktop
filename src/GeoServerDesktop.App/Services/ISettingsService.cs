using System.Threading.Tasks;

namespace GeoServerDesktop.App.Services
{
    /// <summary>
    /// Interface for managing application settings
    /// </summary>
    public interface ISettingsService
    {
        /// <summary>
        /// Loads settings from storage
        /// </summary>
        /// <returns>Settings object</returns>
        Task<AppSettings> LoadSettingsAsync();

        /// <summary>
        /// Saves settings to storage
        /// </summary>
        /// <param name="settings">Settings to save</param>
        Task SaveSettingsAsync(AppSettings settings);
    }

    /// <summary>
    /// Application settings
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// Last used GeoServer base URL
        /// </summary>
        public string? LastBaseUrl { get; set; }

        /// <summary>
        /// Last used username
        /// </summary>
        public string? LastUsername { get; set; }

        /// <summary>
        /// Whether to remember connection settings
        /// </summary>
        public bool RememberConnection { get; set; }

        /// <summary>
        /// Window width
        /// </summary>
        public double WindowWidth { get; set; } = 1200;

        /// <summary>
        /// Window height
        /// </summary>
        public double WindowHeight { get; set; } = 800;
    }
}
