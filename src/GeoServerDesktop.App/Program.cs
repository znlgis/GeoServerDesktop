using System;
using Avalonia;

namespace GeoServerDesktop.App;

/// <summary>
/// 程序入口类
/// </summary>
sealed class Program
{
    /// <summary>
    /// 初始化代码。在调用 Main 方法之前不要使用任何 Avalonia、第三方 API 或任何依赖于 SynchronizationContext 的代码：
    /// 因为这些内容还没有初始化，可能会导致程序崩溃。
    /// </summary>
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    /// <summary>
    /// Avalonia 配置，不要删除；同时也被可视化设计器使用。
    /// </summary>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
