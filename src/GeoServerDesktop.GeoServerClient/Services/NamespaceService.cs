using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing GeoServer namespaces
    /// </summary>
    public class NamespaceService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the NamespaceService class
        /// </summary>
        /// <param name="httpClient">HTTP client for GeoServer operations</param>
        public NamespaceService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets a list of all namespaces
        /// </summary>
        /// <returns>Array of namespaces</returns>
        public async Task<Namespace[]> GetNamespacesAsync()
        {
            var response = await _httpClient.GetAsync("/rest/namespaces.json");
            var wrapper = JsonConvert.DeserializeObject<NamespaceListWrapper>(response);
            return wrapper?.NamespaceList?.Namespaces ?? Array.Empty<Namespace>();
        }

        /// <summary>
        /// Gets details for a specific namespace
        /// </summary>
        /// <param name="namespacePrefix">Prefix of the namespace</param>
        /// <returns>Namespace details</returns>
        public async Task<Namespace> GetNamespaceAsync(string namespacePrefix)
        {
            var response = await _httpClient.GetAsync($"/rest/namespaces/{namespacePrefix}.json");
            var wrapper = JsonConvert.DeserializeObject<NamespaceWrapper>(response);
            return wrapper?.Namespace;
        }

        /// <summary>
        /// Creates a new namespace
        /// </summary>
        /// <param name="namespacePrefix">Prefix for the namespace</param>
        /// <param name="uri">URI for the namespace</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task CreateNamespaceAsync(string namespacePrefix, string uri)
        {
            var ns = new { @namespace = new { prefix = namespacePrefix, uri = uri } };
            var json = JsonConvert.SerializeObject(ns);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync("/rest/namespaces", content);
        }

        /// <summary>
        /// Updates an existing namespace
        /// </summary>
        /// <param name="namespacePrefix">Prefix of the namespace to update</param>
        /// <param name="uri">New URI for the namespace</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task UpdateNamespaceAsync(string namespacePrefix, string uri)
        {
            var ns = new { @namespace = new { prefix = namespacePrefix, uri = uri } };
            var json = JsonConvert.SerializeObject(ns);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync($"/rest/namespaces/{namespacePrefix}", content);
        }

        /// <summary>
        /// Deletes a namespace
        /// </summary>
        /// <param name="namespacePrefix">Prefix of the namespace to delete</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task DeleteNamespaceAsync(string namespacePrefix)
        {
            await _httpClient.DeleteAsync($"/rest/namespaces/{namespacePrefix}");
        }
    }
}
