using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GeoServerDesktop.GeoServerClient.Migration;
using GeoServerDesktop.GeoServerClient.Services;
using Xunit;

namespace GeoServerDesktop.Tests.Unit;

/// <summary>
/// 服务接口奇偶校验（M5 接口化守护）：
/// ① 每个公开服务类必须有同名 <c>I{Name}</c> 接口并被其实现；
/// ② 接口成员与实现类的公开实例方法在名字/返回类型/参数类型上双向一致
///    （防止"加了服务方法忘了加接口"或"接口漂移"）；
/// ③ 工厂 CreateXxxService 的返回类型必须实现对应接口（消费方可按抽象注入）。
/// 全部离线反射，不触网。
/// </summary>
public class ServiceInterfaceParityTests
{
    private static readonly Assembly Lib = typeof(WorkspaceService).Assembly;

    /// <summary>不参与接口化的类型：基类本身与非服务辅助类。</summary>
    private static readonly string[] Excluded =
    {
        nameof(ServiceBase),
        nameof(LogResourcePathResolver),
    };

    private static IEnumerable<Type> ServiceTypes() =>
        Lib.GetTypes()
            .Where(t => t.IsClass && t.IsPublic && !t.IsAbstract) // 静态类 = abstract+sealed，一并排除
            .Where(t => t.Namespace == "GeoServerDesktop.GeoServerClient.Services"
                     || t.Namespace == "GeoServerDesktop.GeoServerClient.Migration")
            .Where(t => t.Name.EndsWith("Service") && t.Name != "ServiceBase") // 模型/清单类型不参与接口化
            .Where(t => !Excluded.Contains(t.Name))
            .OrderBy(t => t.FullName, StringComparer.Ordinal);

    private static IEnumerable<MethodInfo> PublicInstanceMethods(Type t) =>
        t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
         .Where(m => !m.IsStatic && !m.Name.StartsWith("get_") && !m.Name.StartsWith("set_")
                  && !m.Name.StartsWith("add_") && !m.Name.StartsWith("remove_")
                  && !typeof(object).GetMethods().Any(om => om.Name == m.Name && SameSig(om, m)));

    private static bool SameSig(MethodInfo a, MethodInfo b) =>
        a.GetParameters().Select(p => p.ParameterType).SequenceEqual(b.GetParameters().Select(p => p.ParameterType));

    [Fact]
    public void EveryServiceHasMatchingInterfaceAndImplementsIt()
    {
        var types = ServiceTypes().ToList();
        Assert.NotEmpty(types);

        var missing = new List<string>();
        foreach (var t in types)
        {
            var itf = Lib.GetType(t.Namespace + ".I" + t.Name, throwOnError: false);
            if (itf == null) { missing.Add(t.Name + " 缺接口 I" + t.Name); continue; }
            if (!itf.IsAssignableFrom(t)) missing.Add(t.Name + " 未实现 I" + t.Name);
        }
        Assert.True(missing.Count == 0, string.Join("；", missing));
    }

    [Fact]
    public void InterfaceMembersMirrorPublicServiceSurface_BothDirections()
    {
        var drift = new List<string>();
        foreach (var t in ServiceTypes())
        {
            var itf = Lib.GetType(t.Namespace + ".I" + t.Name);
            if (itf == null) continue;

            var impl = PublicInstanceMethods(t).ToList();
            var iface = itf.GetMethods().ToList();

            foreach (var m in impl)
            {
                var match = iface.FirstOrDefault(i => i.Name == m.Name && SameSig(i, m));
                if (match == null)
                    drift.Add(t.Name + "." + m.Name + " 未出现在 I" + t.Name);
                else if (match.ReturnType != m.ReturnType)
                    drift.Add(t.Name + "." + m.Name + " 返回类型不一致");
            }
            foreach (var i in iface)
            {
                if (!impl.Any(m => m.Name == i.Name && SameSig(i, m)))
                    drift.Add("I" + t.Name + "." + i.Name + " 在实现类中不存在（接口漂移）");
            }
        }
        Assert.True(drift.Count == 0, string.Join("；", drift));
    }

    [Fact]
    public void FactoryCreateMethodsReturnInterfaceImplementingTypes()
    {
        var factory = typeof(GeoServerDesktop.GeoServerClient.Configuration.GeoServerClientFactory);
        var creates = factory.GetMethods().Where(m => m.Name.StartsWith("Create") && m.Name.EndsWith("Service")).ToList();
        Assert.NotEmpty(creates);

        foreach (var c in creates)
        {
            var ret = c.ReturnType;
            if (ret == typeof(GeoServerDesktop.GeoServerClient.Services.PreviewService)) continue;
            var itf = Lib.GetType(ret.Namespace + ".I" + ret.Name, throwOnError: false);
            Assert.True(itf != null && itf.IsAssignableFrom(ret),
                c.Name + " 返回类型 " + ret.Name + " 没有对应接口");
        }
    }

    [Fact]
    public void InterfacesAreAllPublicAndInExpectedNamespace()
    {
        var ifaces = Lib.GetTypes().Where(t => t.IsInterface && t.Name.StartsWith("I") && t.Name.Length > 1
                     && (t.Namespace == "GeoServerDesktop.GeoServerClient.Services"
                      || t.Namespace == "GeoServerDesktop.GeoServerClient.Migration")).ToList();
        Assert.Equal(ServiceTypes().Count(), ifaces.Count);
        Assert.All(ifaces, i => Assert.True(i.IsPublic, i.Name + " 必须公开"));
    }
}
