using System;
using System.Linq;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using GeoServerDesktop.Tests.Infrastructure;

namespace GeoServerDesktop.Tests.Integration
{
    /// <summary>
    /// 安全族集成测试（GeoServer 3.0.1，B 域缺陷修复后基线）。
    /// FIXED-E7 族（源码 UsersRestController / UserGroupServiceController / UserPasswordController + 实测）：
    ///  - users.json 为 {"users":[{enabled,password,userName}]} 对象数组——List&lt;User&gt; 模型正常解析；
    ///  - 单用户无 GET 详情路由：GetUserAsync 由列表合成；创建 POST /users、更新/删除走单数 /user/{name}（实测 200/201）；
    ///  - groups.json 为 {"groups":[字符串]}；组创建 POST /group/{name}（实测 201；集合 POST /groups 405）；
    ///  - 组详情路由 /group/{g}/users 实测对任意组恒 400 → GetGroupAsync 明确化为 NotSupportedException；
    ///  - roles.json 为 {"roles":["ADMIN",...]} 字符串数组（E7 对 roles 属误报）；
    ///  - ACL 无 .json 后缀（FIXED-E19）：/acl/catalog 回 {"mode":"HIDE"}、/acl/layers 回扁平 map；
    ///  - ChangePassword 绝不在集成测试里真调（会改坏 admin 口令；契约已由离线单测+源码固化）。
    /// 命名族缩写：sec。
    /// </summary>
    [Collection("GeoServerSerial")]
    public class SecurityIT : GeoServerTestBase
    {
        public SecurityIT(GeoServerFixture fx) : base(fx) { }

        private const string TestUser = TestEnv.Prefix + "_user1";
        private const string TestGroup = TestEnv.Prefix + "_grp1";

        /// <summary>兜底：若 REST 删除失效走文件级删除 + reload。</summary>
        private async Task EnsureUserGoneAsync(string name)
        {
            var list = await Fx.Cleanup.GetAsync("/rest/security/usergroup/users.json");
            if (list != null && list.Contains("\"" + name + "\""))
            {
                CleanupTracker.RemoveUserViaRegistryFile(name);
                await Fx.Cleanup.PostAsync("/rest/reload");
            }
        }

        [Fact]
        public async Task Users_List_ParsesObjectArray_FIXED_E7()
        {
            if (!RequireGeoServer()) return;
            using var f = Fx.Factory();
            var svc = f.CreateUserGroupService();
            // FIXED-E7 翻转：对象数组模型直接解析，且含 admin
            var w = await svc.GetUsersAsync();
            Assert.NotNull(w?.Users);
            Assert.Contains("admin", w!.UserNames);
        }

        [Fact]
        public async Task User_CreateGetUpdateDelete_Works_FIXED_E7()
        {
            if (!RequireGeoServer()) return;
            using var f = Fx.Factory();
            var svc = f.CreateUserGroupService();
            Fx.Cleanup.TrackUser(TestUser);
            try
            {
                await EnsureUserGoneAsync(TestUser);

                // CREATE：POST /users（enabled 显式）实测 201
                await svc.CreateUserAsync(new User { UserName = TestUser, Password = "Gdtest#123", Enabled = true });
                var raw = await Fx.Cleanup.GetAsync("/rest/security/usergroup/users.json");
                Assert.Contains(TestUser, raw ?? "");

                // GET 合成：由列表 + /user/{name}/groups 组装（无单体路由）
                var got = await svc.GetUserAsync(TestUser);
                Assert.NotNull(got.User);
                Assert.Equal(TestUser, got.User!.UserName);
                Assert.True(got.User.Enabled);

                // UPDATE：POST /user/{name}（实测 200）
                await svc.UpdateUserAsync(TestUser, new User { UserName = TestUser, Enabled = false });
                var after = await svc.GetUserAsync(TestUser);
                Assert.False(after.User!.Enabled);

                // DELETE：DELETE /user/{name}（实测 200）
                await svc.DeleteUserAsync(TestUser);
                raw = await Fx.Cleanup.GetAsync("/rest/security/usergroup/users.json");
                Assert.DoesNotContain(TestUser, raw ?? "");
            }
            finally
            {
                try { await svc.DeleteUserAsync(TestUser); } catch { }
                await EnsureUserGoneAsync(TestUser);
            }
        }

        [Fact]
        public async Task Groups_List_And_Named_Create_Delete_FIXED_E7()
        {
            if (!RequireGeoServer()) return;
            using var f = Fx.Factory();
            var svc = f.CreateUserGroupService();

            // 列表为字符串数组（模型即真实形态）
            var g = await svc.GetGroupsAsync();
            Assert.NotNull(g?.Groups);

            // 创建：POST /group/{name} 实测 201（集合 POST /groups 405）
            await svc.CreateGroupAsync(new UserGroup { GroupName = TestGroup, Enabled = true });
            var raw = await Fx.Cleanup.GetAsync("/rest/security/usergroup/groups.json");
            Assert.Contains(TestGroup, raw ?? "");

            // 组详情通道不可用（实测 /group/{g}/users 恒 400）
            await Assert.ThrowsAsync<NotSupportedException>(() => svc.GetGroupAsync(TestGroup));

            // 删除：DELETE /group/{name} 实测 200
            await svc.DeleteGroupAsync(TestGroup);
            raw = await Fx.Cleanup.GetAsync("/rest/security/usergroup/groups.json");
            Assert.DoesNotContain(TestGroup, raw ?? "");
        }

        [Fact]
        public async Task Roles_List_Works_Baseline()
        {
            if (!RequireGeoServer()) return;
            using var f = Fx.Factory();
            var svc = f.CreateRoleService();
            // 实测 3.0.1：{"roles":["ADMIN","GROUP_ADMIN"]} 字符串数组——E7 对 roles 不成立，模型兼容
            var roles = await svc.GetRolesAsync();
            Assert.NotNull(roles?.Roles);
            Assert.Contains("ADMIN", roles!.Roles!);
        }

        [Fact]
        public async Task Acl_Get_NoJsonSuffix_Parses_FIXED_E19()
        {
            if (!RequireGeoServer()) return;
            using var f = Fx.Factory();
            var svc = f.CreateSecurityService();
            // FIXED-E19 翻转：路由去掉 .json 后 200；catalog 为 {"mode":...}，layers 为扁平 map
            var catalog = await svc.GetACLAsync("catalog");
            Assert.False(string.IsNullOrEmpty(catalog?.Mode));
            var layers = await svc.GetACLAsync("layers");
            Assert.NotNull(layers?.RawRules);
            Assert.NotEmpty(layers!.RawRules!);
        }

        [Fact]
        public async Task AuthFilters_List_LowercaseRoute_Parses_FIXED_E1()
        {
            if (!RequireGeoServer()) return;
            using var f = Fx.Factory();
            var svc = f.CreateAuthenticationFilterService();
            var w = await svc.GetFiltersAsync();
            Assert.NotEmpty(w.Filters);
            Assert.Contains(w.Filters, x => x.Name == "basic");
            var basic = await svc.GetFilterAsync("basic");
            Assert.Equal("basic", basic.Filter?.Name);
            Assert.False(string.IsNullOrEmpty(basic.Filter!.ClassName));
        }

        [Fact]
        public async Task AuthProviders_List_DynamicFqnMap_FIXED_E2()
        {
            if (!RequireGeoServer()) return;
            using var f = Fx.Factory();
            var svc = f.CreateAuthenticationProviderService();
            var w = await svc.GetProvidersAsync();
            Assert.NotEmpty(w.Providers);
            Assert.Contains(w.Providers, p => p.Name == "default");
            Assert.All(w.Providers, p => Assert.False(string.IsNullOrEmpty(p.ConfigClassName)));
        }

        [Fact]
        public async Task FilterChain_SingularRoute_Parses_FIXED_E3()
        {
            if (!RequireGeoServer()) return;
            using var f = Fx.Factory();
            var svc = f.CreateFilterChainService();
            var w = await svc.GetFilterChainsAsync();
            Assert.NotEmpty(w.Chains);
            Assert.Contains(w.Chains, c => c.Name == "web");
        }

        // ChangePasswordAsync 不真调：会改掉 admin 口令并可能锁死服务器（离线层已覆盖请求体契约）。
    }
}
