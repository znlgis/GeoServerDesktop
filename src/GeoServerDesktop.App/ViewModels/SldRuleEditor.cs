using CommunityToolkit.Mvvm.ComponentModel;

namespace GeoServerDesktop.App.ViewModels
{
    /// <summary>
    /// SLD 编辑器的单条规则编辑项（M3 结构化模式）：
    /// 符号化器类型（点/线/面）+ 颜色/尺寸字段 + 可选过滤表达式。
    /// 数字字段以字符串承载（容忍用户输入中间态），构建文档时按 InvariantCulture 解析。
    /// </summary>
    public partial class SldRuleEditor : ObservableObject
    {
        /// <summary>可选的符号化器类型（与 SldBuilder 的模型子类对应）。</summary>
        public static readonly string[] SymbolizerKinds = { "Point", "Line", "Polygon" };

        /// <summary>可选的过滤操作符（与 SldBuilder/SldValidator 支持集合一致）。</summary>
        public static readonly string[] FilterOperators = { "=", "<>", ">", "<", ">=", "<=", "LIKE" };

        /// <summary>规则名（可选）</summary>
        [ObservableProperty]
        private string _name = string.Empty;

        /// <summary>符号化器类型：Point / Line / Polygon</summary>
        [ObservableProperty]
        private string _symbolizerKind = "Polygon";

        /// <summary>点形状（仅点符号）：circle / square / triangle / star 等</summary>
        [ObservableProperty]
        private string _wellKnownName = "circle";

        /// <summary>点尺寸（仅点符号）</summary>
        [ObservableProperty]
        private string _size = "6";

        /// <summary>填充颜色（点/面）</summary>
        [ObservableProperty]
        private string _fillColor = "#FF0000";

        /// <summary>填充不透明度 0-1（点/面）</summary>
        [ObservableProperty]
        private string _fillOpacity = "1";

        /// <summary>描边颜色</summary>
        [ObservableProperty]
        private string _strokeColor = "#000000";

        /// <summary>描边宽度</summary>
        [ObservableProperty]
        private string _strokeWidth = "1";

        /// <summary>是否启用过滤表达式</summary>
        [ObservableProperty]
        private bool _filterEnabled;

        /// <summary>过滤属性名</summary>
        [ObservableProperty]
        private string _filterProperty = string.Empty;

        /// <summary>过滤操作符</summary>
        [ObservableProperty]
        private string _filterOperator = "=";

        /// <summary>过滤值</summary>
        [ObservableProperty]
        private string _filterValue = string.Empty;

        /// <summary>是否点符号（控制点专有字段可见性）</summary>
        public bool IsPoint => SymbolizerKind == "Point";

        /// <summary>是否面符号</summary>
        public bool IsPolygon => SymbolizerKind == "Polygon";

        /// <summary>是否线符号</summary>
        public bool IsLine => SymbolizerKind == "Line";

        /// <summary>是否显示填充字段（点/面）</summary>
        public bool HasFill => IsPoint || IsPolygon;

        partial void OnSymbolizerKindChanged(string value)
        {
            OnPropertyChanged(nameof(IsPoint));
            OnPropertyChanged(nameof(IsPolygon));
            OnPropertyChanged(nameof(IsLine));
            OnPropertyChanged(nameof(HasFill));
        }
    }
}
