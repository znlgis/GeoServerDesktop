using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing fonts in GeoServer
    /// </summary>
    public class FontService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the FontService class
        /// </summary>
        /// <param name="httpClient">HTTP client for GeoServer operations</param>
        public FontService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets the list of available fonts
        /// </summary>
        /// <returns>List of fonts</returns>
        public async Task<FontListWrapper> GetFontsAsync()
        {
            var response = await _httpClient.GetAsync("/rest/fonts.json");
            return JsonConvert.DeserializeObject<FontListWrapper>(response);
        }

        /// <summary>
        /// Uploads a new font file
        /// </summary>
        /// <param name="fontName">Font name</param>
        /// <param name="fontContent">Font file content</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task UploadFontAsync(string fontName, byte[] fontContent)
        {
            var content = new ByteArrayContent(fontContent);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            await _httpClient.PutAsync($"/rest/fonts/{fontName}", content);
        }
    }
}
