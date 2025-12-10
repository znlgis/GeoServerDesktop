using Avalonia.Controls;
using GeoServerDesktop.App.ViewModels;

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
            this.DataContextChanged += (s, e) =>
            {
                if (DataContext is StoresManagementViewModel viewModel)
                {
                    _ = viewModel.InitializeAsync();
                }
            };
        }
    }
}
