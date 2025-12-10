using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoServerDesktop.App.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

namespace GeoServerDesktop.App.ViewModels
{
    /// <summary>
    /// 样式管理的视图模型
    /// </summary>
    public partial class StyleManagementViewModel : ViewModelBase
    {
        private readonly IGeoServerConnectionService _connectionService;

        /// <summary>
        /// 样式列表
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<string> _styles = new();

        /// <summary>
        /// 选中的样式
        /// </summary>
        [ObservableProperty]
        private string? _selectedStyle;

        /// <summary>
        /// 新样式名称
        /// </summary>
        [ObservableProperty]
        private string _newStyleName = string.Empty;

        /// <summary>
        /// SLD 内容
        /// </summary>
        [ObservableProperty]
        private string _sldContent = string.Empty;

        /// <summary>
        /// 状态消息
        /// </summary>
        [ObservableProperty]
        private string _statusMessage = "Ready";

        /// <summary>
        /// 是否正在加载
        /// </summary>
        [ObservableProperty]
        private bool _isLoading;

        /// <summary>
        /// 初始化 StyleManagementViewModel 类的新实例
        /// </summary>
        /// <param name="connectionService">GeoServer 连接服务</param>
        public StyleManagementViewModel(IGeoServerConnectionService connectionService)
        {
            _connectionService = connectionService;
        }

        /// <summary>
        /// 加载样式列表
        /// </summary>
        [RelayCommand]
        private async Task LoadStylesAsync()
        {
            if (!_connectionService.IsConnected)
            {
                StatusMessage = "Not connected to GeoServer";
                return;
            }

            IsLoading = true;
            StatusMessage = "Loading styles...";

            try
            {
                var styleService = _connectionService.GetStyleService();
                var styleList = await styleService.GetStylesAsync();

                Styles.Clear();
                foreach (var style in styleList)
                {
                    Styles.Add(style.Name);
                }

                StatusMessage = $"Loaded {Styles.Count} styles";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to load styles: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 加载选中样式的 SLD 内容
        /// </summary>
        [RelayCommand]
        private async Task LoadStyleContentAsync()
        {
            if (string.IsNullOrWhiteSpace(SelectedStyle))
            {
                StatusMessage = "No style selected";
                return;
            }

            IsLoading = true;
            StatusMessage = $"Loading style '{SelectedStyle}'...";

            try
            {
                var styleService = _connectionService.GetStyleService();
                var sld = await styleService.GetStyleSldAsync(SelectedStyle);

                SldContent = sld;
                StatusMessage = $"Loaded SLD for '{SelectedStyle}'";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to load style: {ex.Message}";
                SldContent = string.Empty;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 上传新样式或更新现有样式
        /// </summary>
        [RelayCommand]
        private async Task UploadStyleAsync()
        {
            if (string.IsNullOrWhiteSpace(NewStyleName))
            {
                StatusMessage = "Style name is required";
                return;
            }

            if (string.IsNullOrWhiteSpace(SldContent))
            {
                StatusMessage = "SLD content is required";
                return;
            }

            IsLoading = true;
            StatusMessage = $"Uploading style '{NewStyleName}'...";

            try
            {
                var styleService = _connectionService.GetStyleService();
                
                // 检查样式是否存在
                var existingStyles = await styleService.GetStylesAsync();
                bool styleExists = false;
                foreach (var style in existingStyles)
                {
                    if (style.Name == NewStyleName)
                    {
                        styleExists = true;
                        break;
                    }
                }

                if (styleExists)
                {
                    // 更新现有样式
                    await styleService.UpdateStyleAsync(NewStyleName, SldContent);
                    StatusMessage = $"Style '{NewStyleName}' updated successfully";
                }
                else
                {
                    // 创建新样式
                    await styleService.CreateStyleAsync(NewStyleName, SldContent);
                    StatusMessage = $"Style '{NewStyleName}' created successfully";
                }

                // 重新加载样式
                await LoadStylesAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to upload style: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 删除选中的样式
        /// </summary>
        [RelayCommand]
        private async Task DeleteStyleAsync()
        {
            if (string.IsNullOrWhiteSpace(SelectedStyle))
            {
                StatusMessage = "No style selected";
                return;
            }

            IsLoading = true;
            StatusMessage = $"Deleting style '{SelectedStyle}'...";

            try
            {
                var styleService = _connectionService.GetStyleService();
                await styleService.DeleteStyleAsync(SelectedStyle);

                StatusMessage = $"Style '{SelectedStyle}' deleted successfully";
                SelectedStyle = null;
                SldContent = string.Empty;
                
                // 重新加载样式
                await LoadStylesAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to delete style: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 创建示例 SLD 用于测试
        /// </summary>
        [RelayCommand]
        private void CreateSampleSld()
        {
            SldContent = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<StyledLayerDescriptor version=""1.0.0"" 
    xmlns=""http://www.opengis.net/sld"" 
    xmlns:ogc=""http://www.opengis.net/ogc"">
  <NamedLayer>
    <Name>Sample Style</Name>
    <UserStyle>
      <Title>Sample Point Style</Title>
      <FeatureTypeStyle>
        <Rule>
          <PointSymbolizer>
            <Graphic>
              <Mark>
                <WellKnownName>circle</WellKnownName>
                <Fill>
                  <CssParameter name=""fill"">#FF0000</CssParameter>
                </Fill>
              </Mark>
              <Size>6</Size>
            </Graphic>
          </PointSymbolizer>
        </Rule>
      </FeatureTypeStyle>
    </UserStyle>
  </NamedLayer>
</StyledLayerDescriptor>";
            StatusMessage = "Sample SLD created";
        }
    }
}
