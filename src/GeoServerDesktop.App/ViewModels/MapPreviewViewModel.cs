using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapsui;
using System;
using System.Threading.Tasks;
using GeoServerLayer = GeoServerDesktop.GeoServerClient.Models.Layer;

namespace GeoServerDesktop.App.ViewModels
{
    /// <summary>
    /// ViewModel for map preview functionality
    /// </summary>
    public partial class MapPreviewViewModel : ViewModelBase
    {
        [ObservableProperty]
        private Map? _map;

        [ObservableProperty]
        private string _statusMessage = "Map preview ready. WMS layer integration available.";

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private GeoServerLayer? _currentLayer;

        [ObservableProperty]
        private string? _currentWorkspace;

        [ObservableProperty]
        private string? _baseUrl;

        [ObservableProperty]
        private string? _previewUrl;

        public MapPreviewViewModel()
        {
            InitializeMap();
        }

        /// <summary>
        /// Initializes the map with default settings
        /// </summary>
        private void InitializeMap()
        {
            Map = new Map();
            StatusMessage = "Map initialized. Use LoadWmsLayerAsync to add layers.";
        }

        /// <summary>
        /// Loads a WMS layer for preview
        /// </summary>
        /// <param name="geoServerBaseUrl">GeoServer base URL</param>
        /// <param name="workspace">Workspace name</param>
        /// <param name="layerName">Layer name</param>
        public async Task LoadWmsLayerAsync(string geoServerBaseUrl, string workspace, string layerName)
        {
            IsLoading = true;
            StatusMessage = $"Preparing layer {workspace}:{layerName}...";

            try
            {
                BaseUrl = geoServerBaseUrl;
                CurrentWorkspace = workspace;
                
                var layerFullName = string.IsNullOrWhiteSpace(workspace) ? layerName : $"{workspace}:{layerName}";

                // Generate WMS preview URL
                var wmsUrl = $"{geoServerBaseUrl.TrimEnd('/')}/wms";
                var bbox = "-180,-90,180,90"; // World extent
                var width = 800;
                var height = 600;
                
                PreviewUrl = $"{wmsUrl}?service=WMS&version=1.1.0&request=GetMap&layers={Uri.EscapeDataString(layerFullName)}&srs=EPSG:4326&bbox={bbox}&width={width}&height={height}&format=image/png";

                StatusMessage = $"WMS URL generated for: {layerFullName}. Click 'View WMS URL' to see the preview URL.";

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to generate preview: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Clears the current layer preview
        /// </summary>
        [RelayCommand]
        private void ClearLayers()
        {
            PreviewUrl = null;
            CurrentWorkspace = null;
            BaseUrl = null;
            StatusMessage = "Preview cleared";
        }

        /// <summary>
        /// Resets the map view
        /// </summary>
        [RelayCommand]
        private void ZoomToExtent()
        {
            StatusMessage = "Map ready for WMS layer preview";
        }
    }
}
