using System;
using System.IO;
using System.Linq;

namespace GeoServerDesktop.Tests.Infrastructure
{
    /// <summary>
    /// 测试环境配置：全部经环境变量注入，不内置任何数据集/主机信息。
    /// GeoServer 不可达时测试应整体跳过（数据无关、环境无关）。
    /// </summary>
    public static class TestEnv
    {
        public const string Prefix = "gdtest";

        public static string BaseUrl => Env("GSD_GEOSERVER_URL", "http://localhost:8765/geoserver");
        public static string User => Env("GSD_GEOUSER", "admin");
        public static string Pass => Env("GSD_GEEPASS", "geoserver");

        /// <summary>数据目录内由测试生成的数据集目录（位于 GeoServer 可读的 data_dir 下）。</summary>
        public static string GeneratedDataDir =>
            Env("GSD_TEST_DATA_DIR", @"D:\self\tool\docker\data\geoserver\gdtest_data");

        /// <summary>可选：外部真实数据目录（成对 .shp/.dbf 自动发现，期望值从文件头解析）。</summary>
        public static string RealDataDir => Env("GSD_REAL_DATA_DIR", null);

        public static string PgHost => Env("GSD_TEST_PG_HOST", "127.0.0.1");
        public static int PgPort => int.Parse(Env("GSD_TEST_PG_PORT", "5432"));
        public static string PgDb => Env("GSD_TEST_PG_DB", "postgres");
        public static string PgUser => Env("GSD_TEST_PG_USER", "postgres");
        public static string PgPass => Env("GSD_TEST_PG_PASS", "postgres");
        public const string PgTablePrefix = "gdtest_";

        /// <summary>
        /// GeoServer（Docker 容器）视角下可达的 PostGIS 主机名——容器内 127.0.0.1 指容器自身，
        /// Docker Desktop 下宿主机库需用 host.docker.internal；集成测试建 PostGIS 存储必须用本值。
        /// </summary>
        public static string GeoServerVisiblePgHost => Env("GSD_GS_PG_HOST", "host.docker.internal");

        /// <summary>GeoServer data_dir 根（宿主机视角）：GeneratedDataDir 的父目录，供文件级兜底清理（如 users.xml）。</summary>
        public static string DataDirRoot => Env("GSD_GS_DATA_DIR",
            System.IO.Path.GetDirectoryName(GeneratedDataDir.TrimEnd('\\', '/')) ?? GeneratedDataDir);

        /// <summary>App SettingsService 存储路径覆盖（测试绝不动用户真实 %APPDATA%）。</summary>
        public static string SettingsPathOverride => Env("GSD_SETTINGS_PATH", null);

        public static string Env(string name, string fallback)
        {
            var v = Environment.GetEnvironmentVariable(name);
            return string.IsNullOrWhiteSpace(v) ? fallback : v.Trim();
        }

        /// <summary>REST 基址（无尾斜杠）。</summary>
        public static string RestBase
        {
            get
            {
                var u = BaseUrl.TrimEnd('/');
                return u.EndsWith("/geoserver", StringComparison.OrdinalIgnoreCase) ? u : u;
            }
        }

        /// <summary>OGC 端点（如 .../geoserver/wms），若 BaseUrl 含 /geoserver 前缀则复用。</summary>
        public static string OgcUrl(string suffix) => RestBase + "/" + suffix.TrimStart('/');

        public static string AbsDataPath(params string[] parts) =>
            Path.Combine(new[] { GeneratedDataDir }.Concat(parts).ToArray());

        public static bool GeneratedDataExists() => Directory.Exists(GeneratedDataDir);
    }
}
