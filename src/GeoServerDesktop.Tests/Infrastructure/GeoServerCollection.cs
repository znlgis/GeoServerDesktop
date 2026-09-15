using System;
using System.Net.Http;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Configuration;

namespace GeoServerDesktop.Tests.Infrastructure
{
    /// <summary>GeoServer 可达性探测：不可达时所有集成测试应跳过。</summary>
    public sealed class GeoServerAvailability
    {
        private static bool? _reachable;
        private static readonly object Gate = new object();

        public static bool IsGeoServerReachable
        {
            get
            {
                lock (Gate)
                {
                    if (_reachable.HasValue) return _reachable.Value;
                    try
                    {
                        using (var c = new HttpClient { Timeout = TimeSpan.FromSeconds(8) })
                        {
                            var url = TestEnv.RestBase.TrimEnd('/') + "/rest/about/version.json";
                            c.DefaultRequestHeaders.Authorization =
                                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic",
                                    Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(
                                        TestEnv.User + ":" + TestEnv.Pass)));
                            var ok = c.GetAsync(url).GetAwaiter().GetResult().IsSuccessStatusCode;
                            _reachable = ok;
                        }
                    }
                    catch { _reachable = false; }
                    return _reachable.Value;
                }
            }
        }

        public static GeoServerClientOptions Options() => new GeoServerClientOptions
        {
            BaseUrl = TestEnv.BaseUrl,
            Username = TestEnv.User,
            Password = TestEnv.Pass,
            TimeoutSeconds = 60
        };
    }

    /// <summary>所有触碰共享 GeoServer 状态的测试挂在此串行集合上。</summary>
    /// <remarks>
    /// 必须实现 ICollectionFixture&lt;GeoServerFixture&gt;，xunit 才会向 GeoServerTestBase 构造函数注入
    /// 共享夹具实例，并在整个集合跑完后调用 GeoServerFixture.Dispose()（→ CleanupOrphansBestEffort 兜底清理）。
    /// </remarks>
    [Xunit.CollectionDefinition("GeoServerSerial", DisableParallelization = true)]
    public sealed class GeoServerSerialCollection : Xunit.ICollectionFixture<GeoServerFixture> { }

    /// <summary>
    /// 运行期跳过登记器。xunit v2 (2.9.2) 不支持运行期动态 Skip（无公共 SkipException/Assert.Skip），
    /// 因此统一协议：守卫返回 false → 测试方法立即 return（控制台呈现为“通过”），
    /// 同时在此登记原因；dotnet test 日志与 harness 报告都会显式列出 SKIPPED 条目，不会静默。
    /// </summary>
    public static class SkipLog
    {
        public static readonly System.Collections.Concurrent.ConcurrentQueue<string> Entries
            = new System.Collections.Concurrent.ConcurrentQueue<string>();

        public static bool Skip(string reason, [System.Runtime.CompilerServices.CallerMemberName] string test = "")
        {
            Entries.Enqueue(test + " :: " + reason);
            Console.WriteLine("[SKIPPED] " + test + " :: " + reason);
            return false;
        }
    }

    /// <summary>共享夹具：探测 + 统一清理登记。</summary>
    public sealed class GeoServerFixture : IDisposable
    {
        public bool Available => GeoServerAvailability.IsGeoServerReachable;
        public CleanupTracker Cleanup { get; } = new CleanupTracker();

        public GeoServerClientFactory Factory() => new GeoServerClientFactory(GeoServerAvailability.Options());

        public void Dispose()
        {
            // 孤儿资源兜底清理（删除所有 gdtest 前缀资源；共享 e2e 夹具工作空间保留，见 CleanupTracker）
            try { Cleanup.CleanupOrphansBestEffort(); } catch { }
        }
    }

    [Xunit.Collection("GeoServerSerial")]
    public abstract class GeoServerTestBase : IDisposable
    {
        protected readonly GeoServerFixture Fx;
        protected GeoServerTestBase(GeoServerFixture fx) { Fx = fx; }

        /// <summary>集成测试统一守卫：GeoServer 不可达 → false（调用方立即 return，并用 SkipLog 记录）。</summary>
        protected bool RequireGeoServer()
        {
            if (!Fx.Available)
                return SkipLog.Skip("GeoServer 不可达（" + TestEnv.BaseUrl + "）");
            return true;
        }

        public virtual void Dispose() { }
    }
}
