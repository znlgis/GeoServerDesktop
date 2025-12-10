using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing GeoServer styles
    /// </summary>
    public class StyleService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the StyleService class
        /// </summary>
        /// <param name="httpClient">HTTP client for GeoServer operations</param>
        public StyleService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets a list of all styles
        /// </summary>
        /// <returns>Array of styles</returns>
        public async Task<Style[]> GetStylesAsync()
        {
            var response = await _httpClient.GetAsync("/rest/styles.json");
            var wrapper = JsonConvert.DeserializeObject<StyleListWrapper>(response);
            return wrapper?.StyleList?.Styles ?? new Style[0];
        }

        /// <summary>
        /// Gets details for a specific style
        /// </summary>
        /// <param name="styleName">Name of the style</param>
        /// <returns>Style details</returns>
        public async Task<Style> GetStyleAsync(string styleName)
        {
            var response = await _httpClient.GetAsync($"/rest/styles/{styleName}.json");
            var wrapper = JsonConvert.DeserializeObject<StyleWrapper>(response);
            return wrapper?.Style;
        }

        /// <summary>
        /// Gets the SLD content for a specific style
        /// </summary>
        /// <param name="styleName">Name of the style</param>
        /// <returns>SLD content as string</returns>
        public async Task<string> GetStyleSldAsync(string styleName)
        {
            var response = await _httpClient.GetAsync($"/rest/styles/{styleName}.sld");
            return response;
        }

        /// <summary>
        /// Creates a new style
        /// </summary>
        /// <param name="styleName">Name of the style to create</param>
        /// <param name="sldContent">SLD content for the style</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task CreateStyleAsync(string styleName, string sldContent)
        {
            // First create the style metadata
            var style = new { style = new { name = styleName, filename = $"{styleName}.sld" } };
            var json = JsonConvert.SerializeObject(style);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync("/rest/styles", content);

            // Then upload the SLD content
            var sldContentData = new StringContent(sldContent, Encoding.UTF8, "application/vnd.ogc.sld+xml");
            await _httpClient.PutAsync($"/rest/styles/{styleName}", sldContentData);
        }

        /// <summary>
        /// Updates an existing style
        /// </summary>
        /// <param name="styleName">Name of the style</param>
        /// <param name="sldContent">Updated SLD content</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task UpdateStyleAsync(string styleName, string sldContent)
        {
            var content = new StringContent(sldContent, Encoding.UTF8, "application/vnd.ogc.sld+xml");
            await _httpClient.PutAsync($"/rest/styles/{styleName}", content);
        }

        /// <summary>
        /// Deletes a style
        /// </summary>
        /// <param name="styleName">Name of the style to delete</param>
        /// <param name="purge">Whether to purge the style file from disk</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task DeleteStyleAsync(string styleName, bool purge = false)
        {
            var path = $"/rest/styles/{styleName}?purge={purge.ToString().ToLower()}";
            await _httpClient.DeleteAsync(path);
        }

        /// <summary>
        /// Gets a list of styles in a specific workspace
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <returns>Array of styles in the workspace</returns>
        public async Task<Style[]> GetWorkspaceStylesAsync(string workspaceName)
        {
            var response = await _httpClient.GetAsync($"/rest/workspaces/{workspaceName}/styles.json");
            var wrapper = JsonConvert.DeserializeObject<StyleListWrapper>(response);
            return wrapper?.StyleList?.Styles ?? new Style[0];
        }

        /// <summary>
        /// Gets details for a specific style in a workspace
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <param name="styleName">Name of the style</param>
        /// <returns>Style details</returns>
        public async Task<Style> GetWorkspaceStyleAsync(string workspaceName, string styleName)
        {
            var response = await _httpClient.GetAsync($"/rest/workspaces/{workspaceName}/styles/{styleName}.json");
            var wrapper = JsonConvert.DeserializeObject<StyleWrapper>(response);
            return wrapper?.Style;
        }

        /// <summary>
        /// Gets the SLD content for a specific style in a workspace
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <param name="styleName">Name of the style</param>
        /// <returns>SLD content as string</returns>
        public async Task<string> GetWorkspaceStyleSldAsync(string workspaceName, string styleName)
        {
            var response = await _httpClient.GetAsync($"/rest/workspaces/{workspaceName}/styles/{styleName}.sld");
            return response;
        }

        /// <summary>
        /// Creates a new style in a specific workspace
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <param name="styleName">Name of the style to create</param>
        /// <param name="sldContent">SLD content for the style</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task CreateWorkspaceStyleAsync(string workspaceName, string styleName, string sldContent)
        {
            // First create the style metadata
            var style = new { style = new { name = styleName, filename = $"{styleName}.sld" } };
            var json = JsonConvert.SerializeObject(style);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync($"/rest/workspaces/{workspaceName}/styles", content);

            // Then upload the SLD content
            var sldContentData = new StringContent(sldContent, Encoding.UTF8, "application/vnd.ogc.sld+xml");
            await _httpClient.PutAsync($"/rest/workspaces/{workspaceName}/styles/{styleName}", sldContentData);
        }

        /// <summary>
        /// Updates an existing style in a workspace
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <param name="styleName">Name of the style</param>
        /// <param name="sldContent">Updated SLD content</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task UpdateWorkspaceStyleAsync(string workspaceName, string styleName, string sldContent)
        {
            var content = new StringContent(sldContent, Encoding.UTF8, "application/vnd.ogc.sld+xml");
            await _httpClient.PutAsync($"/rest/workspaces/{workspaceName}/styles/{styleName}", content);
        }

        /// <summary>
        /// Deletes a style from a workspace
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <param name="styleName">Name of the style to delete</param>
        /// <param name="purge">Whether to purge the style file from disk</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task DeleteWorkspaceStyleAsync(string workspaceName, string styleName, bool purge = false)
        {
            var path = $"/rest/workspaces/{workspaceName}/styles/{styleName}?purge={purge.ToString().ToLower()}";
            await _httpClient.DeleteAsync(path);
        }
    }
}
