using CommunityToolkit.Mvvm.ComponentModel;
using GeoServerDesktop.App.Services;

namespace GeoServerDesktop.App.ViewModels;

/// <summary>
/// 所有视图模型的基类
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
    /// <summary>
    /// 本地化服务，提供中英文字符串
    /// </summary>
    public LocalizationService L => LocalizationService.Instance;
}
