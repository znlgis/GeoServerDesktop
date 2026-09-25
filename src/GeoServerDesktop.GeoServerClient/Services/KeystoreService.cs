using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing SSL/TLS keystores。
    /// FIXED-E4（明确化）：GeoServer 3.0.1 无 keystore REST 端点——
    /// 实测 GET /rest/security/keystore.json 与 /rest/security/keystores.json 均回 problem+json 404
    /// （gs-restconfig-3.0.1.jar 安全包内仅有 UserGroupServiceController / UsersRestController /
    /// UserPasswordController / MasterPasswordController 等，无 keystore 控制器）。
    /// 方法签名保留以兼容既有调用面，调用即抛 <see cref="NotSupportedException"/>。
    /// </summary>
    public class KeystoreService : ServiceBase, IKeystoreService
    {
        private const string NoEndpointMessage =
            "GeoServer 3.0.1 无 keystore REST 端点（/rest/security/keystore(s) 实测 404），该功能仅 GUI 可用。";


        /// <summary>
        /// 初始化 KeystoreService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public KeystoreService(IGeoServerHttpClient httpClient)
            : base(httpClient)
        {
        }

        /// <summary>
        /// Gets the keystore information —— 3.0.1 无对应端点，恒抛 <see cref="NotSupportedException"/>（FIXED-E4）。
        /// </summary>
        public Task<KeystoreInfo> GetKeystoreInfoAsync() => throw new NotSupportedException(NoEndpointMessage);

        /// <summary>
        /// Uploads a certificate to the keystore —— 3.0.1 无对应端点，恒抛 <see cref="NotSupportedException"/>（FIXED-E4）。
        /// </summary>
        public Task UploadCertificateAsync(string alias, byte[] certificateData) => throw new NotSupportedException(NoEndpointMessage);

        /// <summary>
        /// Deletes a certificate from the keystore —— 3.0.1 无对应端点，恒抛 <see cref="NotSupportedException"/>（FIXED-E4）。
        /// </summary>
        public Task DeleteCertificateAsync(string alias) => throw new NotSupportedException(NoEndpointMessage);
    }
}
