using System;
using System.Collections.Generic;
using System.IO;
using GeoServerDesktop.Tests.Infrastructure;

namespace GeoServerDesktop.Tests.RealData
{
    /// <summary>
    /// 数据环境判定（不修改共享 TestEnv）：容器 data_dir 挂载根、生成数据是否在挂载内、
    /// 外部真实数据目录的成对 .shp/.dbf 发现。期望值只从文件推导。
    /// </summary>
    public static class DataEnv
    {
        /// <summary>GeoServer 容器 data_dir 在宿主上的根（Docker 挂载点）。可用 GSD_CONTAINER_DATA_DIR 覆盖。</summary>
        public static string ContainerDataRoot =>
            TestEnv.Env("GSD_CONTAINER_DATA_DIR", @"D:\self\tool\docker\data\geoserver");

        /// <summary>GWC 磁盘缓存目录（宿主可见，位于 data_dir/gwc）。可用 GSD_GWC_DIR 覆盖。</summary>
        public static string GwcDir =>
            TestEnv.Env("GSD_GWC_DIR", Path.Combine(ContainerDataRoot, "gwc"));

        private static string Norm(string p) =>
            Path.GetFullPath(p).TrimEnd('\\', '/').Replace('/', '\\');

        /// <summary>生成数据目录是否位于容器挂载的 data_dir 之内（GeoServer 才读得到）。</summary>
        public static bool GeneratedDataInMount()
        {
            var data = TestEnv.GeneratedDataDir;
            if (!Directory.Exists(data)) return false;
            var root = Norm(ContainerDataRoot);
            var dir = Norm(data);
            return dir.StartsWith(root + "\\", StringComparison.OrdinalIgnoreCase)
                || string.Equals(dir, root, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>递归发现成对 .shp/.dbf（同名同目录）。返回 (shpPath, dbfPath)。</summary>
        public static List<(string shp, string dbf)> DiscoverShapefilePairs(string dir)
        {
            var pairs = new List<(string shp, string dbf)>();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return pairs;
            foreach (var shp in Directory.EnumerateFiles(dir, "*.shp", SearchOption.AllDirectories))
            {
                var dbf = Path.ChangeExtension(shp, ".dbf");
                if (File.Exists(dbf)) pairs.Add((shp, dbf));
            }
            pairs.Sort((a, b) => string.CompareOrdinal(a.shp, b.shp));
            return pairs;
        }

        /// <summary>把宿主绝对路径映射到 GeoServer file: 相对 data_dir 的引用；不在挂载内返回 null。</summary>
        public static string HostPathToDataDirRef(string hostPath)
        {
            var root = Norm(ContainerDataRoot);
            var p = Norm(hostPath);
            if (!p.StartsWith(root + "\\", StringComparison.OrdinalIgnoreCase)) return null;
            var rel = p.Substring(root.Length + 1).Replace('\\', '/');
            return "file:" + rel;                 // 相对 data_dir（容器内解析）
        }
    }
}
