using System;
using GeoServerDesktop.App.Services;

namespace GeoServerDesktop.Tests.Headless
{
    /// <summary>
    /// LocalizationService 离线测试（进程级全局单例）。
    /// 归入 GeoServerSerial 串行集合：与所有会读取 L.* 期望串/翻转语言的用例互斥，避免全局状态泄漏。
    /// 每个用例先探测原始语言、全程 try/finally 恢复。
    /// </summary>
    [Collection("GeoServerSerial")]
    public sealed class LocalizationServiceTests
    {
        private static LocalizationService L => LocalizationService.Instance;

        // 通过某个双语文本不同来判断当前是否中文（AppHeaderSubtitle：英文 "Administration" / 中文 "管理"）
        private static bool IsChinese() => L.AppHeaderSubtitle != "Administration";

        private static void EnsureChinese() { if (!IsChinese()) L.ToggleLanguage(); }
        private static void EnsureEnglish() { if (IsChinese()) L.ToggleLanguage(); }

        [Fact]
        public void Instance_IsProcessSingleton()
        {
            Assert.True(ReferenceEquals(LocalizationService.Instance, LocalizationService.Instance));
        }

        [Fact]
        public void ToggleLanguage_DefaultEnglish_Chinese_AndBack_AllRestored()
        {
            bool originalChinese = IsChinese();
            try
            {
                EnsureEnglish();

                // 代表不同分节的 ≥15 个属性：英文态
                var probes = new (Func<string> get, string en, string zh)[]
                {
                    (() => L.AppHeaderSubtitle, "Administration", "管理"),
                    (() => L.Login, "Login", "登录"),
                    (() => L.Logout, "Logout", "注销"),
                    (() => L.Create, "Create", "创建"),
                    (() => L.Delete, "Delete", "删除"),
                    (() => L.Cancel, "Cancel", "取消"),
                    (() => L.SaveSettings, "Save Settings", "保存设置"),
                    (() => L.LabelUser, "User:", "用户:"),
                    (() => L.LabelPassword, "Password:", "密码:"),
                    (() => L.NavWorkspaces, "Workspaces", "工作空间"),
                    (() => L.NavStores, "Stores", "数据存储"),
                    (() => L.NavLayers, "Layers", "图层"),
                    (() => L.NavStyles, "Styles", "样式"),
                    (() => L.NavSettings, "Settings", "设置"),
                    (() => L.NavSecurity, "Security", "安全"),
                    (() => L.NavAboutAndStatus, "About & Status", "关于 & 状态"),
                    (() => L.Connected, "● Connected", "● 已连接"),
                    (() => L.StatusNotConnected, "Not connected to GeoServer", "未连接到 GeoServer"),
                };

                foreach (var p in probes)
                    Assert.Equal(p.en, p.get());

                // 切到中文
                L.ToggleLanguage();
                Assert.True(IsChinese());
                foreach (var p in probes)
                    Assert.Equal(p.zh, p.get());

                // 切回英文
                L.ToggleLanguage();
                Assert.False(IsChinese());
                foreach (var p in probes)
                    Assert.Equal(p.en, p.get());
            }
            finally
            {
                if (IsChinese() != originalChinese) L.ToggleLanguage();
            }
        }

        [Fact]
        public void StatusPlaceholders_RetainFormatArguments()
        {
            bool originalChinese = IsChinese();
            try
            {
                EnsureEnglish();
                // 带 {0} 占位的 Status* 属性在两种语言下都须保留占位（供 string.Format 注入）
                Assert.Contains("{0}", L.StatusWorkspacesLoaded);
                Assert.Contains("{0}", L.StatusSystemInfoLoadFailed);
                Assert.Contains("{0}", L.StatusWorkspaceCreated);
                Assert.Contains("{0}", L.StatusResourcesLoadFailed);

                EnsureChinese();
                Assert.Contains("{0}", L.StatusWorkspacesLoaded);
                Assert.Contains("{0}", L.StatusWorkspaceCreated);

                // M4（批量操作/迁移/设置同步）双语代表串：中英两态均断言（显式定语言态，避免前序用例语言残留）
                if (IsChinese()) L.ToggleLanguage(); // → 英文
                var m4Probes = new (Func<string> get, string en, string zh)[]
                {
                    (() => L.NavTools, "Tools", "工具"),
                    (() => L.NavBatch, "Batch Operations", "批量操作"),
                    (() => L.BatchApplyStyle, "Set Default Style", "批量改默认样式"),
                    (() => L.NavMigration, "Workspace Import/Export", "工作空间迁移"),
                    (() => L.MigOverwrite, "Overwrite existing resources", "覆盖已存在资源"),
                    (() => L.NavSettingsSync, "Settings Sync", "设置同步"),
                    (() => L.SyncStatusIdentical, "Settings are identical", "两侧设置一致，无差异"),
                };
                foreach (var p in m4Probes)
                    Assert.Equal(p.en, p.get());
                L.ToggleLanguage(); // → 中文
                foreach (var p in m4Probes)
                    Assert.Equal(p.zh, p.get());
                L.ToggleLanguage(); // → 英文
                foreach (var p in m4Probes)
                    Assert.Equal(p.en, p.get());
                if (originalChinese) L.ToggleLanguage(); // 恢复原始语言

                // string.Format 能正确注入
                Assert.Contains("7", string.Format(L.StatusWorkspacesLoaded, 7));
            }
            finally
            {
                if (IsChinese() != originalChinese) L.ToggleLanguage();
            }
        }
    }
}
