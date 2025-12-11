using Avalonia.Controls;
using Avalonia.Interactivity;
using GeoServerDesktop.App.ViewModels;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.App.Views
{
    /// <summary>
    /// 图层组管理视图
    /// </summary>
    public partial class LayerGroupsManagementView : UserControl
    {
        /// <summary>
        /// 初始化 LayerGroupsManagementView 类的新实例
        /// </summary>
        public LayerGroupsManagementView()
        {
            InitializeComponent();
            
            // 当控件加载时，初始化 ViewModel
            Loaded += async (s, e) =>
            {
                if (DataContext is LayerGroupsManagementViewModel viewModel)
                {
                    await viewModel.InitializeAsync();
                }
            };
        }

        /// <summary>
        /// 删除图层组按钮点击事件处理程序
        /// </summary>
        private async void OnDeleteLayerGroupClick(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && 
                button.Tag is LayerGroup layerGroup &&
                DataContext is LayerGroupsManagementViewModel viewModel)
            {
                viewModel.SelectedLayerGroup = layerGroup;
                await viewModel.DeleteLayerGroupCommand.ExecuteAsync(null);
            }
        }
    }
}
