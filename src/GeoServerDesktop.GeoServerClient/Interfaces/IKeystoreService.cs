using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// IKeystoreService 的服务接口（M5 接口化）：与实现类 KeystoreService 的公开实例方法一一对应，
    /// 供 ViewModel / 测试替身 / 插件系统按抽象注入；成员奇偶由 ServiceInterfaceParityTests 守护。
    /// </summary>
    public interface IKeystoreService
    {
        /// <summary>
        /// Gets the keystore information —— 3.0.1 无对应端点，恒抛 <see cref="NotSupportedException"/>（FIXED-E4）。
        /// </summary>
        Task<KeystoreInfo> GetKeystoreInfoAsync();

        /// <summary>
        /// Uploads a certificate to the keystore —— 3.0.1 无对应端点，恒抛 <see cref="NotSupportedException"/>（FIXED-E4）。
        /// </summary>
        Task UploadCertificateAsync(string alias, byte[] certificateData);

        /// <summary>
        /// Deletes a certificate from the keystore —— 3.0.1 无对应端点，恒抛 <see cref="NotSupportedException"/>（FIXED-E4）。
        /// </summary>
        Task DeleteCertificateAsync(string alias);
    }
}
