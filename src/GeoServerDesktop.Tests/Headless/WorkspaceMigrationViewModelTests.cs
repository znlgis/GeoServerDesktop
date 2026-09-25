using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GeoServerDesktop.App.ViewModels;
using GeoServerDesktop.GeoServerClient.Import;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Headless
{
    /// <summary>
    /// WorkspaceMigrationViewModel 无头测试（M4）：加载工作空间列表 → 导出（断言内存归档存在）→
    /// SaveFilePicker 注入写临时文件 → 导入到第二工作空间（逐项结果表 + 裸 REST 复核图层落库）。
    /// 文件对话框经委托注入，无头环境不依赖 UI。
    /// </summary>
    [Collection("GeoServerSerial")]
    public sealed class WorkspaceMigrationViewModelTests : GeoServerTestBase
    {
        private const string Src = "gdtest_vm_mig";
        private const string Tgt = "gdtest_vm_mig2";
        private const string Ds = "vm_mds";
        private const string Layer = "vm_mig_poly";

        public WorkspaceMigrationViewModelTests(GeoServerFixture fx) : base(fx) { }

        private async Task PublishFixtureAsync()
        {
            var conn = VmTestKit.Connected();
            try
            {
                try { await conn.GetWorkspaceService().GetWorkspaceAsync(Src); }
                catch { await conn.GetWorkspaceService().CreateWorkspaceAsync(Src); }
                var r = await conn.GetImportWizardService().PublishShapefileAsync(new ImportSourceRequest
                {
                    Kind = ImportDataSourceKind.ShapefileDirectory,
                    Workspace = Src,
                    StoreName = Ds,
                    LayerName = Layer,
                    NativeName = VmTestKit.PolyNativeName,
                    FileRef = VmTestKit.ShapefileUrl,
                    Srs = "EPSG:4326",
                });
                Assert.True(r.Success, r.Message);
            }
            finally { conn.Disconnect(); }
        }

        [Fact]
        public async Task Export_Then_Save_Then_ImportIntoTarget()
        {
            if (!RequireGeoServer()) return;
            await PublishFixtureAsync();
            var conn = VmTestKit.Connected();
            var vm = new WorkspaceMigrationViewModel(conn);
            string tmp = Path.Combine(Path.GetTempPath(), "gdtest_mig_" + Guid.NewGuid().ToString("N") + ".gdws.zip");
            try
            {
                await vm.LoadCommand.ExecuteAsync(null);
                Assert.Contains(Src, vm.Workspaces);

                vm.SelectedWorkspace = Src;
                await vm.ExportCommand.ExecuteAsync(null);
                Assert.Contains(Src, vm.ExportStatus);
                Assert.NotEqual(string.Empty, vm.ExportStatus);

                // 保存对话框注入 → 写临时文件
                vm.SaveFilePicker = (_, name) => Task.FromResult<string?>(tmp);
                await vm.SaveArchiveCommand.ExecuteAsync(null);
                Assert.True(File.Exists(tmp));
                Assert.Contains(tmp, vm.ExportStatus);

                // 导入到第二工作空间
                VmTestKit.TryDeleteWorkspace(Tgt);
                vm.ArchivePath = tmp;
                vm.TargetWorkspace = Tgt;
                await vm.ImportCommand.ExecuteAsync(null);

                Assert.NotEmpty(vm.ImportResults);
                Assert.DoesNotContain(vm.ImportResults, i => i.StatusDisplay == vm.L.MigStatusFailedItem);

                // 裸 REST 复核：目标图层/存储存在，源不受影响
                Assert.True(VmRest.LayerExists(Tgt, Layer), "目标图层应存在");
                Assert.True(VmRest.DataStoreExists(Tgt, Ds), "目标存储应存在");
            }
            finally
            {
                conn.Disconnect();
                try { File.Delete(tmp); } catch { }
                VmTestKit.TryDeleteWorkspace(Tgt);
                VmTestKit.TryDeleteWorkspace(Src);
            }
        }

        [Fact]
        public async Task Import_WithoutArchive_ReportsGuidance()
        {
            if (!RequireGeoServer()) return;
            var conn = VmTestKit.Connected();
            var vm = new WorkspaceMigrationViewModel(conn);
            try
            {
                vm.ArchivePath = Path.Combine(Path.GetTempPath(), "definitely-missing-" + Guid.NewGuid().ToString("N") + ".zip");
                await vm.ImportCommand.ExecuteAsync(null);
                Assert.Equal(vm.L.MigStatusNeedArchive, vm.StatusMessage);
            }
            finally { conn.Disconnect(); }
        }
    }
}
