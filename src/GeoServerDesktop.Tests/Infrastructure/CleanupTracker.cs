using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace GeoServerDesktop.Tests.Infrastructure
{
    /// <summary>
    /// gdtest 前缀资源登记与兜底清理：显式登记 + 结束时全量扫描孤儿。
    /// 用裸 HttpClient 直发 REST（不经被测客户端），保证清理路径独立于被测代码。
    /// </summary>
    public sealed class CleanupTracker
    {
        private static readonly HttpClient Http = NewClient();

        private static HttpClient NewClient()
        {
            var c = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.ASCII.GetBytes(TestEnv.User + ":" + TestEnv.Pass)));
            c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return c;
        }

        public readonly SortedSet<string> Workspaces = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        public readonly List<string> LayerGroups = new List<string>();          // 全局
        public readonly List<string> Styles = new List<string>();               // 全局
        public readonly List<string> Users = new List<string>();
        public readonly List<string> Groups = new List<string>();
        public readonly List<string> PgTables = new List<string>();

        public void TrackWorkspace(string ws) { Workspaces.Add(ws); }
        public void TrackUser(string u) { if (u != null && u.StartsWith(TestEnv.Prefix, StringComparison.OrdinalIgnoreCase)) Users.Add(u); }
        public void TrackGroup(string g) { if (g != null && g.StartsWith(TestEnv.Prefix, StringComparison.OrdinalIgnoreCase)) Groups.Add(g); }
        public void TrackStyle(string s) { if (s != null && s.StartsWith(TestEnv.Prefix, StringComparison.OrdinalIgnoreCase)) Styles.Add(s); }
        public void TrackLayerGroup(string g) { if (g != null && g.StartsWith(TestEnv.Prefix, StringComparison.OrdinalIgnoreCase)) LayerGroups.Add(g); }
        public void TrackPgTable(string t) { PgTables.Add(t); }

        public async Task<HttpResponseMessage> DeleteAsync(string pathWithQuery)
        {
            return await Http.DeleteAsync(TestEnv.RestBase.TrimEnd('/') + pathWithQuery).ConfigureAwait(false);
        }

        /// <summary>裸 POST（如 /rest/reload），独立于被测客户端。</summary>
        public async Task<HttpResponseMessage> PostAsync(string pathWithQuery)
        {
            return await Http.PostAsync(TestEnv.RestBase.TrimEnd('/') + pathWithQuery,
                new StringContent("")).ConfigureAwait(false);
        }

        /// <summary>GET 仅取状态码（不关心响应体），失败/不可达返回 null。</summary>
        public async Task<int?> GetStatusCodeAsync(string path)
        {
            try
            {
                var resp = await Http.GetAsync(TestEnv.RestBase.TrimEnd('/') + path).ConfigureAwait(false);
                return (int)resp.StatusCode;
            }
            catch { return null; }
        }

        public async Task<string> GetAsync(string path)
        {
            try
            {
                var resp = await Http.GetAsync(TestEnv.RestBase.TrimEnd('/') + path).ConfigureAwait(false);
                return await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch { return null; }
        }

        /// <summary>兜底扫描：删除所有 gdtest 前缀的工作空间/样式/图层组/用户/组。best-effort。</summary>
        public void CleanupOrphansBestEffort()
        {
            try
            {
                CleanupOrphansBestEffortAsync().GetAwaiter().GetResult();
            }
            catch { }
        }

        public async Task CleanupOrphansBestEffortAsync()
        {
            if (!GeoServerAvailability.IsGeoServerReachable) return;

            // 1. 工作空间（含级联存储/图层/工作空间级样式与图层组）
            // 共享 e2e 夹具工作空间（Layer2/3 常驻复用）保留：其生命周期由 E2ePublisher/E2ePublishHelper
            // 管理，彻底清理走 harness 收尾或手工脚本；其余 gdtest 前缀一律扫掉。
            var keep = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "gdtest_ws_e2e" };
            var wsJson = await GetAsync("/rest/workspaces.json");
            foreach (var name in ExtractNames(wsJson, "workspace"))
                if (name.StartsWith(TestEnv.Prefix, StringComparison.OrdinalIgnoreCase) && !keep.Contains(name))
                    try { await DeleteAsync("/rest/workspaces/" + Uri.EscapeDataString(name) + "?recurse=true"); } catch { }

            // 1b. 命名空间（列表项只有 prefix 没有 name；且工作空间级联不清理命名空间，单独扫描）
            var nsJson = await GetAsync("/rest/namespaces.json");
            if (!string.IsNullOrEmpty(nsJson))
                foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(nsJson, "\"prefix\"\\s*:\\s*\"([^\"]+)\""))
                    if (m.Groups[1].Value.StartsWith(TestEnv.Prefix, StringComparison.OrdinalIgnoreCase))
                        try { await DeleteAsync("/rest/namespaces/" + Uri.EscapeDataString(m.Groups[1].Value)); } catch { }

            // 2. 全局图层组
            var lgJson = await GetAsync("/rest/layergroups.json");
            foreach (var name in ExtractNames(lgJson, "layerGroup"))
                if (name.StartsWith(TestEnv.Prefix, StringComparison.OrdinalIgnoreCase))
                    try { await DeleteAsync("/rest/layergroups/" + Uri.EscapeDataString(name)); } catch { }

            // 3. 全局样式（被引用时删除会失败，先跳过即可）
            var stJson = await GetAsync("/rest/styles.json");
            foreach (var name in ExtractNames(stJson, "style"))
                if (name.StartsWith(TestEnv.Prefix, StringComparison.OrdinalIgnoreCase))
                    try { await DeleteAsync("/rest/styles/" + Uri.EscapeDataString(name) + "?purge=true"); } catch { }

            // 4. 用户与组（3.0.1 新安全 REST 无单用户 DELETE 路由，REST 删除必 404——兜底走文件级删除+reload）
            var uJson = await GetAsync("/rest/security/usergroup/users.json");
            foreach (var name in ExtractNames(uJson, "user"))
                if (name.StartsWith(TestEnv.Prefix, StringComparison.OrdinalIgnoreCase))
                {
                    try { await DeleteAsync("/rest/security/usergroup/users/" + Uri.EscapeDataString(name)); } catch { }
                    try { RemoveUserViaRegistryFile(name); await PostAsync("/rest/reload"); } catch { }
                }
            var gJson = await GetAsync("/rest/security/usergroup/groups.json");
            foreach (var name in ExtractNames(gJson, "group"))
                if (name.StartsWith(TestEnv.Prefix, StringComparison.OrdinalIgnoreCase))
                    try { await DeleteAsync("/rest/security/usergroup/groups/" + Uri.EscapeDataString(name)); } catch { }
        }

        /// <summary>
        /// 文件级用户清理：直接编辑宿主机可见的 data_dir/security/usergroup/default/users.xml
        /// 删除对应用户行（GeoServer 3.0.1 实测单用户 GET/PUT/DELETE 路由全部不存在，REST 无法删用户）。
        /// 调用方随后需 POST /rest/reload 让服务器重读。文件不存在/结构异常时静默返回。
        /// </summary>
        public static void RemoveUserViaRegistryFile(string username)
        {
            var path = System.IO.Path.Combine(TestEnv.DataDirRoot, "security", "usergroup", "default", "users.xml");
            if (!System.IO.File.Exists(path) || string.IsNullOrEmpty(username)) return;
            var lines = System.IO.File.ReadAllLines(path);
            var kept = new List<string>();
            bool changed = false;
            foreach (var l in lines)
                if (l.Contains("name=\"" + username + "\"")) changed = true;
                else kept.Add(l);
            if (changed) System.IO.File.WriteAllLines(path, kept);
        }

        /// <summary>从 GeoServer JSON 中抽取 "xxx":[{...}] 或 "xxx":["name",...] 的 name 字段，轻量正则，不用 JSON 库以保持独立。</summary>
        public static IEnumerable<string> ExtractNames(string json, string arrayKey)
        {
            var results = new List<string>();
            if (string.IsNullOrEmpty(json)) return results;
            int idx = json.IndexOf("\"" + arrayKey + "\"", StringComparison.Ordinal);
            while (idx >= 0)
            {
                int colon = json.IndexOf(':', idx);
                if (colon < 0) break;
                int lb = json.IndexOf('[', colon);
                if (lb < 0) break;
                int rb = MatchBracket(json, lb);
                if (rb < 0) break;
                var body = json.Substring(lb + 1, rb - lb - 1);
                int p = 0;
                while (true)
                {
                    int obj = body.IndexOf("{\"name\":", p, StringComparison.Ordinal);
                    if (obj < 0) obj = body.IndexOf("{ \"name\":", p, StringComparison.Ordinal);
                    if (obj < 0) break;
                    int q = body.IndexOf('"', obj + 1);            // 值起始引号
                    int s1 = body.IndexOf(':', obj) + 1;
                    int s2 = body.IndexOf('"', s1);
                    if (s2 < 0) break;
                    int s3 = body.IndexOf('"', s2 + 1);
                    if (s3 < 0) break;
                    results.Add(body.Substring(s2 + 1, s3 - s2 - 1));
                    p = s3;
                }
                // 数组元素为裸字符串的情况（部分 GWC 端点）
                if (results.Count == 0)
                {
                    foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(body, "\"([^\"]+)\""))
                        results.Add(m.Groups[1].Value);
                }
                idx = json.IndexOf("\"" + arrayKey + "\"", rb, StringComparison.Ordinal);
            }
            return results;
        }

        private static int MatchBracket(string s, int open)
        {
            int depth = 0; bool inStr = false; char prev = '\0';
            for (int i = open; i < s.Length; i++)
            {
                char ch = s[i];
                if (inStr) { if (ch == '"' && prev != '\\') inStr = false; prev = ch; continue; }
                if (ch == '"') inStr = true;
                else if (ch == '[') depth++;
                else if (ch == ']') { depth--; if (depth == 0) return i; }
                prev = ch;
            }
            return -1;
        }
    }
}
