using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// IPasswordService 的服务接口（M5 接口化）：与实现类 PasswordService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface IPasswordService
    {
        /// <summary>
        /// 更改当前用户的密码。
        /// FIXED-E16（原基线误报）：3.0.1 源码 UserPasswordController#passwordPut 以
        /// <c>@RequestBody Map&lt;String,String&gt;</c> 直接取扁平 <c>newPassword</c> 键——扁平体即真实契约，
        /// 外层 {"password":{...}} 包装反而会 400 "Missing 'newPassword'"。（弱口令仍可能被密码策略拒为 400。）
        /// </summary>
        /// <param name="newPassword">新密码</param>
        /// <returns>表示异步操作的任务</returns>
        Task ChangePasswordAsync(string newPassword);
    }
}
