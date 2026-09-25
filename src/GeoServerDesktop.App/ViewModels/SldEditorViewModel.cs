using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoServerDesktop.App.Services;
using GeoServerDesktop.GeoServerClient.Sld;

namespace GeoServerDesktop.App.ViewModels
{
    /// <summary>
    /// SLD 编辑器视图模型（M3）：结构化模式（点/线/面符号 + 过滤表达式）与源码模式（XML 直接编辑）
    /// 双模式切换，应用前经 SldValidator 本地校验，保存为全局样式（自动判别创建/更新），
    /// 预览生成 WMS GetMap 地址（与 MapPreview 同技术）。
    /// 名字信任边界（实测 3.0.1）：加载已有样式时以 REST 资源名为准（SLD 内 NamedLayer 名会被服务端规范化）。
    /// </summary>
    public partial class SldEditorViewModel : ViewModelBase
    {
        private readonly IGeoServerConnectionService _connectionService;

        /// <summary>可选样式名列表（全局样式）</summary>
        [ObservableProperty]
        private ObservableCollection<string> _styles = new();

        /// <summary>在列表中选中的样式（加载来源）</summary>
        [ObservableProperty]
        private string? _selectedStyle;

        /// <summary>编辑/保存的目标样式名</summary>
        [ObservableProperty]
        private string _styleName = string.Empty;

        /// <summary>是否源码模式（false=结构化模式）</summary>
        [ObservableProperty]
        private bool _isSourceMode;

        /// <summary>源码模式的 SLD XML 文本</summary>
        [ObservableProperty]
        private string _sourceXml = string.Empty;

        /// <summary>结构化模式的规则编辑项列表</summary>
        public ObservableCollection<SldRuleEditor> Rules { get; } = new();

        /// <summary>选中的规则编辑项</summary>
        [ObservableProperty]
        private SldRuleEditor? _selectedRule;

        /// <summary>预览可选图层（qualified 名）</summary>
        [ObservableProperty]
        private ObservableCollection<string> _previewLayers = new();

        /// <summary>选中的预览图层</summary>
        [ObservableProperty]
        private string? _selectedPreviewLayer;

        /// <summary>生成的 WMS GetMap 预览地址</summary>
        [ObservableProperty]
        private string? _previewUrl;

        /// <summary>状态消息</summary>

        /// <summary>是否正在加载</summary>

        /// <summary>
        /// 初始化 SldEditorViewModel 类的新实例
        /// </summary>
        /// <param name="connectionService">GeoServer 连接服务</param>
        public SldEditorViewModel(IGeoServerConnectionService connectionService)
        : base(connectionService)
        {
            _connectionService = connectionService;
            StatusMessage = L.SldEditorReady;
        }

        /// <summary>
        /// 加载全局样式列表
        /// </summary>
        [RelayCommand]
        private async Task LoadStylesAsync()
        {
            IsLoading = true;
            try
            {
                await RefreshStylesAsync();
                StatusMessage = string.Format(L.SldEditorStatusStylesLoaded, Styles.Count);
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(L.SldEditorStatusLoadFailed, ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 加载选中样式的 SLD 并解析为结构化规则（名字以 REST 资源名为准）
        /// </summary>
        [RelayCommand]
        private async Task LoadSelectedStyleAsync()
        {
            if (string.IsNullOrWhiteSpace(SelectedStyle))
            {
                StatusMessage = L.SldEditorStatusSelectStyle;
                return;
            }

            IsLoading = true;
            try
            {
                var sld = await _connectionService.GetStyleService().GetStyleSldAsync(SelectedStyle);
                var parsed = SldParser.Parse(sld);
                if (!parsed.Success)
                {
                    StatusMessage = string.Format(L.SldEditorStatusParseFailed, parsed.Error);
                    return;
                }

                StyleName = SelectedStyle;
                SourceXml = sld;
                Rules.Clear();
                SldEditorMapper.ApplyDocument(parsed.Document!, Rules);
                SelectedRule = Rules.FirstOrDefault();
                IsSourceMode = false;
                PreviewUrl = null;

                StatusMessage = parsed.Warnings.Count > 0
                    ? string.Format(L.SldEditorStatusStyleLoadedWarn, SelectedStyle, string.Join("; ", parsed.Warnings))
                    : string.Format(L.SldEditorStatusStyleLoaded, SelectedStyle);
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(L.SldEditorStatusLoadFailed, ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 清空并开始新建样式（给一条默认规则）
        /// </summary>
        [RelayCommand]
        private void NewStyle()
        {
            StyleName = string.Empty;
            SourceXml = string.Empty;
            PreviewUrl = null;
            Rules.Clear();
            Rules.Add(new SldRuleEditor());
            SelectedRule = Rules[0];
            IsSourceMode = false;
            StatusMessage = L.SldEditorStatusNewReady;
        }

        /// <summary>
        /// 添加一条规则
        /// </summary>
        [RelayCommand]
        private void AddRule()
        {
            var e = new SldRuleEditor();
            Rules.Add(e);
            SelectedRule = e;
            StatusMessage = L.SldEditorStatusRuleAdded;
        }

        /// <summary>
        /// 删除选中规则
        /// </summary>
        [RelayCommand]
        private void RemoveRule()
        {
            if (SelectedRule == null)
            {
                StatusMessage = L.SldEditorStatusNoRuleSelected;
                return;
            }

            var idx = Rules.IndexOf(SelectedRule);
            Rules.Remove(SelectedRule);
            SelectedRule = Rules.Count > 0 ? Rules[Math.Min(idx, Rules.Count - 1)] : null;
            StatusMessage = L.SldEditorStatusRuleRemoved;
        }

        /// <summary>
        /// 切换到源码模式：从结构化模型生成 XML（需要样式名作为 LayerName）
        /// </summary>
        [RelayCommand]
        private void SwitchToSource()
        {
            if (string.IsNullOrWhiteSpace(StyleName))
            {
                StatusMessage = L.SldEditorStatusNeedName;
                return;
            }

            try
            {
                var doc = SldEditorMapper.ToDocument(StyleName, Rules);
                SourceXml = SldBuilder.Build(doc);
                IsSourceMode = true;
                StatusMessage = L.SldEditorStatusSwitchedToSource;
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(L.SldEditorStatusSwitchFailed, ex.Message);
            }
        }

        /// <summary>
        /// 切换到结构化模式：解析源码 XML（失败则留在源码模式；超子集内容给警告）
        /// </summary>
        [RelayCommand]
        private void SwitchToStructured()
        {
            var parsed = SldParser.Parse(SourceXml);
            if (!parsed.Success)
            {
                StatusMessage = string.Format(L.SldEditorStatusParseFailed, parsed.Error);
                return;
            }

            Rules.Clear();
            SldEditorMapper.ApplyDocument(parsed.Document!, Rules);
            SelectedRule = Rules.FirstOrDefault();
            IsSourceMode = false;

            StatusMessage = parsed.Warnings.Count > 0
                ? string.Format(L.SldEditorStatusSwitchedWarn, string.Join("; ", parsed.Warnings))
                : L.SldEditorStatusSwitchedToStructured;
        }

        /// <summary>
        /// 应用前校验（按当前模式）
        /// </summary>
        [RelayCommand]
        private void ValidateSld()
        {
            if (IsSourceMode)
            {
                var r = SldValidator.Validate(SourceXml);
                StatusMessage = r.IsValid
                    ? L.SldEditorStatusValid
                    : string.Format(L.SldEditorStatusInvalid, string.Join("; ", r.Errors));
                return;
            }

            var doc = SldEditorMapper.ToDocument(EffectiveLayerName, Rules);
            var rv = SldValidator.ValidateDocument(doc);
            StatusMessage = rv.IsValid
                ? L.SldEditorStatusValid
                : string.Format(L.SldEditorStatusInvalid, string.Join("; ", rv.Errors));
        }

        /// <summary>
        /// 保存样式（校验 → 创建或更新 → 刷新列表）
        /// </summary>
        [RelayCommand]
        private async Task SaveStyleAsync()
        {
            if (string.IsNullOrWhiteSpace(StyleName))
            {
                StatusMessage = L.SldEditorStatusNeedName;
                return;
            }

            // 按模式确定保存内容：源码模式保留原文；结构化模式用样式名重建 LayerName
            string sld;
            if (IsSourceMode)
            {
                var r = SldValidator.Validate(SourceXml);
                if (!r.IsValid)
                {
                    StatusMessage = string.Format(L.SldEditorStatusInvalid, string.Join("; ", r.Errors));
                    return;
                }
                sld = SourceXml;
            }
            else
            {
                var doc = SldEditorMapper.ToDocument(StyleName, Rules);
                var rv = SldValidator.ValidateDocument(doc);
                if (!rv.IsValid)
                {
                    StatusMessage = string.Format(L.SldEditorStatusInvalid, string.Join("; ", rv.Errors));
                    return;
                }
                sld = SldBuilder.Build(doc);
            }

            IsLoading = true;
            try
            {
                var styleService = _connectionService.GetStyleService();
                var existing = await styleService.GetStylesAsync();
                var exists = existing.Any(s => s.Name == StyleName);

                if (exists)
                    await styleService.UpdateStyleAsync(StyleName, sld);
                else
                    await styleService.CreateStyleAsync(StyleName, sld);

                await RefreshStylesAsync();
                StatusMessage = string.Format(L.SldEditorStatusSaved, StyleName);
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(L.SldEditorStatusSaveFailed, ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 加载预览可选图层（qualified 名）
        /// </summary>
        [RelayCommand]
        private async Task LoadPreviewLayersAsync()
        {
            try
            {
                var layers = await _connectionService.GetLayerService().GetLayersAsync();
                PreviewLayers.Clear();
                foreach (var l in layers.Where(l => !string.IsNullOrEmpty(l.Name)))
                {
                    PreviewLayers.Add(l.Name);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(L.SldEditorStatusLoadFailed, ex.Message);
            }
        }

        /// <summary>
        /// 生成预览地址（WMS GetMap；需样式已保存）
        /// </summary>
        [RelayCommand]
        private void PreviewStyle()
        {
            if (string.IsNullOrWhiteSpace(StyleName))
            {
                StatusMessage = L.SldEditorStatusNeedName;
                return;
            }
            if (!Styles.Contains(StyleName))
            {
                StatusMessage = L.SldEditorStatusNeedSaveForPreview;
                return;
            }
            if (string.IsNullOrWhiteSpace(SelectedPreviewLayer))
            {
                StatusMessage = L.SldEditorStatusNeedLayer;
                return;
            }

            var baseUrl = _connectionService.CurrentOptions?.BaseUrl;
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                StatusMessage = L.StatusPleaseConnect;
                return;
            }

            var wmsUrl = $"{baseUrl.TrimEnd('/')}/wms";
            PreviewUrl = $"{wmsUrl}?service=WMS&version=1.1.0&request=GetMap"
                + $"&layers={Uri.EscapeDataString(SelectedPreviewLayer)}"
                + $"&styles={Uri.EscapeDataString(StyleName)}"
                + "&srs=EPSG:4326&bbox=-180,-90,180,90&width=800&height=600&format=image/png";
            StatusMessage = string.Format(L.SldEditorStatusPreviewReady, SelectedPreviewLayer);
        }

        /// <summary>结构化校验用 LayerName：优先样式名，未填时用占位（聚焦规则内容校验）。</summary>
        private string EffectiveLayerName => string.IsNullOrWhiteSpace(StyleName) ? "style" : StyleName;

        /// <summary>刷新样式名列表（不改变状态消息）。</summary>
        private async Task RefreshStylesAsync()
        {
            var styles = await _connectionService.GetStyleService().GetStylesAsync();
            Styles.Clear();
            foreach (var s in styles)
            {
                if (!string.IsNullOrEmpty(s.Name))
                    Styles.Add(s.Name);
            }
        }
    }
}
