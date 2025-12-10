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
    /// ViewModel for style management
    /// </summary>
    public partial class StyleManagementViewModel : ViewModelBase
    {
        private readonly IGeoServerConnectionService _connectionService;

        [ObservableProperty]
        private ObservableCollection<string> _styles = new();

        [ObservableProperty]
        private string? _selectedStyle;

        [ObservableProperty]
        private string _newStyleName = string.Empty;

        [ObservableProperty]
        private string _sldContent = string.Empty;

        [ObservableProperty]
        private string _statusMessage = "Ready";

        [ObservableProperty]
        private bool _isLoading;

        public StyleManagementViewModel(IGeoServerConnectionService connectionService)
        {
            _connectionService = connectionService;
        }

        /// <summary>
        /// Loads the list of styles
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
        /// Loads the SLD content for the selected style
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
        /// Uploads a new style or updates an existing one
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
                
                // Check if style exists
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
                    // Update existing style
                    await styleService.UpdateStyleAsync(NewStyleName, SldContent);
                    StatusMessage = $"Style '{NewStyleName}' updated successfully";
                }
                else
                {
                    // Create new style
                    await styleService.CreateStyleAsync(NewStyleName, SldContent);
                    StatusMessage = $"Style '{NewStyleName}' created successfully";
                }

                // Reload styles
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
        /// Deletes the selected style
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
                
                // Reload styles
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
        /// Creates a sample SLD for testing
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
