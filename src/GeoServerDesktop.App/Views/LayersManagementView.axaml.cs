using Avalonia.Controls;
using Avalonia.Interactivity;
using GeoServerDesktop.App.ViewModels;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.App.Views
{
    /// <summary>
    /// 图层管理视图
    /// </summary>
    public partial class LayersManagementView : UserControl
    {
        /// <summary>
        /// 初始化 LayersManagementView 类的新实例
        /// </summary>
        public LayersManagementView()
        {
            InitializeComponent();
            
            // 当控件加载时，初始化 ViewModel
            Loaded += async (s, e) =>
            {
                if (DataContext is LayersManagementViewModel viewModel)
                {
                    await viewModel.InitializeAsync();
                }
            };
        }

        /// <summary>
        /// 删除图层按钮点击事件处理程序
        /// </summary>
        private async void OnDeleteLayerClick(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && 
                button.Tag is Layer layer &&
                DataContext is LayersManagementViewModel viewModel)
            {
                viewModel.SelectedLayer = layer;
                await viewModel.DeleteLayerCommand.ExecuteAsync(null);
            }
        }
    }
}
