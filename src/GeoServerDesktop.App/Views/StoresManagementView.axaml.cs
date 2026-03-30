using Avalonia.Controls;
using Avalonia.Interactivity;
using GeoServerDesktop.App.ViewModels;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.App.Views
{
    /// <summary>
    /// 数据存储管理视图
    /// </summary>
    public partial class StoresManagementView : UserControl
    {
        /// <summary>
        /// 初始化 StoresManagementView 类的新实例
        /// </summary>
        public StoresManagementView()
        {
            InitializeComponent();

            // 当 DataContext 设置时初始化
            this.DataContextChanged += async (s, e) =>
            {
                if (DataContext is StoresManagementViewModel viewModel)
                {
                    try
                    {
                        await viewModel.InitializeAsync();
                    }
                    catch
                    {
                        // 错误已在 ViewModel 中处理
                    }
                }
            };
        }

        /// <summary>
        /// 删除数据存储按钮点击事件处理程序
        /// </summary>
        private async void OnDeleteDataStoreClick(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button &&
                button.Tag is DataStore dataStore &&
                DataContext is StoresManagementViewModel viewModel)
            {
                viewModel.SelectedDataStore = dataStore;
                await viewModel.DeleteDataStoreCommand.ExecuteAsync(null);
            }
        }
    }
}
