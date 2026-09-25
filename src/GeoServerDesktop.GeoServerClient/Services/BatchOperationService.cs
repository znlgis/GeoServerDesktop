using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// 批量操作服务（M4）：图层/样式/存储/工作空间的多选批量操作——批量改默认样式、批量启用/禁用存储、
    /// 批量删除（含级联选项）。逐项执行、部分成功语义（单项失败不中断整批，逐项结果汇总）。
    ///
    /// 实测真值（GeoServer 3.0.1，2026-09-25 探测）：
    /// ① PUT 支持局部更新（请求体仅含目标键，其余字段保持不变），批量操作因此无需回传整包；
    /// ② 图层（LayerInfo）REST 序列化不含 enabled 键——PUT {"layer":{"enabled":...}} 被静默忽略，
    ///    图层级"启停"无 REST 通道；启停的可用面在存储（dataStore/coverageStore 的 enabled 键），
    ///    禁用存储后其下图层即从 WMS/WFS 能力与出图中消失（实测 GetCapabilities 命中数归零、
    ///    GetFeature 报异常，恢复启用后正常）；
    /// ③ 引用中的全局样式 DELETE → 403（服务端删除保护），批量删样式按失败项上报而非中断；
    /// ④ DELETE 工作空间 recurse=true 级联回收其孤立命名空间（实测工作空间创建即自动建同名 namespace，
    ///    recurse 删除后 namespace 一并消失）。
    /// </summary>
    public class BatchOperationService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// 初始化 BatchOperationService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public BatchOperationService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <summary>
        /// 批量设置图层默认样式（PUT 局部更新：layer.name + defaultStyle）。
        /// </summary>
        /// <param name="qualifiedLayerNames">图层 qualified 名列表（workspace:layer，与 /rest/layers.json 一致）。</param>
        /// <param name="styleName">默认样式名（全局样式裸名；工作空间样式可带 workspace/ 前缀由调用方解析，此处直接透传名字）。</param>
        /// <returns>批量结果。</returns>
        public Task<BatchResult> SetLayersDefaultStyleAsync(IEnumerable<string> qualifiedLayerNames, string styleName)
        {
            if (string.IsNullOrEmpty(styleName)) throw new ArgumentException("缺少样式名", nameof(styleName));
            return RunAsync(qualifiedLayerNames, async layer =>
            {
                var body = new
                {
                    layer = new
                    {
                        name = layer,
                        defaultStyle = new { name = styleName },
                    },
                };
                await PutJsonAsync("/rest/layers/" + Uri.EscapeDataString(layer), body);
            });
        }

        /// <summary>
        /// 批量启用/禁用存储（dataStore 或 coverageStore 的 enabled 键；见类注释实测结论②——
        /// 图层级启停无 REST 通道，存储级启停即为对用户可用的"批量启停"面）。
        /// qualified 名（workspace:store）的目标先按 dataStore 尝试，404 时回落 coverageStore。
        /// </summary>
        /// <param name="targets">存储目标列表（Workspace 或 qualified Name）。</param>
        /// <param name="enabled">true 启用，false 禁用。</param>
        /// <returns>批量结果。</returns>
        public async Task<BatchResult> SetStoresEnabledAsync(IEnumerable<BatchTarget> targets, bool enabled)
        {
            var result = new BatchResult();
            if (targets == null) return result;
            foreach (var target in targets)
            {
                if (target == null || string.IsNullOrEmpty(target.Name)) continue;
                string workspace = target.Workspace;
                string name = target.Name;
                if (string.IsNullOrEmpty(workspace) && name.IndexOf(':') >= 0)
                {
                    var parts = name.Split(':');
                    workspace = parts[0];
                    name = parts[1];
                }
                var display = string.IsNullOrEmpty(workspace) ? target.Name : workspace + ":" + name;
                if (string.IsNullOrEmpty(workspace))
                {
                    result.Items.Add(BatchItemResult.Fail(display, "缺少工作空间名"));
                    continue;
                }

                try
                {
                    await PutStoreAsync(workspace, name, enabled, isCoverage: false);
                    result.Items.Add(BatchItemResult.Ok(display));
                }
                catch (GeoServerRequestException ex) when (ex.StatusCode == 404)
                {
                    // 实测：列表接口不区分 vector/raster 存储，404 回落 coverageStore
                    try
                    {
                        await PutStoreAsync(workspace, name, enabled, isCoverage: true);
                        result.Items.Add(BatchItemResult.Ok(display));
                    }
                    catch (Exception ex2)
                    {
                        result.Items.Add(BatchItemResult.Fail(display, Describe(ex2)));
                    }
                }
                catch (Exception ex)
                {
                    result.Items.Add(BatchItemResult.Fail(display, Describe(ex)));
                }
            }
            return result;
        }

        /// <summary>
        /// 批量删除图层。
        /// </summary>
        /// <param name="qualifiedLayerNames">图层 qualified 名列表。</param>
        /// <param name="recurse">是否级联删除关联资源。</param>
        /// <returns>批量结果。</returns>
        public Task<BatchResult> DeleteLayersAsync(IEnumerable<string> qualifiedLayerNames, bool recurse = false)
        {
            return RunAsync(qualifiedLayerNames, async layer =>
            {
                await _httpClient.DeleteAsync(
                    "/rest/layers/" + Uri.EscapeDataString(layer) + "?recurse=" + Lower(recurse));
            });
        }

        /// <summary>
        /// 批量删除样式（全局样式与 workspace/ 前缀的工作空间样式混合；引用中的样式按 403 失败项上报）。
        /// </summary>
        /// <param name="targets">样式目标列表。</param>
        /// <param name="purge">是否同时清除磁盘上的样式文件。</param>
        /// <returns>批量结果。</returns>
        public Task<BatchResult> DeleteStylesAsync(IEnumerable<BatchTarget> targets, bool purge = false)
        {
            return RunAsync(targets, async target =>
            {
                await _httpClient.DeleteAsync(StylePath(target) + "?purge=" + Lower(purge));
            });
        }

        /// <summary>
        /// 批量删除工作空间（recurse=true 级联其下存储/图层/工作空间样式/图层组，并回收孤立命名空间）。
        /// </summary>
        /// <param name="workspaceNames">工作空间名列表。</param>
        /// <param name="recurse">是否递归删除工作空间内全部资源。</param>
        /// <returns>批量结果。</returns>
        public Task<BatchResult> DeleteWorkspacesAsync(IEnumerable<string> workspaceNames, bool recurse = true)
        {
            return RunAsync(workspaceNames, async ws =>
            {
                await _httpClient.DeleteAsync(
                    "/rest/workspaces/" + Uri.EscapeDataString(ws) + "?recurse=" + Lower(recurse));
            });
        }

        // ---------- 内部辅助 ----------

        private static string StylePath(BatchTarget target)
        {
            if (target.IsWorkspaceStyle)
            {
                if (string.IsNullOrEmpty(target.Workspace))
                    throw new ArgumentException("工作空间样式目标缺少工作空间名", nameof(target));
                return "/rest/workspaces/" + Uri.EscapeDataString(target.Workspace)
                    + "/styles/" + Uri.EscapeDataString(target.StyleName);
            }
            return "/rest/styles/" + Uri.EscapeDataString(target.StyleName);
        }

        private async Task PutStoreAsync(string workspace, string store, bool enabled, bool isCoverage)
        {
            if (isCoverage)
            {
                await PutJsonAsync(
                    "/rest/workspaces/" + Uri.EscapeDataString(workspace) + "/coveragestores/" + Uri.EscapeDataString(store),
                    new { coverageStore = new { enabled = enabled } });
            }
            else
            {
                await PutJsonAsync(
                    "/rest/workspaces/" + Uri.EscapeDataString(workspace) + "/datastores/" + Uri.EscapeDataString(store),
                    new { dataStore = new { enabled = enabled } });
            }
        }

        private async Task PutJsonAsync(string path, object body)
        {
            var json = JsonConvert.SerializeObject(body, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
            });
            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            {
                await _httpClient.PutAsync(path, content);
            }
        }

        private async Task<BatchResult> RunAsync<T>(IEnumerable<T> items, Func<T, Task> op)
        {
            var result = new BatchResult();
            if (items == null) return result;
            foreach (var item in items)
            {
                var target = item as BatchTarget;
                string display = target != null ? target.QualifiedName : Convert.ToString(item);
                try
                {
                    if (item == null) continue;
                    await op(item);
                    result.Items.Add(BatchItemResult.Ok(display));
                }
                catch (Exception ex)
                {
                    result.Items.Add(BatchItemResult.Fail(display, Describe(ex)));
                }
            }
            return result;
        }

        private static string Describe(Exception ex)
        {
            var gs = ex as GeoServerRequestException;
            if (gs == null) return ex.Message;
            var text = gs.ResponseContent;
            if (string.IsNullOrWhiteSpace(text)) return "HTTP " + gs.StatusCode;
            text = TrimOneLine(text);
            return "HTTP " + gs.StatusCode + "：" + text;
        }

        private static string TrimOneLine(string text)
        {
            var sb = new StringBuilder();
            int skipped = 0;
            foreach (var ch in text.Trim())
            {
                if (ch == '\r' || ch == '\n' || ch == '\t')
                {
                    if (skipped == 0) sb.Append(' ');
                    skipped = (skipped + 1) % 2; // 连续空白折叠为一个空格
                    continue;
                }
                skipped = 0;
                sb.Append(ch);
            }
            var flat = sb.ToString();
            return flat.Length > 200 ? flat.Substring(0, 200) + "…" : flat;
        }

        private static string Lower(bool value)
        {
            return value ? "true" : "false";
        }
    }
}
