using System;
using System.Collections.Generic;

namespace GeoServerDesktop.GeoServerClient.Models
{
    /// <summary>
    /// 批量操作目标（M4）。图层/存储用 qualified 名（workspace:name），样式用裸名（工作空间样式用
    /// workspace/name 形态前缀区分，见 <see cref="IsWorkspaceStyle"/>），工作空间用裸名。
    /// </summary>
    public sealed class BatchTarget
    {
        /// <summary>工作空间名（图层/存储必填；全局样式与工作空间目标可空）。</summary>
        public string Workspace { get; set; }

        /// <summary>资源名（图层/存储为对象名；样式为裸名；工作空间目标即工作空间名）。</summary>
        public string Name { get; set; }

        /// <summary>
        /// 工作空间样式的存储形态前缀（"workspace/"）：样式名带该前缀时表示工作空间样式，
        /// Name 为去掉前缀后的样式名。
        /// </summary>
        public const string WorkspaceStylePrefix = "workspace/";

        /// <summary>是否工作空间样式目标（Name 带 workspace/ 前缀）。</summary>
        public bool IsWorkspaceStyle
        {
            get
            {
                return Name != null && Name.StartsWith(WorkspaceStylePrefix, StringComparison.Ordinal);
            }
        }

        /// <summary>样式裸名（去除 workspace/ 前缀后的名字）。</summary>
        public string StyleName
        {
            get
            {
                if (Name == null) return null;
                return IsWorkspaceStyle ? Name.Substring(WorkspaceStylePrefix.Length) : Name;
            }
        }

        /// <summary>qualified 展示名（workspace:name；无工作空间时即名字）。</summary>
        public string QualifiedName
        {
            get { return string.IsNullOrEmpty(Workspace) ? Name : Workspace + ":" + Name; }
        }

        /// <summary>创建全局样式目标。</summary>
        /// <param name="styleName">样式名。</param>
        /// <returns>批量目标。</returns>
        public static BatchTarget GlobalStyle(string styleName)
        {
            return new BatchTarget { Name = styleName };
        }

        /// <summary>创建工作空间样式目标。</summary>
        /// <param name="workspaceName">工作空间名。</param>
        /// <param name="styleName">样式名。</param>
        /// <returns>批量目标。</returns>
        public static BatchTarget WorkspaceStyle(string workspaceName, string styleName)
        {
            return new BatchTarget { Workspace = workspaceName, Name = WorkspaceStylePrefix + styleName };
        }

        /// <summary>创建工作空间目标。</summary>
        /// <param name="workspaceName">工作空间名。</param>
        /// <returns>批量目标。</returns>
        public static BatchTarget ForWorkspace(string workspaceName)
        {
            return new BatchTarget { Name = workspaceName };
        }
    }

    /// <summary>批量操作单项结果。</summary>
    public sealed class BatchItemResult
    {
        /// <summary>目标展示名（qualified）。</summary>
        public string Target { get; set; }

        /// <summary>是否成功。</summary>
        public bool Success { get; set; }

        /// <summary>说明（失败原因或成功提示）。</summary>
        public string Message { get; set; }

        /// <summary>构造成功结果。</summary>
        /// <param name="target">目标展示名。</param>
        /// <returns>成功结果。</returns>
        public static BatchItemResult Ok(string target)
        {
            return new BatchItemResult { Target = target, Success = true };
        }

        /// <summary>构造失败结果。</summary>
        /// <param name="target">目标展示名。</param>
        /// <param name="message">失败说明。</param>
        /// <returns>失败结果。</returns>
        public static BatchItemResult Fail(string target, string message)
        {
            return new BatchItemResult { Target = target, Success = false, Message = message };
        }
    }

    /// <summary>批量操作聚合结果：逐项成败 + 计数（部分成功语义，单项失败不中断整批）。</summary>
    public sealed class BatchResult
    {
        /// <summary>逐项结果（顺序与输入一致）。</summary>
        public List<BatchItemResult> Items { get; set; } = new List<BatchItemResult>();

        /// <summary>目标总数。</summary>
        public int Total { get { return Items.Count; } }

        /// <summary>成功数。</summary>
        public int Succeeded { get { return Count(true); } }

        /// <summary>失败数。</summary>
        public int Failed { get { return Count(false); } }

        /// <summary>是否全部成功（空集合视为成功）。</summary>
        public bool AllSucceeded { get { return Failed == 0; } }

        /// <summary>失败项摘要（"target: message" 逐行）。</summary>
        public string FailureSummary
        {
            get
            {
                var lines = new List<string>();
                foreach (var item in Items)
                {
                    if (!item.Success) lines.Add(item.Target + ": " + item.Message);
                }
                return string.Join("; ", lines.ToArray());
            }
        }

        private int Count(bool success)
        {
            int n = 0;
            foreach (var item in Items)
            {
                if (item.Success == success) n++;
            }
            return n;
        }
    }
}
