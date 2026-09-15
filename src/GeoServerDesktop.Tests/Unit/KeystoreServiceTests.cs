using System;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Services;
using GeoServerDesktop.Tests.Infrastructure;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>KeystoreService 离线单元测试。FIXED-E4（明确化）：GeoServer 3.0.1 无 keystore REST 端点
/// ——实测 /rest/security/keystore.json 与 /rest/security/keystores.json 均 404（problem+json），
/// gs-restconfig-3.0.1.jar 安全包内亦无 keystore 控制器；方法保留但恒抛 NotSupportedException。</summary>
public class KeystoreServiceTests
{
    private readonly RecordingFakeClient _fake = new();
    private readonly KeystoreService _svc;

    public KeystoreServiceTests() => _svc = new KeystoreService(_fake);

    [Fact]
    public void Ctor_NullClient_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new KeystoreService(null!));

    [Fact]
    public async Task GetKeystoreInfoAsync_ThrowsNotSupported_FIXED_E4()
    {
        var ex = await Assert.ThrowsAsync<NotSupportedException>(() => _svc.GetKeystoreInfoAsync());
        Assert.Contains("keystore", ex.Message);
        Assert.Null(_fake.Last); // 不发出任何请求
    }

    [Fact]
    public async Task UploadCertificateAsync_ThrowsNotSupported_FIXED_E4()
    {
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            _svc.UploadCertificateAsync("myalias", new byte[] { 1, 2, 3 }));
        Assert.Null(_fake.Last);
    }

    [Fact]
    public async Task DeleteCertificateAsync_ThrowsNotSupported_FIXED_E4()
    {
        await Assert.ThrowsAsync<NotSupportedException>(() => _svc.DeleteCertificateAsync("myalias"));
        Assert.Null(_fake.Last);
    }
}
