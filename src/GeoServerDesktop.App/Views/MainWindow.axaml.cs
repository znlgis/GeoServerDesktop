using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace GeoServerDesktop.App.Views;

/// <summary>
/// 主窗口
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// 初始化 MainWindow 类的新实例
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        // M4：工作空间迁移的文件对话框经视图代码后台挂接（ViewModel 保持无头可测）
        if (DataContext is ViewModels.MainWindowViewModel vm)
        {
            vm.BindFilePickers(this);
        }
        DataContextChanged += (_, _) =>
        {
            if (DataContext is ViewModels.MainWindowViewModel vm2)
            {
                vm2.BindFilePickers(this);
            }
        };
    }

    /// <summary>
    /// 保存文件对话框（取消返回 null）
    /// </summary>
    /// <param name="description">文件类型描述</param>
    /// <param name="defaultFileName">默认文件名</param>
    /// <returns>选中路径或 null</returns>
    public async Task<string?> PickSavePathAsync(string description, string defaultFileName)
    {
        try
        {
            var provider = StorageProvider;
            var suggested = defaultFileName;
            var file = await provider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save",
                SuggestedFileName = suggested,
                FileTypeChoices = new List<FilePickerFileType>
                {
                    new(description) { Patterns = new List<string> { "*" + System.IO.Path.GetExtension(defaultFileName) } },
                },
            });
            return file?.Path.LocalPath;
        }
        catch (Exception)
        {
            // 平台无原生对话框（如无头环境）时静默取消
            return null;
        }
    }

    /// <summary>
    /// 打开文件对话框（取消返回 null）
    /// </summary>
    /// <param name="description">文件类型描述</param>
    /// <returns>选中路径或 null</returns>
    public async Task<string?> PickOpenPathAsync(string description)
    {
        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open",
                AllowMultiple = false,
                FileTypeFilter = new List<FilePickerFileType>
                {
                    new(description) { Patterns = new List<string> { "*.gdws.zip", "*.zip" } },
                },
            });
            return files.Count > 0 ? files[0].Path.LocalPath : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
