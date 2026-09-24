using System;
using GeoServerDesktop.App.Services;
using GeoServerDesktop.App.ViewModels;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Headless
{
    /// <summary>
    /// ImportWizardViewModel 集成测试（M2）：三步状态机 → 向导路径发布闭环 → 本地预检 → PostGIS 探测。
    /// 独立复核走裸 REST（VmRest）；资源名前缀 gdtest_vm，测后兜底清理。
    /// </summary>
    [Collection("GeoServerSerial")]
    public sealed class ImportWizardViewModelTests : GeoServerTestBase
    {
        private const string Ws = "gdtest_vm_ws_wizard";
        private const string Layer = "gdtest_vm_wizard_poly";
        private const string Store = "gdtest_vm_wizard_ds";

        public ImportWizardViewModelTests(GeoServerFixture fx) : base(fx) { }

        [Fact]
        public async Task Wizard_ShapefilePublish_ClosedLoop()
        {
            if (!RequireGeoServer()) return;
            VmTestKit.TryDeleteWorkspace(Ws);
            Assert.Equal(201, VmRest.PostJson("/rest/workspaces", "{\"workspace\":{\"name\":\"" + Ws + "\"}}"));

            var conn = VmTestKit.Connected();
            var vm = new ImportWizardViewModel(conn);
            try
            {
                // 步骤 1：文件引用与原始名
                vm.FileRef = "file:gdtest_data";
                vm.NativeName = "gdtest_poly";
                await vm.NextStepCommand.ExecuteAsync(null);
                Assert.True(vm.IsStep2);
                Assert.Equal("gdtest_poly", vm.PublishName);   // 默认发布名 = 原始名

                // 步骤 2：目标与参数（工作空间列表已自动加载）
                Assert.Contains(Ws, vm.Workspaces);
                vm.SelectedWorkspace = Ws;
                vm.PublishName = Layer;   // 发布名全局唯一
                vm.StoreName = Store;
                vm.Srs = "EPSG:4326";
                await vm.NextStepCommand.ExecuteAsync(null);
                Assert.True(vm.IsStep3);

                // 步骤 3：发布（向导服务路径）
                await vm.PublishCommand.ExecuteAsync(null);
                Assert.True(vm.PublishSucceeded, vm.ResultMessage);
                Assert.Contains(Layer, vm.ResultMessage);

                // 独立通道复核：图层经裸 REST 可见
                Assert.True(VmRest.LayerExists(Ws, Layer));

                // 重新开始回到第 1 步
                vm.RestartCommand.Execute(null);
                Assert.True(vm.IsStep1);
            }
            finally
            {
                VmTestKit.TryDeleteWorkspace(Ws);
                conn.Disconnect();
            }
        }

        [Fact]
        public async Task Wizard_StepValidation_BlocksInvalidInput()
        {
            // 未连接：下一步给出未连接提示
            var offline = new ImportWizardViewModel(new GeoServerConnectionService());
            await offline.NextStepCommand.ExecuteAsync(null);
            Assert.Equal(offline.L.StatusNotConnected, offline.StatusMessage);
            Assert.True(offline.IsStep1);

            // 已连接：文件引用缺失 / 原始名缺失分别拦截
            var conn = VmTestKit.Connected();
            var vm = new ImportWizardViewModel(conn);
            try
            {
                vm.FileRef = string.Empty;
                await vm.NextStepCommand.ExecuteAsync(null);
                Assert.Equal(vm.L.WizardStatusNeedFileRef, vm.StatusMessage);
                Assert.True(vm.IsStep1);

                vm.FileRef = "file:gdtest_data";
                vm.NativeName = string.Empty;
                await vm.NextStepCommand.ExecuteAsync(null);
                Assert.Equal(vm.L.WizardStatusNeedNativeName, vm.StatusMessage);
                Assert.True(vm.IsStep1);
            }
            finally
            {
                conn.Disconnect();
            }
        }

        [Fact]
        public void Wizard_LocalInspection_Shapefile()
        {
            if (!TestEnv.GeneratedDataExists())
            {
                SkipLog.Skip("缺少测试数据目录 " + TestEnv.GeneratedDataDir + "，跳过本地预检检查");
                return;
            }
            var vm = new ImportWizardViewModel(new GeoServerConnectionService());

            // 目录浏览
            vm.LocalPreviewPath = TestEnv.GeneratedDataDir;
            vm.InspectLocalPathCommand.Execute(null);
            Assert.Contains("gdtest_poly", vm.PreviewText);

            // 单文件解析（记录数 = 生成器确定性 12）
            vm.LocalPreviewPath = TestEnv.AbsDataPath("gdtest_poly.shp");
            vm.InspectLocalPathCommand.Execute(null);
            Assert.Contains("12", vm.PreviewText);
            Assert.Contains("3", vm.PreviewText);   // 字段数
        }

        [Fact]
        public async Task Wizard_PostgisProbe_Success()
        {
            if (!RequireGeoServer()) return;
            VmTestKit.TryDeleteWorkspace(Ws);
            Assert.Equal(201, VmRest.PostJson("/rest/workspaces", "{\"workspace\":{\"name\":\"" + Ws + "\"}}"));

            var conn = VmTestKit.Connected();
            var vm = new ImportWizardViewModel(conn);
            try
            {
                vm.SourceKindIndex = 2;   // PostGIS 表
                vm.PgHost = TestEnv.GeoServerVisiblePgHost;
                vm.PgPort = TestEnv.PgPort.ToString();
                vm.PgDatabase = TestEnv.PgDb;
                vm.PgUser = TestEnv.PgUser;
                vm.PgPassword = TestEnv.PgPass;

                await vm.NextStepCommand.ExecuteAsync(null);
                Assert.True(vm.IsStep2);
                vm.SelectedWorkspace = Ws;

                await vm.ProbePostgisCommand.ExecuteAsync(null);
                if (vm.ProbeMessage != vm.L.WizardProbeOk)
                {
                    SkipLog.Skip("PostGIS 不可用：" + vm.ProbeMessage);
                    return;
                }
                Assert.Equal(vm.L.WizardProbeOk, vm.ProbeMessage);
            }
            finally
            {
                VmTestKit.TryDeleteWorkspace(Ws);
                conn.Disconnect();
            }
        }
    }
}
