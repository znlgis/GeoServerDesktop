namespace GeoServerDesktop.App.ViewModels
{
    /// <summary>
    /// 占位符视图模型，用于尚未实现的功能
    /// </summary>
    public class PlaceholderViewModel : ViewModelBase
    {
        /// <summary>
        /// 页面标题
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// 页面消息
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// 初始化 PlaceholderViewModel 类的新实例
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="message">消息</param>
        public PlaceholderViewModel(string title, string message)
        {
            Title = title;
            Message = message;
        }
    }
}
