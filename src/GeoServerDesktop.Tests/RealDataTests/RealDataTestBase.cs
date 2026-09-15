using System;
using GeoServerDesktop.Tests.Infrastructure;
using GeoServerDesktop.Tests.RealData;

namespace GeoServerDesktop.Tests.RealDataTests
{
    /// <summary>
    /// Layer 3 端到端测试基类：串行集合 + GeoServer 可达 + 生成数据在容器 data_dir 挂载内，
    /// 否则守卫返回 false（SkipLog 登记，调用方立即 return——xunit v2 统一跳过协议）。
    /// 数据齐备则首次进入本进程时用被测库幂等发布 gdtest_ws_e2e 全量 fixture（进程内仅一次，
    /// 但 EnsurePublished 内部逐项探测：任一资源缺失/损坏（如 PostGIS 表未建、coverage 零维）即补建，
    /// 服务面读取一律走裸 HttpClient（OgcProbe）。
    /// </summary>
    [Collection("GeoServerSerial")]
    public abstract class RealDataTestBase : GeoServerTestBase
    {
        protected static readonly object Gate = new object();
        protected static bool Published;

        protected RealDataTestBase(GeoServerFixture fx) : base(fx) { }

        protected string DataDir => TestEnv.GeneratedDataDir;

        /// <summary>端到端就绪守卫：不可达或数据缺失 → false（已登记跳过）；齐备 → 幂等发布 fixture 后 true。</summary>
        protected bool RequireReady()
        {
            if (!RequireGeoServer()) return false;
            if (!TestEnv.GeneratedDataExists() || !DataEnv.GeneratedDataInMount())
                return SkipLog.Skip("生成数据缺失或不在容器 data_dir 挂载内（GSD_TEST_DATA_DIR / GSD_CONTAINER_DATA_DIR），跳过端到端校验");
            lock (Gate)
            {
                if (!Published)
                {
                    E2ePublishHelper.EnsurePublished(Fx);
                    Published = true;
                }
            }
            return true;
        }
    }
}
