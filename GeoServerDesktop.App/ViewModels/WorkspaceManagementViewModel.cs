using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoServerDesktop.App.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace GeoServerDesktop.App.ViewModels
{
    /// <summary>
    /// ViewModel for workspace management
    /// </summary>
    public partial class WorkspaceManagementViewModel : ViewModelBase
    {
        private readonly IGeoServerConnectionService _connectionService;

        [ObservableProperty]
        private ObservableCollection<string> _workspaces = new();

        [ObservableProperty]
        private string? _selectedWorkspace;

        [ObservableProperty]
        private string _newWorkspaceName = string.Empty;

        [ObservableProperty]
        private string _statusMessage = "Ready";

        [ObservableProperty]
        private bool _isLoading;

        public WorkspaceManagementViewModel(IGeoServerConnectionService connectionService)
        {
            _connectionService = connectionService;
        }

        /// <summary>
        /// Loads the list of workspaces
        /// </summary>
        [RelayCommand]
        private async Task LoadWorkspacesAsync()
        {
            if (!_connectionService.IsConnected)
            {
                StatusMessage = "Not connected to GeoServer";
                return;
            }

            IsLoading = true;
            StatusMessage = "Loading workspaces...";

            try
            {
                var workspaceService = _connectionService.GetWorkspaceService();
                var workspaceList = await workspaceService.GetWorkspacesAsync();

                Workspaces.Clear();
                foreach (var workspace in workspaceList)
                {
                    Workspaces.Add(workspace.Name);
                }

                StatusMessage = $"Loaded {Workspaces.Count} workspaces";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to load workspaces: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Creates a new workspace
        /// </summary>
        [RelayCommand]
        private async Task CreateWorkspaceAsync()
        {
            if (string.IsNullOrWhiteSpace(NewWorkspaceName))
            {
                StatusMessage = "Workspace name is required";
                return;
            }

            IsLoading = true;
            StatusMessage = $"Creating workspace '{NewWorkspaceName}'...";

            try
            {
                var workspaceService = _connectionService.GetWorkspaceService();
                await workspaceService.CreateWorkspaceAsync(NewWorkspaceName);

                StatusMessage = $"Workspace '{NewWorkspaceName}' created successfully";
                NewWorkspaceName = string.Empty;
                
                // Reload workspaces
                await LoadWorkspacesAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to create workspace: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Deletes the selected workspace
        /// </summary>
        [RelayCommand]
        private async Task DeleteWorkspaceAsync()
        {
            if (string.IsNullOrWhiteSpace(SelectedWorkspace))
            {
                StatusMessage = "No workspace selected";
                return;
            }

            IsLoading = true;
            StatusMessage = $"Deleting workspace '{SelectedWorkspace}'...";

            try
            {
                var workspaceService = _connectionService.GetWorkspaceService();
                await workspaceService.DeleteWorkspaceAsync(SelectedWorkspace, recurse: false);

                StatusMessage = $"Workspace '{SelectedWorkspace}' deleted successfully";
                SelectedWorkspace = null;
                
                // Reload workspaces
                await LoadWorkspacesAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to delete workspace: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
