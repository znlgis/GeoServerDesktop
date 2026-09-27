using System;
using System.IO;
using GeoServerDesktop.App.ViewModels;
using GeoServerDesktop.Tests.Infrastructure;
using GeoServerDesktop.Tests.RealData;
using Xunit;

namespace GeoServerDesktop.Tests.Headless
{
    /// <summary>
    /// 导入向导“数据风险预检”的 App 层用例（L4 + 真实实例 + 真实数据文件）：
    /// 证明库层检出的风险确实到达用户可见的文案，且损坏数据在点击发布时就被拦下（而不是“发布成功但图层空”）。
    /// 独立复核一律走裸 REST（VmRest），期望值来自数据文件自身。
    /// </summary>
    [Collection("GeoServerSerial")]
    public sealed class ImportWizardPreflightVmTests : GeoServerTestBase
    {
        private const string Ws = "gdtest_vm_ws_preflight";

        public ImportWizardPreflightVmTests(GeoServerFixture fx) : base(fx) { }

        private static string VecDir => ExtendedFixture.VecDir;
        private static string BadDir => ExtendedFixture.BadDir;

        private static bool DataReady()
        {
            if (!ExtendedFixture.DataPresent)
                return SkipLog.Skip("扩展数据集缺失（请先运行 tests/testdata/generate_testdata.py）");
            return true;
        }

        [Fact]
        public void Preflight_GbkAttributesWithoutCpg_AreSurfacedToUser()
        {
            if (!RequireGeoServer() || !DataReady()) return;
            var vm = new ImportWizardViewModel(VmTestKit.Connected());
            vm.LocalPreviewPath = Path.Combine(VecDir, "enc_gbk_nocpg.shp");

            vm.InspectLocalPathCommand.Execute(null);

            // 断言取“与语言无关的证据串”：库层必须点明服务端默认解码是 ISO-8859-1（L4 用例跑在英文文化下）
            Assert.Contains("ISO-8859-1", vm.PreviewText);
            Assert.Contains(".cpg", vm.PreviewText);
            Assert.Contains("4 records", vm.PreviewText);           // 预检摘要本身仍要正常给出
        }

        [Fact]
        public void Preflight_DeclaredEncodings_ProduceNoFalseAlarm()
        {
            if (!RequireGeoServer() || !DataReady()) return;
            var vm = new ImportWizardViewModel(VmTestKit.Connected());
            foreach (var shp in new[] { "enc_utf8.shp", "enc_gbk_cpg.shp" })
            {
                vm.LocalPreviewPath = Path.Combine(VecDir, shp);
                vm.InspectLocalPathCommand.Execute(null);
                Assert.DoesNotContain("ISO-8859-1", vm.PreviewText);   // 已声明编码 → 不得误报
                Assert.DoesNotContain("GBK", vm.PreviewText);
                Assert.Contains("4 records", vm.PreviewText);           // 摘要正常渲染
            }
        }

        [Fact]
        public void Preflight_MissingPrj_IsFlaggedButNotBlocked()
        {
            if (!RequireGeoServer() || !DataReady()) return;
            var vm = new ImportWizardViewModel(VmTestKit.Connected());
            vm.LocalPreviewPath = Path.Combine(BadDir, "bd_noprj.shp");

            vm.InspectLocalPathCommand.Execute(null);

            Assert.Contains(".prj", vm.PreviewText);
            Assert.Contains("SRS", vm.PreviewText);                 // 缺投影：告警要点明“发布须显式声明 SRS”
        }

        [Fact]
        public void Preflight_DirectoryListing_ReportsPerFileRisk()
        {
            if (!RequireGeoServer() || !DataReady()) return;
            var vm = new ImportWizardViewModel(VmTestKit.Connected());
            vm.LocalPreviewPath = VecDir;

            vm.InspectLocalPathCommand.Execute(null);

            Assert.Contains("enc_gbk_nocpg", vm.PreviewText);       // 目录形态也要指出是哪个文件
            Assert.Contains("ISO-8859-1", vm.PreviewText);          // 且风险逐条挂到文件名上
        }

        [Fact]
        public async Task Publish_CorruptedShapefile_BlockedBeforeReachingServer()
        {
            if (!RequireGeoServer() || !DataReady()) return;
            VmTestKit.TryDeleteWorkspace(Ws);
            Assert.Equal(201, VmRest.PostJson("/rest/workspaces", "{\"workspace\":{\"name\":\"" + Ws + "\"}}"));

            var vm = new ImportWizardViewModel(VmTestKit.Connected());
            try
            {
                vm.SourceKindIndex = 0;
                vm.FileRef = "file:gdtest_bad";
                vm.NativeName = "bd_nodbf";
                vm.LocalPreviewPath = Path.Combine(BadDir, "bd_nodbf.shp");
                await vm.NextStepCommand.ExecuteAsync(null);
                vm.SelectedWorkspace = Ws;
                vm.PublishName = "gdtest_vm_bd_nodbf";
                vm.StoreName = "gdtest_vm_bd_ds";
                vm.Srs = "EPSG:4326";
                await vm.NextStepCommand.ExecuteAsync(null);
                await vm.PublishCommand.ExecuteAsync(null);

                // 关键回归点：早前此处会“发布成功”，用户拿到一个没有属性面的空图层
                Assert.False(vm.PublishSucceeded, "损坏数据被当作发布成功：" + vm.ResultMessage);
                Assert.Contains("dbf", vm.ResultMessage, StringComparison.OrdinalIgnoreCase);
                Assert.False(VmRest.LayerExists(Ws, "gdtest_vm_bd_nodbf"));
                Assert.DoesNotContain("gdtest_vm_bd_nodbf",
                    VmRest.Get("/rest/workspaces/" + Ws + "/datastores/gdtest_vm_bd_ds/featuretypes.json") ?? "");
            }
            finally { VmTestKit.TryDeleteWorkspace(Ws); }
        }

        [Fact]
        public async Task Publish_RiskyButValidData_SucceedsWithVisibleNotice()
        {
            if (!RequireGeoServer() || !DataReady()) return;
            VmTestKit.TryDeleteWorkspace(Ws);
            Assert.Equal(201, VmRest.PostJson("/rest/workspaces", "{\"workspace\":{\"name\":\"" + Ws + "\"}}"));

            var vm = new ImportWizardViewModel(VmTestKit.Connected());
            try
            {
                vm.SourceKindIndex = 0;
                vm.FileRef = "file:gdtest_vec";
                vm.NativeName = "enc_gbk_nocpg";
                vm.LocalPreviewPath = Path.Combine(VecDir, "enc_gbk_nocpg.shp");
                await vm.NextStepCommand.ExecuteAsync(null);
                vm.SelectedWorkspace = Ws;
                vm.PublishName = "gdtest_vm_enc_gbk_nocpg";
                vm.StoreName = "gdtest_vm_enc_ds";
                vm.Srs = "EPSG:4326";
                await vm.NextStepCommand.ExecuteAsync(null);
                await vm.PublishCommand.ExecuteAsync(null);

                Assert.True(vm.PublishSucceeded, vm.ResultMessage);
                // 告警不阻断发布，但必须出现在结果里（服务端按 Latin-1 解码 → 中文会乱码）
                Assert.Contains("ISO-8859-1", vm.ResultMessage);   // 服务端按 Latin-1 解码的后果要点明
                Assert.True(VmRest.LayerExists(Ws, "gdtest_vm_enc_gbk_nocpg"));
            }
            finally { VmTestKit.TryDeleteWorkspace(Ws); }
        }
    }
}
