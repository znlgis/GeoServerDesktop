using System.Collections.Generic;
using GeoServerDesktop.App.Services;

namespace GeoServerDesktop.App.ViewModels
{
    /// <summary>
    /// 样式库条目展示包装（M3）：样式名 + 引用图层（qualified 名列表）+ 是否在用。
    /// </summary>
    public sealed class StyleUsageItem
    {
        private static LocalizationService L => LocalizationService.Instance;

        /// <summary>样式名</summary>
        public string StyleName { get; }

        /// <summary>是否被至少一个图层引用为默认样式</summary>
        public bool IsUsed { get; }

        /// <summary>引用图层（"workspace:layer" 全名）</summary>
        public IReadOnlyList<string> Layers { get; }

        /// <summary>
        /// 初始化 StyleUsageItem 类的新实例
        /// </summary>
        public StyleUsageItem(string styleName, bool isUsed, IReadOnlyList<string> layers)
        {
            StyleName = styleName;
            IsUsed = isUsed;
            Layers = layers;
        }

        /// <summary>引用图层展示（未引用时显示本地化"未引用"）</summary>
        public string LayersDisplay => IsUsed ? string.Join(", ", Layers) : L.StyleLibUnused;
    }
}
