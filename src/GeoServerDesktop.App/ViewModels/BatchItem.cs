using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.App.ViewModels
{
    /// <summary>
    /// 批量操作条目展示包装（M4）：多选勾选状态 + 类型化目标构造。
    /// 图层/存储/工作空间样式 Display 为 qualified 名（workspace:name）；全局样式 Display 即裸名。
    /// </summary>
    public sealed class BatchItem : ViewModelBase
    {
        private bool _isSelected;

        /// <summary>展示名（qualified）</summary>
        public string Display { get; }

        /// <summary>所属工作空间（全局样式为空）</summary>
        public string Workspace { get; }

        /// <summary>对象名</summary>
        public string Name { get; }

        /// <summary>是否工作空间样式</summary>
        public bool IsWorkspaceStyle { get; }

        /// <summary>是否选中</summary>
        public bool IsSelected
        {
            get { return _isSelected; }
            set { SetProperty(ref _isSelected, value); }
        }

        /// <summary>
        /// 初始化 BatchItem 类的新实例
        /// </summary>
        public BatchItem(string display, string workspace, string name, bool isWorkspaceStyle = false)
        {
            Display = display;
            Workspace = workspace;
            Name = name;
            IsWorkspaceStyle = isWorkspaceStyle;
        }

        /// <summary>图层目标（qualified 名）。</summary>
        public string QualifiedLayer { get { return Display; } }

        /// <summary>样式批量目标（全局裸名 / 工作空间带 workspace/ 前缀）。</summary>
        public BatchTarget ToStyleTarget()
        {
            return IsWorkspaceStyle
                ? BatchTarget.WorkspaceStyle(Workspace, Name)
                : BatchTarget.GlobalStyle(Name);
        }

        /// <summary>存储批量目标。</summary>
        public BatchTarget ToStoreTarget()
        {
            return new BatchTarget { Workspace = Workspace, Name = Name };
        }
    }
}
