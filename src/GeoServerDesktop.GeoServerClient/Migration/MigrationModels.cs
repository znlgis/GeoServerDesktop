using System.Collections.Generic;

namespace GeoServerDesktop.GeoServerClient.Migration
{
    /// <summary>工作空间导出结果（归档字节 + 清单摘要）。</summary>
    public sealed class WorkspaceExportResult
    {
        /// <summary>归档包字节（ZIP：manifest.json + styles/*.sld）。</summary>
        public byte[] Archive { get; set; }

        /// <summary>归档清单。</summary>
        public WorkspaceManifest Manifest { get; set; }

        /// <summary>源工作空间名。</summary>
        public string Workspace { get { return Manifest != null ? Manifest.Workspace : null; } }

        /// <summary>导出资源总数（存储+资源+样式+绑定+图层组）。</summary>
        public int ResourceCount
        {
            get
            {
                if (Manifest == null) return 0;
                return Manifest.DataStores.Count + Manifest.CoverageStores.Count
                    + Manifest.FeatureTypes.Count + Manifest.Coverages.Count
                    + Manifest.Styles.Count + Manifest.LayerBindings.Count + Manifest.LayerGroups.Count;
            }
        }

        /// <summary>导出警告（单项读取失败等）。</summary>
        public IList<string> Warnings
        {
            get { return Manifest != null ? Manifest.Warnings : (IList<string>)new List<string>(); }
        }
    }

    /// <summary>工作空间导入请求。</summary>
    public sealed class WorkspaceImportRequest
    {
        /// <summary>归档包字节（与 ArchivePath 二选一，优先本项）。</summary>
        public byte[] Archive { get; set; }

        /// <summary>归档包文件路径（可空）。</summary>
        public string ArchivePath { get; set; }

        /// <summary>目标工作空间名（缺省沿用源工作空间名）。</summary>
        public string TargetWorkspace { get; set; }

        /// <summary>目标命名空间前缀（缺省同目标工作空间名）。</summary>
        public string TargetNamespacePrefix { get; set; }

        /// <summary>目标命名空间 URI（缺省沿用源 URI；源 URI 已被其它前缀占用时自动生成占位 URI）。</summary>
        public string TargetNamespaceUri { get; set; }

        /// <summary>已存在资源是否覆盖更新（false 时跳过并记 Skipped；样式按 PUT 更新内容）。</summary>
        public bool OverwriteExisting { get; set; }
    }

    /// <summary>导入/导出过程中的单项结果状态。</summary>
    public enum MigrationItemStatus
    {
        /// <summary>已创建。</summary>
        Created = 0,

        /// <summary>已更新（覆盖模式）。</summary>
        Updated = 1,

        /// <summary>已跳过（已存在且非覆盖模式）。</summary>
        Skipped = 2,

        /// <summary>失败。</summary>
        Failed = 3,
    }

    /// <summary>导入单项回放结果。</summary>
    public sealed class MigrationItemResult
    {
        /// <summary>步骤名（workspace/namespace/dataStore/coverageStore/featureType/coverage/style/layerBinding/layerGroup）。</summary>
        public string Step { get; set; }

        /// <summary>目标资源展示名。</summary>
        public string Target { get; set; }

        /// <summary>状态。</summary>
        public MigrationItemStatus Status { get; set; }

        /// <summary>说明（失败原因或跳过提示）。</summary>
        public string Message { get; set; }

        /// <summary>是否成功（Skipped 不计成功也不计失败）。</summary>
        public bool IsFailure
        {
            get { return Status == MigrationItemStatus.Failed; }
        }
    }

    /// <summary>工作空间导入聚合结果。</summary>
    public sealed class WorkspaceImportResult
    {
        /// <summary>归档清单（读取后回填）。</summary>
        public WorkspaceManifest Manifest { get; set; }

        /// <summary>逐项回放结果（按依赖顺序）。</summary>
        public List<MigrationItemResult> Items { get; set; } = new List<MigrationItemResult>();

        /// <summary>整体是否成功（无失败项）。</summary>
        public bool Success
        {
            get { return FailedCount == 0; }
        }

        /// <summary>失败项数。</summary>
        public int FailedCount
        {
            get
            {
                int n = 0;
                foreach (var i in Items) if (i.IsFailure) n++;
                return n;
            }
        }

        /// <summary>跳过项数。</summary>
        public int SkippedCount
        {
            get
            {
                int n = 0;
                foreach (var i in Items) if (i.Status == MigrationItemStatus.Skipped) n++;
                return n;
            }
        }

        /// <summary>失败摘要。</summary>
        public string FailureSummary
        {
            get
            {
                var lines = new List<string>();
                foreach (var i in Items)
                {
                    if (i.IsFailure) lines.Add(i.Step + " " + i.Target + ": " + i.Message);
                }
                return string.Join("; ", lines.ToArray());
            }
        }
    }
}
