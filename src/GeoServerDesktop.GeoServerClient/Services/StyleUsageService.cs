using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// 样式使用关系聚合服务：全局样式 × 图层默认样式绑定 → 使用总览 + 未引用检测。
    /// 数据源：样式列表（一次）+ 图层列表（一次）+ 逐图层详情（REST 列表不含 defaultStyle，须逐个取）。
    /// 引用匹配：defaultStyle 的 href 含 "/workspaces/" 视为工作空间样式引用（不计入全局样式总览）；
    /// 否则按名字精确匹配全局样式（服务端资源名大小写敏感；在用样式删除有服务端 403 保护兜底）。
    /// </summary>
    public class StyleUsageService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// 初始化 StyleUsageService 类的新实例
        /// </summary>
        /// <param name="httpClient">用于 GeoServer 操作的 HTTP 客户端</param>
        public StyleUsageService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <summary>
        /// 聚合样式使用总览：每个全局样式及其被哪些图层引用（作为默认样式）。
        /// </summary>
        /// <returns>使用情况数组（含未引用样式；顺序与样式列表一致）。</returns>
        public async Task<StyleUsage[]> GetStyleUsageAsync()
        {
            var styleSvc = new StyleService(_httpClient);
            var layerSvc = new LayerService(_httpClient);

            var styles = await styleSvc.GetStylesAsync();
            var layers = await layerSvc.GetLayersAsync();

            var map = new Dictionary<string, StyleUsage>(StringComparer.Ordinal);
            var list = new List<StyleUsage>();
            foreach (var style in styles)
            {
                if (style == null || string.IsNullOrEmpty(style.Name)) continue;
                if (map.ContainsKey(style.Name)) continue;
                var usage = new StyleUsage { StyleName = style.Name };
                map[style.Name] = usage;
                list.Add(usage);
            }

            foreach (var layer in layers)
            {
                if (layer == null || string.IsNullOrEmpty(layer.Name)) continue;

                Layer detail;
                try
                {
                    detail = await layerSvc.GetLayerAsync(layer.Name);
                }
                catch
                {
                    continue; // 单图层详情失败不中断总览（best-effort）
                }

                var styleRef = detail == null ? null : detail.DefaultStyle;
                if (styleRef == null || string.IsNullOrEmpty(styleRef.Name)) continue;

                // 工作空间样式引用（href 含 /workspaces/）不计入全局样式总览
                if (styleRef.Href != null
                    && styleRef.Href.IndexOf("/workspaces/", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;

                StyleUsage usage;
                if (map.TryGetValue(styleRef.Name, out usage))
                    usage.Layers.Add(layer.Name);
            }

            return list.ToArray();
        }

        /// <summary>
        /// 未引用样式名：无任何图层作为默认样式引用（删除这类样式不会触发在用保护）。
        /// </summary>
        /// <returns>未引用样式名数组。</returns>
        public async Task<string[]> GetUnusedStyleNamesAsync()
        {
            var usages = await GetStyleUsageAsync();
            var unused = new List<string>();
            foreach (var usage in usages)
            {
                if (!usage.IsUsed) unused.Add(usage.StyleName);
            }
            return unused.ToArray();
        }
    }
}
