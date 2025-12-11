using Avalonia.Controls;
using GeoServerDesktop.App.ViewModels;

namespace GeoServerDesktop.App.Views
{
    /// <summary>
    /// 关于和系统状态视图
    /// </summary>
    public partial class AboutView : UserControl
    {
        /// <summary>
        /// 初始化 AboutView 类的新实例
        /// </summary>
        public AboutView()
        {
            InitializeComponent();
            
            // 当 DataContext 设置时初始化
            this.DataContextChanged += async (s, e) =>
            {
                if (DataContext is AboutViewModel viewModel)
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
    }
}
