using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using GeoServerDesktop.App.ViewModels;
using GeoServerDesktop.App.Views;
using Microsoft.Extensions.DependencyInjection;

namespace GeoServerDesktop.App;

/// <summary>
/// 应用程序主类
/// </summary>
public partial class App : Application
{
    /// <summary>应用服务容器（M5 组合根）</summary>
    private System.IServiceProvider? _services;

    /// <summary>
    /// 初始化应用程序
    /// </summary>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// 当框架初始化完成时调用
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 避免 Avalonia 和 CommunityToolkit 的重复验证
            // 更多信息: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            // M5：组合根容器构建与主视图模型解析（子 VM 由容器按首次访问惰性取出）
            _services = Composition.AppServices.BuildDefault();
            desktop.MainWindow = new MainWindow
            {
                DataContext = _services.GetRequiredService<MainWindowViewModel>(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// 禁用 Avalonia 数据注解验证
    /// </summary>
    private void DisableAvaloniaDataAnnotationValidation()
    {
        // 获取需要移除的插件数组
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // 移除找到的每个条目
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
