using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoServerDesktop.App.Services;

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
        /// 初始化 StyleManagementViewModel 类的新实例
        /// </summary>
        /// <param name="connectionService">GeoServer 连接服务</param>
        public StyleManagementViewModel(IGeoServerConnectionService connectionService)
        : base(connectionService)
        {
            _connectionService = connectionService;
        }

        /// <summary>
        /// 加载样式列表
        /// </summary>
        [RelayCommand]
        private async Task LoadStylesAsync()
        {
            if (!HasConnection(L.StatusNotConnected)) return;

            IsLoading = true;
            StatusMessage = L.StatusLoadingStyles;

            try
            {
                var styleService = _connectionService.GetStyleService();
                var styleList = await styleService.GetStylesAsync();

                Styles.Clear();
                foreach (var style in styleList)
                {
                    Styles.Add(style.Name);
                }

                StatusMessage = string.Format(L.StatusStylesLoaded, Styles.Count);
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(L.StatusStylesLoadFailed, ex.Message);
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
                StatusMessage = L.StatusNoStyleSelected;
                return;
            }

            // FIXED-E36：原硬编码英文串改用本地化键
            IsLoading = true;
            StatusMessage = string.Format(L.StatusLoadingStyleNamed, SelectedStyle);

            try
            {
                var styleService = _connectionService.GetStyleService();
                var sld = await styleService.GetStyleSldAsync(SelectedStyle);

                SldContent = sld;
                StatusMessage = string.Format(L.StatusLoadedSldFor, SelectedStyle);
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(L.StatusStyleLoadFailed, ex.Message);
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
            // FIXED-E36：原硬编码英文串改用本地化键
            if (string.IsNullOrWhiteSpace(NewStyleName))
            {
                StatusMessage = L.StatusStyleNameRequired;
                return;
            }

            if (string.IsNullOrWhiteSpace(SldContent))
            {
                StatusMessage = L.StatusSldContentRequired;
                return;
            }

            IsLoading = true;
            StatusMessage = string.Format(L.StatusUploadingStyleNamed, NewStyleName);

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
                    StatusMessage = string.Format(L.StatusStyleUpdated, NewStyleName);
                }
                else
                {
                    // 创建新样式
                    await styleService.CreateStyleAsync(NewStyleName, SldContent);
                    StatusMessage = string.Format(L.StatusStyleCreated, NewStyleName);
                }

                // 重新加载样式
                await LoadStylesAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(L.StatusStyleUploadFailed, ex.Message);
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
                StatusMessage = L.StatusNoStyleSelected;
                return;
            }

            IsLoading = true;
            StatusMessage = string.Format(L.StatusDeletingStyleNamed, SelectedStyle);

            try
            {
                var styleService = _connectionService.GetStyleService();
                await styleService.DeleteStyleAsync(SelectedStyle);

                StatusMessage = string.Format(L.StatusStyleDeletedNamed, SelectedStyle);
                SelectedStyle = null;
                SldContent = string.Empty;

                // 重新加载样式
                await LoadStylesAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(L.StatusStyleDeleteFailed, ex.Message);
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
            StatusMessage = L.StatusSampleSldCreated;
        }
    }
}
