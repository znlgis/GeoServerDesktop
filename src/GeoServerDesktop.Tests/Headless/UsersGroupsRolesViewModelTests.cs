using System;
using System.Linq;
using GeoServerDesktop.App.ViewModels;
using GeoServerDesktop.Tests.Infrastructure;

namespace GeoServerDesktop.Tests.Headless
{
    /// <summary>
    /// UsersGroupsRolesViewModel 集成测试（只读基线，绝不写共享服务器，避免不可删除的用户残留）。
    /// FIXED-E7 后现状：users/groups 模型改为对象数组/字符串数组的真实形态，LoadAll 可正常回填——
    /// 用户列表含 admin、角色列表含 ADMIN；组列表按服务器实际（默认空）断言可加载即可。
    /// </summary>
    [Collection("GeoServerSerial")]
    public sealed class UsersGroupsRolesViewModelTests : GeoServerTestBase
    {
        public UsersGroupsRolesViewModelTests(GeoServerFixture fx) : base(fx) { }

        [Fact]
        public async Task LoadAll_UsersAndRoles_Populated_FIXED_E7()
        {
            if (!RequireGeoServer()) return;
            var conn = VmTestKit.Connected();
            var vm = new UsersGroupsRolesViewModel(conn);
            try
            {
                await vm.LoadAllCommand.ExecuteAsync(null);
                Assert.False(vm.IsLoading);

                // 服务器事实与视图模型回填一致
                Assert.True(VmRest.UserInList("admin"), "GD 服务器列表应含 admin");
                Assert.Contains("admin", vm.Users);
                Assert.NotNull(vm.Groups); // 组列表默认可为空，但加载不得抛错中断
                Assert.Contains("ADMIN", vm.Roles);
            }
            finally { conn.Disconnect(); }
        }
    }
}
