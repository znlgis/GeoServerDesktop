using System.Collections.Generic;

namespace GeoServerDesktop.GeoServerClient.Models
{
    /// <summary>
    /// 样式使用情况：样式名 + 引用它的图层（默认样式绑定）。
    /// </summary>
    public sealed class StyleUsage
    {
        /// <summary>样式名。</summary>
        public string StyleName { get; set; }

        /// <summary>引用该样式为默认样式的图层（"workspace:layer" 全名，按发现顺序）。</summary>
        public List<string> Layers { get; set; } = new List<string>();

        /// <summary>是否被引用（Layers 非空）。</summary>
        public bool IsUsed
        {
            get { return Layers.Count > 0; }
        }
    }
}
