using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapsui;
using GeoServerLayer = GeoServerDesktop.GeoServerClient.Models.Layer;

namespace GeoServerDesktop.App.ViewModels
{
    /// <summary>
    /// 地图预览功能的视图模型
    /// </summary>
    public partial class MapPreviewViewModel : ViewModelBase
    {
        /// <summary>
        /// 地图实例
        /// </summary>
        [ObservableProperty]
        private Map? _map;

        /// <summary>
        /// 状态消息
        /// </summary>
        [ObservableProperty]
        private string _statusMessage = "Map preview ready. WMS layer integration available.";

        /// <summary>
        /// 是否正在加载
        /// </summary>
        [ObservableProperty]
        private bool _isLoading;

        /// <summary>
        /// 当前图层
        /// </summary>
        [ObservableProperty]
        private GeoServerLayer? _currentLayer;

        /// <summary>
        /// 当前工作空间
        /// </summary>
        [ObservableProperty]
        private string? _currentWorkspace;

        /// <summary>
        /// GeoServer 基础 URL
        /// </summary>
        [ObservableProperty]
        private string? _baseUrl;

        /// <summary>
        /// 预览 URL
        /// </summary>
        [ObservableProperty]
        private string? _previewUrl;

        /// <summary>
        /// 初始化 MapPreviewViewModel 类的新实例
        /// </summary>
        public MapPreviewViewModel()
        {
            InitializeMap();
        }

        /// <summary>
        /// 使用默认设置初始化地图
        /// </summary>
        private void InitializeMap()
        {
            Map = new Map();
            StatusMessage = "Map initialized. Use LoadWmsLayerAsync to add layers.";
        }

        /// <summary>
        /// 加载 WMS 图层进行预览
        /// </summary>
        /// <param name="geoServerBaseUrl">GeoServer 基础 URL</param>
        /// <param name="workspace">工作空间名称</param>
        /// <param name="layerName">图层名称</param>
        public async Task LoadWmsLayerAsync(string geoServerBaseUrl, string workspace, string layerName)
        {
            IsLoading = true;
            StatusMessage = $"Preparing layer {workspace}:{layerName}...";

            try
            {
                BaseUrl = geoServerBaseUrl;
                CurrentWorkspace = workspace;

                var layerFullName = string.IsNullOrWhiteSpace(workspace) ? layerName : $"{workspace}:{layerName}";

                // 生成 WMS 预览 URL
                var wmsUrl = $"{geoServerBaseUrl.TrimEnd('/')}/wms";
                var bbox = "-180,-90,180,90"; // 世界范围
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
        /// 清除当前图层预览
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
        /// 重置地图视图
        /// </summary>
        [RelayCommand]
        private void ZoomToExtent()
        {
            StatusMessage = "Map ready for WMS layer preview";
        }
    }
}
