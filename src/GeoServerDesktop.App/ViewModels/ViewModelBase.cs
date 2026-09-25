using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using GeoServerDesktop.App.Services;

namespace GeoServerDesktop.App.ViewModels;

/// <summary>
/// 所有视图模型的基类（M5 架构收敛：集中 Loading / Status / 连接守卫 / 加载管道样板）。
///
/// 约定：
/// ① <see cref="IsLoading"/> 与 <see cref="StatusMessage"/> 由基类统一提供（原先 23+25 处
///    <c>[ObservableProperty]</c> 重复声明的收敛点），视图绑定与既有命令语义不变；
/// ② 需要访问 GeoServer 的子 VM 通过 <c>: base(connectionService)</c> 上交连接服务，
///    命令入口统一用 <see cref="HasConnection"/> 做守卫（未连接时给出本地化提示）；
/// ③ 新增命令请优先使用 <see cref="RunGuardedAsync"/> / <see cref="RunConnectedGuardedAsync"/>，
///    不要再手写 IsLoading + try/catch/finally 三段式。
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
    /// <summary>
    /// 无连接依赖的视图模型构造（地图预览、主窗口等）
    /// </summary>
    protected ViewModelBase()
    {
    }

    /// <summary>
    /// 带连接依赖的视图模型构造
    /// </summary>
    /// <param name="connectionService">GeoServer 连接服务</param>
    protected ViewModelBase(IGeoServerConnectionService connectionService)
    {
        Connection = connectionService;
    }

    /// <summary>
    /// 本地化服务，提供中英文字符串
    /// </summary>
    public LocalizationService L => LocalizationService.Instance;

    /// <summary>连接服务（无连接依赖的子 VM 为 null）</summary>
    protected IGeoServerConnectionService? Connection { get; }

    private bool _isLoading;

    /// <summary>是否正在加载（基类统一提供）</summary>
    public bool IsLoading
    {
        get { return _isLoading; }
        set { SetProperty(ref _isLoading, value); }
    }

    private string _statusMessage = string.Empty;

    /// <summary>状态消息（基类统一提供）</summary>
    public string StatusMessage
    {
        get { return _statusMessage; }
        set { SetProperty(ref _statusMessage, value); }
    }

    /// <summary>
    /// 连接守卫：已连接返回 true；否则写入未连接提示并返回 false。
    /// </summary>
    /// <param name="notConnectedMessage">未连接提示文案（缺省用导航级"请先连接"）。</param>
    /// <returns>是否可以继续执行需要连接的逻辑。</returns>
    protected bool HasConnection(string? notConnectedMessage = null)
    {
        if (Connection != null && Connection.IsConnected) return true;
        StatusMessage = notConnectedMessage ?? L.StatusPleaseConnect;
        return false;
    }

    /// <summary>
    /// 加载管道：置 IsLoading → 执行工作 → 异常转为状态消息 → 复位 IsLoading。
    /// </summary>
    /// <param name="work">工作体。</param>
    /// <param name="describeError">异常到状态消息的格式化。</param>
    /// <returns>表示异步操作的任务。</returns>
    protected async Task RunGuardedAsync(Func<Task> work, Func<Exception, string> describeError)
    {
        if (work == null) throw new ArgumentNullException(nameof(work));
        if (describeError == null) throw new ArgumentNullException(nameof(describeError));

        IsLoading = true;
        try
        {
            await work();
        }
        catch (Exception ex)
        {
            StatusMessage = describeError(ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 连接守卫 + 加载管道：未连接时只给提示、不进入 loading 态。
    /// </summary>
    /// <param name="work">工作体。</param>
    /// <param name="describeError">异常到状态消息的格式化。</param>
    /// <returns>表示异步操作的任务。</returns>
    protected Task RunConnectedGuardedAsync(Func<Task> work, Func<Exception, string> describeError)
    {
        return HasConnection() ? RunGuardedAsync(work, describeError) : Task.CompletedTask;
    }
}
