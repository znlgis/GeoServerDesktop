using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoServerDesktop.App.Services;

namespace GeoServerDesktop.App.ViewModels
{
    /// <summary>
    /// 欢迎页仪表盘视图模型：连接状态、GeoServer 版本、工作空间/图层计数速览。
    /// 订阅连接状态变化：连接建立后自动刷新，断开时复位。
    /// </summary>
    public partial class DashboardViewModel : ViewModelBase
    {
        /// <summary>
        /// 数值未加载时的占位文本
        /// </summary>
        public const string ValuePlaceholder = "—";

        private readonly IGeoServerConnectionService _connectionService;

        /// <summary>
        /// 是否已连接到 GeoServer
        /// </summary>
        [ObservableProperty]
        private bool _isConnected;

        /// <summary>
        /// 当前连接的服务器地址
        /// </summary>
        [ObservableProperty]
        private string _serverUrl = string.Empty;

        /// <summary>
        /// GeoServer 版本（未加载时为占位文本）
        /// </summary>
        [ObservableProperty]
        private string _geoServerVersion = ValuePlaceholder;

        /// <summary>
        /// 工作空间数量（未加载时为 null）
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(WorkspaceCountText))]
        private int? _workspaceCount;

        /// <summary>
        /// 图层数量（未加载时为 null）
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LayerCountText))]
        private int? _layerCount;

        /// <summary>
        /// 状态消息
        /// </summary>
        [ObservableProperty]
        private string _statusMessage = string.Empty;

        /// <summary>
        /// 是否正在加载
        /// </summary>
        [ObservableProperty]
        private bool _isLoading;

        /// <summary>
        /// 工作空间数速览文本（未加载时显示占位文本）
        /// </summary>
        public string WorkspaceCountText => WorkspaceCount?.ToString() ?? ValuePlaceholder;

        /// <summary>
        /// 图层数速览文本（未加载时显示占位文本）
        /// </summary>
        public string LayerCountText => LayerCount?.ToString() ?? ValuePlaceholder;

        /// <summary>
        /// 初始化 DashboardViewModel 类的新实例
        /// </summary>
        /// <param name="connectionService">GeoServer 连接服务</param>
        public DashboardViewModel(IGeoServerConnectionService connectionService)
        {
            _connectionService = connectionService;
            _connectionService.ConnectionStatusChanged += OnConnectionStatusChanged;

            IsConnected = connectionService.IsConnected;
            ServerUrl = connectionService.CurrentOptions?.BaseUrl ?? string.Empty;
            StatusMessage = L.StatusNotConnected;
        }

        /// <summary>
        /// 连接状态变化：同步状态；连接建立后自动刷新速览，断开时复位。
        /// </summary>
        /// <param name="sender">事件源</param>
        /// <param name="isConnected">当前是否已连接</param>
        private void OnConnectionStatusChanged(object? sender, bool isConnected)
        {
            IsConnected = isConnected;
            ServerUrl = _connectionService.CurrentOptions?.BaseUrl ?? string.Empty;

            if (isConnected)
            {
                _ = RefreshAsync(); // 即发即忘；异常已在 RefreshAsync 内部处理
            }
            else
            {
                GeoServerVersion = ValuePlaceholder;
                WorkspaceCount = null;
                LayerCount = null;
                IsLoading = false;
                StatusMessage = L.StatusNotConnected;
            }
        }

        /// <summary>
        /// 刷新仪表盘速览：GeoServer 版本 + 工作空间/图层计数
        /// </summary>
        [RelayCommand]
        private async Task RefreshAsync()
        {
            if (!_connectionService.IsConnected)
            {
                StatusMessage = L.StatusNotConnected;
                return;
            }

            IsLoading = true;
            StatusMessage = L.StatusDashboardLoading;

            try
            {
                // 版本：单独容错，失败仅回落占位文本，不阻断计数加载
                try
                {
                    var versionInfo = await _connectionService.GetAboutService().GetVersionAsync();
                    var resource = versionInfo?.About?.Resources?.FirstOrDefault(r => r.Name == "GeoServer");
                    var version = resource?.Version;
                    GeoServerVersion = string.IsNullOrEmpty(version) ? ValuePlaceholder : version;
                }
                catch
                {
                    GeoServerVersion = ValuePlaceholder;
                }

                var workspaces = await _connectionService.GetWorkspaceService().GetWorkspacesAsync();
                WorkspaceCount = workspaces?.Length ?? 0;

                var layers = await _connectionService.GetLayerService().GetLayersAsync();
                LayerCount = layers?.Length ?? 0;

                StatusMessage = L.StatusDashboardLoaded;
            }
            catch (Exception ex)
            {
                // 连接已失效（如探测失败后的 Disconnect）时不显示加载错误，交回断开复位语义
                StatusMessage = _connectionService.IsConnected
                    ? string.Format(L.StatusDashboardLoadFailed, ex.Message)
                    : L.StatusNotConnected;
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
