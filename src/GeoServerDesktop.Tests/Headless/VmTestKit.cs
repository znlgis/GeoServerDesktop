using System;
using GeoServerDesktop.App.Services;
using GeoServerDesktop.GeoServerClient.Configuration;
using GeoServerDesktop.Tests.Infrastructure;

namespace GeoServerDesktop.Tests.Headless
{
    /// <summary>
    /// Layer 4 集成测试共用工具：真实连接服务工厂 + gdtest_vm 前缀资源名 + 兜底清理。
    /// 所有服务器资源前缀 gdtest_vm / gdtest_vmw_*，测后显式删除并兜底。
    /// </summary>
    internal static class VmTestKit
    {
        /// <summary>新建一个连到真实 GeoServer 的连接服务（调用方负责 Disconnect）。</summary>
        public static GeoServerConnectionService Connected()
        {
            var c = new GeoServerConnectionService();
            c.Connect(GeoServerAvailability.Options());
            return c;
        }

        /// <summary>GeoServer 数据目录中已备好的 shapefile 目录（REST url 参数）。</summary>
        public const string ShapefileUrl = "file:gdtest_data";

        /// <summary>
        /// 让 ViewModel 里 OnSelected*Changed 触发的“即发即忘”后台重载线程跑完，再对 ObservableCollection
        /// 做枚举/快照，避免在后台线程增删集合时枚举导致的 IndexOutOfRangeException/竞态。
        /// </summary>
        public static async System.Threading.Tasks.Task Settle() => await System.Threading.Tasks.Task.Delay(500);

        /// <summary>已备好的面数据 shapefile 的 nativeName（不带扩展名）。</summary>
        public const string PolyNativeName = "gdtest_poly";

        /// <summary>兜底删除工作空间（递归），忽略不存在/错误。</summary>
        public static void TryDeleteWorkspace(string ws) => Safe(() => VmRest.Delete("/rest/workspaces/" + Uri.EscapeDataString(ws) + "?recurse=true"));

        public static void TryDeleteStyle(string s) => Safe(() => VmRest.Delete("/rest/styles/" + Uri.EscapeDataString(s) + "?purge=true"));

        public static void TryDeleteLayerGroup(string lg) => Safe(() => VmRest.Delete("/rest/layergroups/" + Uri.EscapeDataString(lg)));

        public static void TryDeleteUser(string u) => Safe(() => VmRest.Delete("/rest/security/usergroup/users/" + Uri.EscapeDataString(u)));

        public static void TryDeleteGroup(string g) => Safe(() => VmRest.Delete("/rest/security/usergroup/groups/" + Uri.EscapeDataString(g)));

        private static void Safe(Func<int> _) { try { _(); } catch { } }
    }
}
