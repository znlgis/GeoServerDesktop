using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing GeoServer feature types
    /// </summary>
    public class FeatureTypeService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the FeatureTypeService class
        /// </summary>
        /// <param name="httpClient">HTTP client for GeoServer operations</param>
        public FeatureTypeService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets a list of feature types in a data store
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <param name="dataStoreName">Name of the data store</param>
        /// <returns>Array of feature types</returns>
        public async Task<FeatureType[]> GetFeatureTypesAsync(string workspaceName, string dataStoreName)
        {
            var response = await _httpClient.GetAsync($"/rest/workspaces/{workspaceName}/datastores/{dataStoreName}/featuretypes.json");
            var wrapper = JsonConvert.DeserializeObject<FeatureTypeListWrapper>(response);
            return wrapper?.FeatureTypeList?.FeatureTypes ?? new FeatureType[0];
        }

        /// <summary>
        /// Gets details for a specific feature type
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <param name="dataStoreName">Name of the data store</param>
        /// <param name="featureTypeName">Name of the feature type</param>
        /// <returns>Feature type details</returns>
        public async Task<FeatureType> GetFeatureTypeAsync(string workspaceName, string dataStoreName, string featureTypeName)
        {
            var response = await _httpClient.GetAsync($"/rest/workspaces/{workspaceName}/datastores/{dataStoreName}/featuretypes/{featureTypeName}.json");
            var wrapper = JsonConvert.DeserializeObject<FeatureTypeWrapper>(response);
            return wrapper?.FeatureType;
        }

        /// <summary>
        /// Publishes a new feature type (creates a layer from a feature type)
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <param name="dataStoreName">Name of the data store</param>
        /// <param name="featureType">Feature type configuration</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task CreateFeatureTypeAsync(string workspaceName, string dataStoreName, FeatureType featureType)
        {
            var wrapper = new { featureType = featureType };
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync($"/rest/workspaces/{workspaceName}/datastores/{dataStoreName}/featuretypes", content);
        }

        /// <summary>
        /// Updates an existing feature type
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <param name="dataStoreName">Name of the data store</param>
        /// <param name="featureTypeName">Name of the feature type</param>
        /// <param name="featureType">Updated feature type configuration</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task UpdateFeatureTypeAsync(string workspaceName, string dataStoreName, string featureTypeName, FeatureType featureType)
        {
            var wrapper = new { featureType = featureType };
            var json = JsonConvert.SerializeObject(wrapper);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync($"/rest/workspaces/{workspaceName}/datastores/{dataStoreName}/featuretypes/{featureTypeName}", content);
        }

        /// <summary>
        /// Deletes a feature type
        /// </summary>
        /// <param name="workspaceName">Name of the workspace</param>
        /// <param name="dataStoreName">Name of the data store</param>
        /// <param name="featureTypeName">Name of the feature type to delete</param>
        /// <param name="recurse">Whether to recursively delete all resources associated with the feature type</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task DeleteFeatureTypeAsync(string workspaceName, string dataStoreName, string featureTypeName, bool recurse = false)
        {
            var path = $"/rest/workspaces/{workspaceName}/datastores/{dataStoreName}/featuretypes/{featureTypeName}?recurse={recurse.ToString().ToLower()}";
            await _httpClient.DeleteAsync(path);
        }
    }
}
