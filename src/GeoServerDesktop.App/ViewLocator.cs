using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using GeoServerDesktop.App.ViewModels;

namespace GeoServerDesktop.App;

/// <summary>
/// 根据视图模型返回相应的视图（如果可能）
/// </summary>
[RequiresUnreferencedCode(
    "Default implementation of ViewLocator involves reflection which may be trimmed away.",
    Url = "https://docs.avaloniaui.net/docs/concepts/view-locator")]
public class ViewLocator : IDataTemplate
{
    /// <summary>
    /// 根据视图模型构建视图
    /// </summary>
    /// <param name="param">视图模型实例</param>
    /// <returns>对应的视图控件</returns>
    public Control? Build(object? param)
    {
        if (param is null)
            return null;
        
        var name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        var type = Type.GetType(name);

        if (type != null)
        {
            return (Control)Activator.CreateInstance(type)!;
        }
        
        return new TextBlock { Text = "Not Found: " + name };
    }

    /// <summary>
    /// 检查数据是否匹配此模板
    /// </summary>
    /// <param name="data">要检查的数据</param>
    /// <returns>如果数据是 ViewModelBase 类型则返回 true</returns>
    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}
