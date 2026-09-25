using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GeoServerDesktop.GeoServerClient.Migration
{
    /// <summary>设置比对域（路径与根键按 GeoServer 3.0.1 实测契约）。</summary>
    public enum SettingsDomain
    {
        /// <summary>全局设置（GET/PUT /rest/settings，根 {"global"}）。</summary>
        Global = 0,

        /// <summary>WMS 服务设置（/rest/services/wms/settings，根 {"wms"}）。</summary>
        Wms = 1,

        /// <summary>WFS 服务设置（/rest/services/wfs/settings，根 {"wfs"}）。</summary>
        Wfs = 2,

        /// <summary>WCS 服务设置（/rest/services/wcs/settings，根 {"wcs"}）。</summary>
        Wcs = 3,

        /// <summary>WMTS 服务设置（/rest/services/wmts/settings，根 {"wmts"}）。</summary>
        Wmts = 4,
    }

    /// <summary>单项设置差异。</summary>
    public sealed class SettingsDiffItem
    {
        /// <summary>叶子路径（如 "settings.contact.addressCity"）。</summary>
        public string Path { get; set; }

        /// <summary>源实例值（JSON 字面量）。</summary>
        public string SourceValue { get; set; }

        /// <summary>目标实例值（JSON 字面量；缺失时为 null）。</summary>
        public string TargetValue { get; set; }

        /// <summary>差异类型。</summary>
        public SettingsDiffKind Kind { get; set; }
    }

    /// <summary>差异类型。</summary>
    public enum SettingsDiffKind
    {
        /// <summary>两侧值不同。</summary>
        Changed = 0,

        /// <summary>仅源侧存在。</summary>
        AddedInSource = 1,

        /// <summary>仅目标侧存在。</summary>
        RemovedInSource = 2,
    }

    /// <summary>一次设置比对的结果（含两侧原始 JSON，供选择性应用）。</summary>
    public sealed class SettingsDiffResult
    {
        /// <summary>域。</summary>
        public SettingsDomain Domain { get; set; }

        /// <summary>差异项（按路径排序）。</summary>
        public List<SettingsDiffItem> Items { get; set; } = new List<SettingsDiffItem>();

        /// <summary>源实例原始 JSON（完整包装）。</summary>
        public string SourceJson { get; set; }

        /// <summary>目标实例原始 JSON（完整包装）。</summary>
        public string TargetJson { get; set; }

        /// <summary>是否无差异。</summary>
        public bool IsIdentical { get { return Items.Count == 0; } }
    }

    /// <summary>
    /// 设置比对与选择性应用（M4 设置同步）：读取两个实例的同域设置 → 叶子级 JSON 差异 →
    /// 勾选路径合并回目标整包 → PUT 回写（保持既有"整包替换 + ExtensionData 防丢键"契约：
    /// 应用以目标实例原始 JSON 为基底，仅覆写勾选路径，未勾选键原样保留）。
    /// 纯静态计算（<see cref="Compare"/> / <see cref="Apply"/>）可离线测试；
    /// <see cref="SettingsCompareService"/> 负责经 HTTP 客户端读写。
    /// 数组按整体叶子比对（不做元素级 diff——GeoServer 设置数组语义为整体替换）。
    /// </summary>
    public static class SettingsCompare
    {
        /// <summary>各域的路径与根键。</summary>
        public static string PathFor(SettingsDomain domain)
        {
            switch (domain)
            {
                case SettingsDomain.Global: return "/rest/settings.json";
                case SettingsDomain.Wms: return "/rest/services/wms/settings.json";
                case SettingsDomain.Wfs: return "/rest/services/wfs/settings.json";
                case SettingsDomain.Wcs: return "/rest/services/wcs/settings.json";
                case SettingsDomain.Wmts: return "/rest/services/wmts/settings.json";
                default: throw new ArgumentOutOfRangeException(nameof(domain));
            }
        }

        /// <summary>各域的 PUT 路径（无 .json 后缀，与写请求形态一致）。</summary>
        public static string PutPathFor(SettingsDomain domain)
        {
            var p = PathFor(domain);
            return p.EndsWith(".json", StringComparison.Ordinal) ? p.Substring(0, p.Length - 5) : p;
        }

        /// <summary>各域的响应根键。</summary>
        public static string RootKeyFor(SettingsDomain domain)
        {
            switch (domain)
            {
                case SettingsDomain.Global: return "global";
                case SettingsDomain.Wms: return "wms";
                case SettingsDomain.Wfs: return "wfs";
                case SettingsDomain.Wcs: return "wcs";
                case SettingsDomain.Wmts: return "wmts";
                default: throw new ArgumentOutOfRangeException(nameof(domain));
            }
        }

        /// <summary>
        /// 比对两侧原始 JSON（volatile 键不计入：id、updateSequence、href、dateCreated、dateModified）。
        /// <paramref name="rootKey"/> 非空时覆盖域默认根键（服务级 WMS 设置为平铺字段，无 service 包装——实测 3.0.1）。
        /// </summary>
        public static SettingsDiffResult Compare(
            SettingsDomain domain, string sourceJson, string targetJson,
            string rootKey = null)
        {
            if (sourceJson == null) throw new ArgumentNullException(nameof(sourceJson));
            if (targetJson == null) throw new ArgumentNullException(nameof(targetJson));
            var rk = rootKey ?? RootKeyFor(domain);
            var source = Unwrap(rk, sourceJson);
            var target = Unwrap(rk, targetJson);

            var result = new SettingsDiffResult
            {
                Domain = domain,
                SourceJson = sourceJson,
                TargetJson = targetJson,
            };

            var leaves = new SortedDictionary<string, SettingsDiffItem>(StringComparer.Ordinal);
            // 路径统一相对解包后的根对象（global 域即 settings.contact.x；wms 域为平铺字段 citeCompliant）
            Collect((JObject)source, "", (JObject)target, leaves);
            result.Items.AddRange(leaves.Values);
            return result;
        }

        /// <summary>
        /// 将勾选路径从源侧合并到目标侧，返回完整 PUT 载荷（含根包装）。
        /// </summary>
        /// <param name="domain">域。</param>
        /// <param name="sourceJson">源实例原始 JSON。</param>
        /// <param name="targetJson">目标实例原始 JSON。</param>
        /// <param name="paths">勾选的差异路径。</param>
        /// <returns>PUT 请求体 JSON。</returns>
        public static string Apply(
            SettingsDomain domain, string sourceJson, string targetJson, IEnumerable<string> paths,
            string rootKey = null)
        {
            if (paths == null) throw new ArgumentNullException(nameof(paths));
            var rk = rootKey ?? RootKeyFor(domain);
            var source = Unwrap(rk, sourceJson ?? throw new ArgumentNullException(nameof(sourceJson)));
            var target = Unwrap(rk, targetJson ?? throw new ArgumentNullException(nameof(targetJson)));

            foreach (var path in paths)
            {
                if (string.IsNullOrEmpty(path)) continue;
                var token = SelectToken(source, path);
                var existing = SelectToken(target, path);
                if (token == null)
                {
                    // RemovedInSource：目标侧删除该键（并回收变空的父对象）
                    if (existing != null)
                    {
                        var parentChain = new System.Collections.Generic.List<JToken>();
                        var p = existing.Parent;
                        while (p is JProperty prop)
                        {
                            parentChain.Add(prop.Parent);
                            p = prop.Parent.Parent;
                        }
                        existing.Parent.Remove();
                        foreach (var ancestor in parentChain)
                        {
                            var obj = ancestor as JObject;
                            if (obj != null && obj.Count == 0) obj.Parent?.Remove();
                            else break;
                        }
                    }
                    continue;
                }
                if (existing != null)
                {
                    existing.Replace(token.DeepClone());
                }
                else
                {
                    SetToken(target, path, token.DeepClone());
                }
            }

            var wrapped = new JObject { { RootKeyFor(domain), target } };
            return wrapped.ToString(Formatting.None);
        }

        // ---------- 内部 ----------

        private static JObject Unwrap(string rootKey, string json)
        {
            var parsed = JObject.Parse(json);
            var root = parsed[rootKey] as JObject;
            return root ?? parsed;
        }

        private static readonly HashSet<string> VolatileKeys = new HashSet<string>(StringComparer.Ordinal)
        {
            "id", "updateSequence", "href", "dateCreated", "dateModified",
        };

        private static void Collect(JObject obj, string prefix, JObject other, SortedDictionary<string, SettingsDiffItem> leaves)
        {
            foreach (var prop in obj.Properties())
            {
                if (VolatileKeys.Contains(prop.Name)) continue;
                string path = prefix.Length == 0 ? prop.Name : prefix + "." + prop.Name;

                if (prop.Value is JObject child)
                {
                    Collect(child, path, other != null ? other[prop.Name] as JObject : null, leaves);
                    continue;
                }

                var counterpart = other != null ? other[prop.Name] : null;
                if (other == null || !other.ContainsKey(prop.Name))
                {
                    leaves[path] = new SettingsDiffItem
                    {
                        Path = path,
                        SourceValue = Compact(prop.Value),
                        TargetValue = null,
                        Kind = SettingsDiffKind.AddedInSource,
                    };
                    continue;
                }
                if (!JToken.DeepEquals(Normalize(prop.Value), Normalize(counterpart)))
                {
                    leaves[path] = new SettingsDiffItem
                    {
                        Path = path,
                        SourceValue = Compact(prop.Value),
                        TargetValue = Compact(counterpart),
                        Kind = SettingsDiffKind.Changed,
                    };
                }
            }

            // 目标侧独有键 → RemovedInSource（子对象递归到叶子，删除路径粒度与添加对称）
            if (other != null)
            {
                foreach (var prop in other.Properties())
                {
                    if (VolatileKeys.Contains(prop.Name)) continue;
                    string path = prefix.Length == 0 ? prop.Name : prefix + "." + prop.Name;
                    if (obj != null && obj.ContainsKey(prop.Name)) continue;
                    if (prop.Value is JObject childOnly)
                    {
                        CollectEmptyBase(childOnly, path, leaves);
                        continue;
                    }
                    if (leaves.ContainsKey(path)) continue;
                    leaves[path] = new SettingsDiffItem
                    {
                        Path = path,
                        SourceValue = null,
                        TargetValue = Compact(prop.Value),
                        Kind = SettingsDiffKind.RemovedInSource,
                    };
                }
            }
        }

        /// <summary>源侧整枝缺失时，把目标侧子对象递归展开为叶子级 RemovedInSource 路径。</summary>
        private static void CollectEmptyBase(JObject obj, string prefix, SortedDictionary<string, SettingsDiffItem> leaves)
        {
            foreach (var prop in obj.Properties())
            {
                if (VolatileKeys.Contains(prop.Name)) continue;
                string path = prefix.Length == 0 ? prop.Name : prefix + "." + prop.Name;
                if (prop.Value is JObject child)
                {
                    CollectEmptyBase(child, path, leaves);
                    continue;
                }
                leaves[path] = new SettingsDiffItem
                {
                    Path = path,
                    SourceValue = null,
                    TargetValue = Compact(prop.Value),
                    Kind = SettingsDiffKind.RemovedInSource,
                };
            }
        }

        /// <summary>归一化：剥离叶子子树内的 volatile 键（防 id/updateSequence 噪声）。</summary>
        private static JToken Normalize(JToken token)
        {
            if (token is JObject obj)
            {
                var clone = (JObject)obj.DeepClone();
                StripVolatile(clone);
                return clone;
            }
            return token;
        }

        private static void StripVolatile(JObject obj)
        {
            foreach (var prop in obj.Properties().ToList())
            {
                if (VolatileKeys.Contains(prop.Name))
                {
                    prop.Remove();
                    continue;
                }
                if (prop.Value is JObject child) StripVolatile(child);
                else if (prop.Value is JArray arr)
                {
                    foreach (var item in arr.OfType<JObject>()) StripVolatile(item);
                }
            }
        }

        private static string Compact(JToken token)
        {
            return token == null || token.Type == JTokenType.Null ? null : token.ToString(Formatting.None);
        }

        private static JToken SelectToken(JObject root, string path)
        {
            JToken current = root;
            foreach (var segment in path.Split('.'))
            {
                var obj = current as JObject;
                if (obj == null) return null;
                current = obj[segment];
                if (current == null) return null;
            }
            return current;
        }

        private static void SetToken(JObject root, string path, JToken value)
        {
            var segments = path.Split('.');
            JToken current = root;
            for (int i = 0; i < segments.Length - 1; i++)
            {
                var obj = current as JObject;
                if (obj == null) return;
                var next = obj[segments[i]];
                if (next == null)
                {
                    next = new JObject();
                    obj[segments[i]] = next;
                }
                current = next;
            }
            var leaf = current as JObject;
            if (leaf != null) leaf[segments[segments.Length - 1]] = value;
        }
    }

    /// <summary>
    /// 设置比对服务：跨实例读取同域设置并产出差异；应用差异时以目标实例为基底合并勾选路径后整包 PUT。
    /// </summary>
    public class SettingsCompareService : ServiceBase
    {

        /// <summary>
        /// 初始化 SettingsCompareService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public SettingsCompareService(IGeoServerHttpClient httpClient)
            : base(httpClient)
        {
        }

        /// <summary>读取本实例指定域的设置原始 JSON。</summary>
        /// <param name="domain">域。</param>
        /// <returns>原始 JSON。</returns>
        public Task<string> ReadRawAsync(SettingsDomain domain)
        {
            return Http.GetAsync(SettingsCompare.PathFor(domain));
        }

        /// <summary>
        /// 读取任意路径的原始 JSON（工作空间级设置等同族端点）。
        /// </summary>
        /// <param name="getPath">GET 路径。</param>
        /// <returns>原始 JSON。</returns>
        public Task<string> ReadRawAtPathAsync(string getPath)
        {
            if (string.IsNullOrEmpty(getPath)) throw new ArgumentException("缺少路径", nameof(getPath));
            return Http.GetAsync(getPath);
        }

        /// <summary>
        /// 整包 PUT 任意设置路径（工作空间级设置等同族端点；载荷须为含根包装的完整 JSON）。
        /// </summary>
        /// <param name="putPath">PUT 路径（无 .json 后缀）。</param>
        /// <param name="wrappedJson">含根包装的完整 JSON。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task PutRawAsync(string putPath, string wrappedJson)
        {
            if (string.IsNullOrEmpty(putPath)) throw new ArgumentException("缺少路径", nameof(putPath));
            using (var content = new StringContent(wrappedJson ?? throw new ArgumentNullException(nameof(wrappedJson)),
                Encoding.UTF8, "application/json"))
            {
                await Http.PutAsync(putPath, content);
            }
        }

        /// <summary>读取两侧设置并比对（source/target 为两个实例上的本服务实例）。</summary>
        /// <param name="source">源实例服务。</param>
        /// <param name="target">目标实例服务。</param>
        /// <param name="domain">域。</param>
        /// <returns>差异结果。</returns>
        public static async Task<SettingsDiffResult> CompareAsync(SettingsCompareService source, SettingsCompareService target, SettingsDomain domain)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));
            var sourceJson = await source.ReadRawAsync(domain);
            var targetJson = await target.ReadRawAsync(domain);
            return SettingsCompare.Compare(domain, sourceJson, targetJson);
        }

        /// <summary>把勾选路径从源侧应用到本（目标）实例：合并整包后 PUT。</summary>
        /// <param name="sourceJson">源实例原始 JSON。</param>
        /// <param name="domain">域。</param>
        /// <param name="paths">勾选的差异路径。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task ApplyAsync(string sourceJson, SettingsDomain domain, IEnumerable<string> paths)
        {
            var targetJson = await ReadRawAsync(domain);
            var payload = SettingsCompare.Apply(domain, sourceJson, targetJson, paths);
            using (var content = new StringContent(payload, Encoding.UTF8, "application/json"))
            {
                await Http.PutAsync(SettingsCompare.PutPathFor(domain), content);
            }
        }
    }
}
