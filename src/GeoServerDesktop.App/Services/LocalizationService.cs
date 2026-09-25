using System.Globalization;
using System.Resources;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GeoServerDesktop.App.Services;

/// <summary>
/// 应用程序本地化服务，支持中英文切换。
///
/// M5 架构收敛：634 条文案从内联 <c>T(en, zh)</c> 硬编码迁移到 resx 资源表
/// （<c>Resources/Strings.resx</c> 英文中性 + <c>Resources/Strings.zh.resx</c> 中文卫星），
/// 属性一律 <c>T(nameof(属性名))</c> 键查找；新增文案只需加资源条目 + 一个属性，
/// 不再在源码里塞双语字面量。语言切换是进程内显式开关（不依赖 <c>CurrentUICulture</c>），
/// 因此查表时显式传文化：<c>zh</c> 走卫星、英文走主程序集中性资源。
/// <see cref="T(string,string)"/> 作为兼容层保留（历史调用形态），新代码请用键查找。
/// </summary>
public class LocalizationService : ObservableObject
{
    private static readonly LocalizationService Singleton = new();

    /// <summary>资源表（基名 GeoServerDesktop.App.Resources.Strings）。</summary>
    private static readonly ResourceManager Strings = new ResourceManager(
        "GeoServerDesktop.App.Resources.Strings", typeof(LocalizationService).Assembly);

    private static readonly CultureInfo ChineseCulture = new CultureInfo("zh");

    /// <summary>
    /// 获取单例实例
    /// </summary>
    public static LocalizationService Instance => Singleton;

    private bool _isChinese;

    /// <summary>当前是否为中文语言态（测试与诊断用）。</summary>
    public bool IsChinese => _isChinese;

    /// <summary>
    /// 切换语言
    /// </summary>
    public void ToggleLanguage()
    {
        _isChinese = !_isChinese;
        OnPropertyChanged(string.Empty);
    }

    /// <summary>按资源键取当前语言文案（缺中文条目时回落中性英文；键完全缺失时回显键名便于发现漏项）。</summary>
    /// <param name="key">资源键（约定与属性名一致）。</param>
    /// <returns>文案。</returns>
    private string T(string key)
    {
        var text = Strings.GetString(key, _isChinese ? ChineseCulture : CultureInfo.InvariantCulture);
        if (!string.IsNullOrEmpty(text)) return text;
        var fallback = Strings.GetString(key, CultureInfo.InvariantCulture);
        return string.IsNullOrEmpty(fallback) ? key : fallback;
    }

    /// <summary>兼容层：历史内联双语文本形态（新增文案请改用 resx + <see cref="T(string)"/>）。</summary>
    private string T(string en, string zh) => _isChinese ? zh : en;

    // ── MainWindow ─────────────────────────────────────────────────────────

    /// <summary>语言切换按钮文本</summary>
    public string LanguageToggleText => T(nameof(LanguageToggleText));

    /// <summary>语言切换提示</summary>
    public string LanguageToggleTooltip => T(nameof(LanguageToggleTooltip));

    /// <summary>应用程序副标题</summary>
    public string AppHeaderSubtitle => T(nameof(AppHeaderSubtitle));

    /// <summary>URL 标签</summary>
    public string LabelUrl => T(nameof(LabelUrl));

    /// <summary>用户名标签</summary>
    public string LabelUser => T(nameof(LabelUser));

    /// <summary>密码标签</summary>
    public string LabelPassword => T(nameof(LabelPassword));

    /// <summary>登录按钮</summary>
    public string Login => T(nameof(Login));

    /// <summary>已连接状态</summary>
    public string Connected => T(nameof(Connected));

    /// <summary>注销按钮</summary>
    public string Logout => T(nameof(Logout));

    // ── 左侧导航 ────────────────────────────────────────────────────────────

    /// <summary>关于与状态</summary>
    public string NavAboutAndStatus => T(nameof(NavAboutAndStatus));

    /// <summary>关于 GeoServer</summary>
    public string NavAboutGeoServer => T(nameof(NavAboutGeoServer));

    /// <summary>数据</summary>
    public string NavData => T(nameof(NavData));

    /// <summary>图层预览</summary>
    public string NavLayerPreview => T(nameof(NavLayerPreview));

    /// <summary>工作空间</summary>
    public string NavWorkspaces => T(nameof(NavWorkspaces));

    /// <summary>数据存储</summary>
    public string NavStores => T(nameof(NavStores));

    /// <summary>图层</summary>
    public string NavLayers => T(nameof(NavLayers));

    /// <summary>图层组</summary>
    public string NavLayerGroups => T(nameof(NavLayerGroups));

    /// <summary>样式</summary>
    public string NavStyles => T(nameof(NavStyles));

    /// <summary>数据导入向导</summary>
    public string NavImportWizard => T(nameof(NavImportWizard));

    /// <summary>服务</summary>
    public string NavServices => T(nameof(NavServices));

    /// <summary>设置</summary>
    public string NavSettings => T(nameof(NavSettings));

    /// <summary>全局</summary>
    public string NavGlobal => T(nameof(NavGlobal));

    /// <summary>日志</summary>
    public string NavLogging => T(nameof(NavLogging));

    /// <summary>切片缓存</summary>
    public string NavTileCaching => T(nameof(NavTileCaching));

    /// <summary>缓存默认值</summary>
    public string NavCachingDefaults => T(nameof(NavCachingDefaults));

    /// <summary>格网集</summary>
    public string NavGridsets => T(nameof(NavGridsets));

    /// <summary>磁盘配额</summary>
    public string NavDiskQuota => T(nameof(NavDiskQuota));

    /// <summary>安全</summary>
    public string NavSecurity => T(nameof(NavSecurity));

    /// <summary>安全设置（导航）</summary>
    public string NavSecuritySettings => T(nameof(NavSecuritySettings));

    /// <summary>用户组角色（导航）</summary>
    public string NavUsersGroupsRoles => T(nameof(NavUsersGroupsRoles));

    // ── ResourceTree（资源树浏览）────────────────────────────────────────────

    /// <summary>资源树标题</summary>
    public string ResourceTreeTitle => T(nameof(ResourceTreeTitle));

    /// <summary>资源树：工作空间容器</summary>
    public string TreeWorkspaces => T(nameof(TreeWorkspaces));

    /// <summary>资源树：数据存储容器</summary>
    public string TreeDataStores => T(nameof(TreeDataStores));

    /// <summary>资源树：图层容器</summary>
    public string TreeLayers => T(nameof(TreeLayers));

    /// <summary>资源树：样式容器</summary>
    public string TreeStyles => T(nameof(TreeStyles));

    /// <summary>资源树：图层组容器</summary>
    public string TreeLayerGroups => T(nameof(TreeLayerGroups));

    // ── Dashboard（欢迎页仪表盘）─────────────────────────────────────────────

    /// <summary>仪表盘标题</summary>
    public string DashboardTitle => T(nameof(DashboardTitle));

    /// <summary>仪表盘欢迎语（未连接时显示）</summary>
    public string DashboardWelcomeMessage => T(nameof(DashboardWelcomeMessage));

    /// <summary>仪表盘：连接状态卡片标题</summary>
    public string DashboardConnectionStatus => T(nameof(DashboardConnectionStatus));

    /// <summary>仪表盘：已连接</summary>
    public string DashboardConnected => T(nameof(DashboardConnected));

    /// <summary>仪表盘：未连接</summary>
    public string DashboardNotConnected => T(nameof(DashboardNotConnected));

    /// <summary>仪表盘：GeoServer 版本卡片标题</summary>
    public string DashboardGeoServerVersion => T(nameof(DashboardGeoServerVersion));

    /// <summary>仪表盘：工作空间计数卡片标题</summary>
    public string DashboardWorkspaces => T(nameof(DashboardWorkspaces));

    /// <summary>仪表盘：图层计数卡片标题</summary>
    public string DashboardLayers => T(nameof(DashboardLayers));

    /// <summary>仪表盘：刷新按钮提示</summary>
    public string DashboardRefreshToolTip => T(nameof(DashboardRefreshToolTip));

    // ── 通用 ────────────────────────────────────────────────────────────────

    /// <summary>刷新</summary>
    public string Refresh => T(nameof(Refresh));

    /// <summary>创建</summary>
    public string Create => T(nameof(Create));

    /// <summary>删除</summary>
    public string Delete => T(nameof(Delete));

    /// <summary>取消</summary>
    public string Cancel => T(nameof(Cancel));

    /// <summary>保存设置</summary>
    public string SaveSettings => T(nameof(SaveSettings));

    /// <summary>加载</summary>
    public string Load => T(nameof(Load));

    /// <summary>操作</summary>
    public string Actions => T(nameof(Actions));

    /// <summary>处理中</summary>
    public string Processing => T(nameof(Processing));

    /// <summary>加载中</summary>
    public string Loading => T(nameof(Loading));

    /// <summary>名称</summary>
    public string NameLabel => T(nameof(NameLabel));

    /// <summary>描述</summary>
    public string DescriptionLabel => T(nameof(DescriptionLabel));

    /// <summary>标题标签</summary>
    public string TitleLabel => T(nameof(TitleLabel));

    /// <summary>删除选中</summary>
    public string DeleteSelected => T(nameof(DeleteSelected));

    /// <summary>已启用</summary>
    public string EnabledLabel => T(nameof(EnabledLabel));

    /// <summary>工作空间标签</summary>
    public string WorkspaceLabel => T(nameof(WorkspaceLabel));

    /// <summary>服务配置</summary>
    public string ServiceConfiguration => T(nameof(ServiceConfiguration));

    /// <summary>服务信息</summary>
    public string ServiceInformation => T(nameof(ServiceInformation));

    /// <summary>名称输入框</summary>
    public string ServiceNameHint => T(nameof(ServiceNameHint));

    /// <summary>标题输入框</summary>
    public string ServiceTitleHint => T(nameof(ServiceTitleHint));

    /// <summary>摘要输入框</summary>
    public string ServiceAbstractHint => T(nameof(ServiceAbstractHint));

    /// <summary>维护者输入框</summary>
    public string MaintainerHint => T(nameof(MaintainerHint));

    /// <summary>在线资源 URL 输入框</summary>
    public string OnlineResourceUrlHint => T(nameof(OnlineResourceUrlHint));

    /// <summary>在线资源标签</summary>
    public string OnlineResourceLabel => T(nameof(OnlineResourceLabel));

    /// <summary>维护者标签</summary>
    public string MaintainerLabel => T(nameof(MaintainerLabel));

    /// <summary>摘要标签</summary>
    public string AbstractLabel => T(nameof(AbstractLabel));

    /// <summary>CITE 合规</summary>
    public string CiteCompliant => T(nameof(CiteCompliant));

    // ── WorkspaceManagement ────────────────────────────────────────────────

    /// <summary>工作空间管理标题</summary>
    public string WorkspaceManagementTitle => T(nameof(WorkspaceManagementTitle));

    /// <summary>重新加载工作空间提示</summary>
    public string ReloadWorkspacesToolTip => T(nameof(ReloadWorkspacesToolTip));

    /// <summary>已有工作空间</summary>
    public string ExistingWorkspaces => T(nameof(ExistingWorkspaces));

    /// <summary>新建工作空间</summary>
    public string CreateNewWorkspace => T(nameof(CreateNewWorkspace));

    /// <summary>输入工作空间名称水印</summary>
    public string EnterWorkspaceNameHint => T(nameof(EnterWorkspaceNameHint));

    /// <summary>删除工作空间</summary>
    public string DeleteWorkspace => T(nameof(DeleteWorkspace));

    /// <summary>删除工作空间警告</summary>
    public string DeleteWorkspaceWarning => T(nameof(DeleteWorkspaceWarning));

    // ── StoresManagement ───────────────────────────────────────────────────

    /// <summary>数据存储标题</summary>
    public string DataStoresTitle => T(nameof(DataStoresTitle));

    /// <summary>数据存储副标题</summary>
    public string DataStoresSubtitle => T(nameof(DataStoresSubtitle));

    /// <summary>添加存储按钮</summary>
    public string AddStore => T(nameof(AddStore));

    /// <summary>选择工作空间提示（存储）</summary>
    public string SelectWorkspaceToViewStoresHint => T(nameof(SelectWorkspaceToViewStoresHint));

    /// <summary>选择工作空间提示（列表）</summary>
    public string SelectWorkspaceToViewStores => T(nameof(SelectWorkspaceToViewStores));

    /// <summary>添加数据存储对话框标题</summary>
    public string AddDataStoreTitle => T(nameof(AddDataStoreTitle));

    /// <summary>数据存储名称水印</summary>
    public string DataStoreNameHint => T(nameof(DataStoreNameHint));

    /// <summary>可选描述水印</summary>
    public string OptionalDescriptionHint => T(nameof(OptionalDescriptionHint));

    // ── LayersManagement ───────────────────────────────────────────────────

    /// <summary>图层标题</summary>
    public string LayersTitle => T(nameof(LayersTitle));

    /// <summary>图层副标题</summary>
    public string LayersSubtitle => T(nameof(LayersSubtitle));

    /// <summary>发布图层按钮</summary>
    public string PublishLayer => T(nameof(PublishLayer));

    /// <summary>选择工作空间过滤图层</summary>
    public string SelectWorkspaceFilterLayersHint => T(nameof(SelectWorkspaceFilterLayersHint));

    /// <summary>选择工作空间查看图层</summary>
    public string SelectWorkspaceToViewLayers => T(nameof(SelectWorkspaceToViewLayers));

    /// <summary>发布新图层对话框标题</summary>
    public string PublishNewLayerTitle => T(nameof(PublishNewLayerTitle));

    /// <summary>数据存储标签</summary>
    public string DataStoreLabel => T(nameof(DataStoreLabel));

    /// <summary>图层名称标签</summary>
    public string LayerNameLabel => T(nameof(LayerNameLabel));

    /// <summary>原生名称标签</summary>
    public string NativeNameLabel => T(nameof(NativeNameLabel));

    /// <summary>SRS 标签</summary>
    public string SrsLabel => T(nameof(SrsLabel));

    /// <summary>已发布图层名称水印</summary>
    public string PublishedLayerNameHint => T(nameof(PublishedLayerNameHint));

    /// <summary>原生名称水印</summary>
    public string NativeNameHint => T(nameof(NativeNameHint));

    /// <summary>发布按钮</summary>
    public string PublishButton => T(nameof(PublishButton));

    /// <summary>编辑按钮</summary>
    public string EditButton => T(nameof(EditButton));

    /// <summary>默认样式标签</summary>
    public string DefaultStyleLabel => T(nameof(DefaultStyleLabel));

    /// <summary>资源标签</summary>
    public string ResourceLabel => T(nameof(ResourceLabel));

    // ── LayerGroupsManagement ──────────────────────────────────────────────

    /// <summary>图层组标题</summary>
    public string LayerGroupsTitle => T(nameof(LayerGroupsTitle));

    /// <summary>图层组副标题</summary>
    public string LayerGroupsSubtitle => T(nameof(LayerGroupsSubtitle));

    /// <summary>创建按钮（图层组）</summary>
    public string CreateLayerGroupButton => T(nameof(CreateLayerGroupButton));

    /// <summary>选择工作空间过滤图层组</summary>
    public string SelectWorkspaceFilterLayerGroupsHint => T(nameof(SelectWorkspaceFilterLayerGroupsHint));

    /// <summary>选择工作空间查看图层组</summary>
    public string SelectWorkspaceToViewLayerGroups => T(nameof(SelectWorkspaceToViewLayerGroups));

    /// <summary>创建图层组对话框标题</summary>
    public string CreateLayerGroupTitle => T(nameof(CreateLayerGroupTitle));

    /// <summary>名称必填标签</summary>
    public string NameRequiredLabel => T(nameof(NameRequiredLabel));

    /// <summary>图层组名称水印</summary>
    public string EnterLayerGroupNameHint => T(nameof(EnterLayerGroupNameHint));

    /// <summary>图层组标题水印</summary>
    public string EnterLayerGroupTitleHint => T(nameof(EnterLayerGroupTitleHint));

    /// <summary>模式标签</summary>
    public string ModeLabel => T(nameof(ModeLabel));

    /// <summary>图层数量标签</summary>
    public string LayersCountLabel => T(nameof(LayersCountLabel));

    /// <summary>工作区标签</summary>
    public string WorkspaceItemLabel => T(nameof(WorkspaceItemLabel));

    // ── StyleManagement ────────────────────────────────────────────────────

    /// <summary>样式管理标题</summary>
    public string StyleManagementTitle => T(nameof(StyleManagementTitle));

    /// <summary>重新加载样式提示</summary>
    public string ReloadStylesToolTip => T(nameof(ReloadStylesToolTip));

    /// <summary>已有样式</summary>
    public string ExistingStyles => T(nameof(ExistingStyles));

    /// <summary>样式名称水印</summary>
    public string StyleNameHint => T(nameof(StyleNameHint));

    /// <summary>加载选中</summary>
    public string LoadSelected => T(nameof(LoadSelected));

    /// <summary>示例 SLD</summary>
    public string SampleSld => T(nameof(SampleSld));

    /// <summary>上传/更新</summary>
    public string UploadUpdate => T(nameof(UploadUpdate));

    /// <summary>SLD 内容</summary>
    public string SldContent => T(nameof(SldContent));

    /// <summary>SLD 内容水印</summary>
    public string SldContentHint => T(nameof(SldContentHint));

    // ── MapPreview ─────────────────────────────────────────────────────────

    /// <summary>地图预览标题</summary>
    public string MapPreviewTitle => T(nameof(MapPreviewTitle));

    /// <summary>重置视图提示</summary>
    public string ResetViewToolTip => T(nameof(ResetViewToolTip));

    /// <summary>清除图层提示</summary>
    public string ClearLayerToolTip => T(nameof(ClearLayerToolTip));

    /// <summary>重置按钮</summary>
    public string ResetButton => T(nameof(ResetButton));

    /// <summary>清除按钮</summary>
    public string ClearButton => T(nameof(ClearButton));

    /// <summary>地图预览描述</summary>
    public string MapPreviewDescription => T(nameof(MapPreviewDescription));

    /// <summary>当前图层 WMS URL</summary>
    public string CurrentLayerWmsUrl => T(nameof(CurrentLayerWmsUrl));

    /// <summary>WMS URL 使用提示</summary>
    public string WmsUrlUsageHint => T(nameof(WmsUrlUsageHint));

    /// <summary>预览图层方法标题</summary>
    public string HowToPreviewTitle => T(nameof(HowToPreviewTitle));

    /// <summary>预览步骤 1</summary>
    public string PreviewStep1 => T(nameof(PreviewStep1));

    /// <summary>预览步骤 2</summary>
    public string PreviewStep2 => T(nameof(PreviewStep2));

    /// <summary>预览步骤 3</summary>
    public string PreviewStep3 => T(nameof(PreviewStep3));

    /// <summary>预览步骤 4</summary>
    public string PreviewStep4 => T(nameof(PreviewStep4));

    /// <summary>地图预览底栏信息</summary>
    public string MapPreviewInfo => T(nameof(MapPreviewInfo));

    // ── About ──────────────────────────────────────────────────────────────

    /// <summary>关于 GeoServer 标题</summary>
    public string AboutGeoServerTitle => T(nameof(AboutGeoServerTitle));

    /// <summary>关于副标题</summary>
    public string AboutSubtitle => T(nameof(AboutSubtitle));

    /// <summary>版本信息</summary>
    public string VersionInformation => T(nameof(VersionInformation));

    /// <summary>GeoServer 版本标签</summary>
    public string GeoServerVersionLabel => T(nameof(GeoServerVersionLabel));

    /// <summary>GeoTools 版本标签</summary>
    public string GeoToolsVersionLabel => T(nameof(GeoToolsVersionLabel));

    /// <summary>GeoWebCache 版本标签</summary>
    public string GeoWebCacheVersionLabel => T(nameof(GeoWebCacheVersionLabel));

    // ── GlobalSettings ─────────────────────────────────────────────────────

    /// <summary>全局设置标题</summary>
    public string GlobalSettingsTitle => T(nameof(GlobalSettingsTitle));

    /// <summary>加载全局设置提示</summary>
    public string LoadGlobalSettingsToolTip => T(nameof(LoadGlobalSettingsToolTip));

    /// <summary>联系信息</summary>
    public string ContactInformation => T(nameof(ContactInformation));

    /// <summary>联系人标签</summary>
    public string ContactPersonLabel => T(nameof(ContactPersonLabel));

    /// <summary>组织标签</summary>
    public string OrganizationLabel => T(nameof(OrganizationLabel));

    /// <summary>电子邮件标签</summary>
    public string EmailLabel => T(nameof(EmailLabel));

    /// <summary>联系人水印</summary>
    public string ContactPersonHint => T(nameof(ContactPersonHint));

    /// <summary>组织名称水印</summary>
    public string OrganizationNameHint => T(nameof(OrganizationNameHint));

    /// <summary>电子邮件地址水印</summary>
    public string EmailAddressHint => T(nameof(EmailAddressHint));

    /// <summary>服务器配置</summary>
    public string ServerConfiguration => T(nameof(ServerConfiguration));

    /// <summary>代理基础 URL 标签</summary>
    public string ProxyBaseUrlLabel => T(nameof(ProxyBaseUrlLabel));

    /// <summary>代理基础 URL 水印</summary>
    public string ProxyBaseUrlHint => T(nameof(ProxyBaseUrlHint));

    /// <summary>详细异常</summary>
    public string VerboseExceptions => T(nameof(VerboseExceptions));

    // ── WMS Settings ───────────────────────────────────────────────────────

    /// <summary>WMS 设置标题</summary>
    public string WmsSettingsTitle => T(nameof(WmsSettingsTitle));

    /// <summary>加载 WMS 设置提示</summary>
    public string LoadWmsSettingsToolTip => T(nameof(LoadWmsSettingsToolTip));

    /// <summary>启用 WMS 服务</summary>
    public string EnableWmsService => T(nameof(EnableWmsService));

    // ── WFS Settings ───────────────────────────────────────────────────────

    /// <summary>WFS 设置标题</summary>
    public string WfsSettingsTitle => T(nameof(WfsSettingsTitle));

    /// <summary>加载 WFS 设置提示</summary>
    public string LoadWfsSettingsToolTip => T(nameof(LoadWfsSettingsToolTip));

    /// <summary>启用 WFS 服务</summary>
    public string EnableWfsService => T(nameof(EnableWfsService));

    /// <summary>WFS 参数</summary>
    public string WfsParameters => T(nameof(WfsParameters));

    /// <summary>最大要素数标签</summary>
    public string MaxFeaturesLabel => T(nameof(MaxFeaturesLabel));

    /// <summary>服务级别标签</summary>
    public string ServiceLevelLabel => T(nameof(ServiceLevelLabel));

    /// <summary>要素边界</summary>
    public string FeatureBounding => T(nameof(FeatureBounding));

    // ── WCS Settings ───────────────────────────────────────────────────────

    /// <summary>WCS 设置标题</summary>
    public string WcsSettingsTitle => T(nameof(WcsSettingsTitle));

    /// <summary>加载 WCS 设置提示</summary>
    public string LoadWcsSettingsToolTip => T(nameof(LoadWcsSettingsToolTip));

    /// <summary>启用 WCS 服务</summary>
    public string EnableWcsService => T(nameof(EnableWcsService));

    /// <summary>WCS 参数</summary>
    public string WcsParameters => T(nameof(WcsParameters));

    /// <summary>最大输入内存标签</summary>
    public string MaxInputMemoryLabel => T(nameof(MaxInputMemoryLabel));

    /// <summary>最大输出内存标签</summary>
    public string MaxOutputMemoryLabel => T(nameof(MaxOutputMemoryLabel));

    /// <summary>子采样启用</summary>
    public string SubsamplingEnabled => T(nameof(SubsamplingEnabled));

    // ── Logging ────────────────────────────────────────────────────────────

    /// <summary>日志设置标题</summary>
    public string LoggingSettingsTitle => T(nameof(LoggingSettingsTitle));

    /// <summary>加载日志设置提示</summary>
    public string LoadLoggingSettingsToolTip => T(nameof(LoadLoggingSettingsToolTip));

    /// <summary>日志配置</summary>
    public string LoggingConfiguration => T(nameof(LoggingConfiguration));

    /// <summary>日志级别标签</summary>
    public string LogLevelLabel => T(nameof(LogLevelLabel));

    /// <summary>日志位置标签</summary>
    public string LogLocationLabel => T(nameof(LogLocationLabel));

    /// <summary>日志文件路径水印</summary>
    public string LogFilePathHint => T(nameof(LogFilePathHint));

    /// <summary>标准输出日志</summary>
    public string StdOutLogging => T(nameof(StdOutLogging));

    /// <summary>启用文件日志</summary>
    public string EnableFileLogging => T(nameof(EnableFileLogging));

    /// <summary>日志文件查看器</summary>
    public string LogFileViewer => T(nameof(LogFileViewer));

    /// <summary>刷新日志按钮</summary>
    public string RefreshLogFile => T(nameof(RefreshLogFile));

    /// <summary>日志文件位置标签（查看器）</summary>
    public string LogFileLocationLabel => T(nameof(LogFileLocationLabel));

    // ── CachingDefaults ────────────────────────────────────────────────────

    /// <summary>切片缓存默认值标题</summary>
    public string TileCachingDefaultsTitle => T(nameof(TileCachingDefaultsTitle));

    /// <summary>加载缓存默认值提示</summary>
    public string LoadCachingDefaultsToolTip => T(nameof(LoadCachingDefaultsToolTip));

    /// <summary>默认缓存配置</summary>
    public string DefaultCacheConfiguration => T(nameof(DefaultCacheConfiguration));

    /// <summary>默认启用切片缓存</summary>
    public string EnableTileCachingByDefault => T(nameof(EnableTileCachingByDefault));

    /// <summary>默认过期设置</summary>
    public string DefaultExpirySettings => T(nameof(DefaultExpirySettings));

    /// <summary>缓存过期时间标签</summary>
    public string CacheExpiryLabel => T(nameof(CacheExpiryLabel));

    /// <summary>客户端缓存过期时间标签</summary>
    public string ClientCacheExpiryLabel => T(nameof(ClientCacheExpiryLabel));

    /// <summary>无过期说明</summary>
    public string NoExpiryNote => T(nameof(NoExpiryNote));

    /// <summary>缓存注意事项</summary>
    public string CachingNote => T(nameof(CachingNote));

    // ── DiskQuota ──────────────────────────────────────────────────────────

    /// <summary>磁盘配额标题</summary>
    public string DiskQuotaTitle => T(nameof(DiskQuotaTitle));

    /// <summary>加载磁盘配额设置提示</summary>
    public string LoadDiskQuotaToolTip => T(nameof(LoadDiskQuotaToolTip));

    /// <summary>配额配置</summary>
    public string QuotaConfiguration => T(nameof(QuotaConfiguration));

    /// <summary>启用磁盘配额</summary>
    public string EnableDiskQuota => T(nameof(EnableDiskQuota));

    /// <summary>全局配额</summary>
    public string GlobalQuota => T(nameof(GlobalQuota));

    /// <summary>配额值标签</summary>
    public string QuotaValueLabel => T(nameof(QuotaValueLabel));

    /// <summary>磁盘块大小标签</summary>
    public string DiskBlockSizeLabel => T(nameof(DiskBlockSizeLabel));

    /// <summary>清理频率标签</summary>
    public string CleanupFrequencyLabel => T(nameof(CleanupFrequencyLabel));

    // ── Gridsets ───────────────────────────────────────────────────────────

    /// <summary>格网集标题</summary>
    public string GridsetsTitle => T(nameof(GridsetsTitle));

    /// <summary>重新加载格网集列表提示</summary>
    public string ReloadGridsetsToolTip => T(nameof(ReloadGridsetsToolTip));

    /// <summary>已有格网集</summary>
    public string ExistingGridsets => T(nameof(ExistingGridsets));

    /// <summary>选中的格网集</summary>
    public string SelectedGridset => T(nameof(SelectedGridset));

    /// <summary>删除格网集</summary>
    public string DeleteGridset => T(nameof(DeleteGridset));

    /// <summary>删除格网集警告</summary>
    public string DeleteGridsetWarning => T(nameof(DeleteGridsetWarning));

    // ── SecuritySettings ───────────────────────────────────────────────────

    /// <summary>安全设置标题</summary>
    public string SecuritySettingsTitle => T(nameof(SecuritySettingsTitle));

    /// <summary>加载安全设置提示</summary>
    public string LoadSecuritySettingsToolTip => T(nameof(LoadSecuritySettingsToolTip));

    /// <summary>访问控制规则</summary>
    public string AccessControlRules => T(nameof(AccessControlRules));

    /// <summary>信息</summary>
    public string InformationLabel => T(nameof(InformationLabel));

    /// <summary>安全信息说明</summary>
    public string SecurityInfoText => T(nameof(SecurityInfoText));

    /// <summary>安全格式说明</summary>
    public string SecurityFormatText => T(nameof(SecurityFormatText));

    // ── UsersGroupsRoles ───────────────────────────────────────────────────

    /// <summary>用户组角色标题</summary>
    public string UsersGroupsRolesTitle => T(nameof(UsersGroupsRolesTitle));

    /// <summary>重新加载全部提示</summary>
    public string ReloadAllToolTip => T(nameof(ReloadAllToolTip));

    /// <summary>用户选项卡</summary>
    public string UsersTab => T(nameof(UsersTab));

    /// <summary>组选项卡</summary>
    public string GroupsTab => T(nameof(GroupsTab));

    /// <summary>角色选项卡</summary>
    public string RolesTab => T(nameof(RolesTab));

    /// <summary>用户列表标题</summary>
    public string UsersHeader => T(nameof(UsersHeader));

    /// <summary>组列表标题</summary>
    public string GroupsHeader => T(nameof(GroupsHeader));

    /// <summary>角色列表标题</summary>
    public string RolesHeader => T(nameof(RolesHeader));

    /// <summary>创建用户</summary>
    public string CreateUser => T(nameof(CreateUser));

    /// <summary>用户名水印</summary>
    public string UsernameHint => T(nameof(UsernameHint));

    /// <summary>密码水印</summary>
    public string PasswordHint => T(nameof(PasswordHint));

    /// <summary>新密码水印</summary>
    public string NewPasswordHint => T(nameof(NewPasswordHint));

    /// <summary>创建用户按钮</summary>
    public string CreateUserButton => T(nameof(CreateUserButton));

    /// <summary>删除用户</summary>
    public string DeleteUser => T(nameof(DeleteUser));

    /// <summary>删除选中用户</summary>
    public string DeleteSelectedUser => T(nameof(DeleteSelectedUser));

    /// <summary>编辑用户</summary>
    public string EditUser => T(nameof(EditUser));

    /// <summary>已启用复选框</summary>
    public string EnabledCheckbox => T(nameof(EnabledCheckbox));

    /// <summary>更新用户</summary>
    public string UpdateUser => T(nameof(UpdateUser));

    /// <summary>创建组</summary>
    public string CreateGroup => T(nameof(CreateGroup));

    /// <summary>组名称水印</summary>
    public string GroupNameHint => T(nameof(GroupNameHint));

    /// <summary>创建组按钮</summary>
    public string CreateGroupButton => T(nameof(CreateGroupButton));

    /// <summary>删除组</summary>
    public string DeleteGroup => T(nameof(DeleteGroup));

    /// <summary>删除选中组</summary>
    public string DeleteSelectedGroup => T(nameof(DeleteSelectedGroup));

    /// <summary>角色（只读）</summary>
    public string RolesReadOnly => T(nameof(RolesReadOnly));

    /// <summary>角色只读说明</summary>
    public string RolesReadOnlyInfo => T(nameof(RolesReadOnlyInfo));

    // ── 状态消息模板 ────────────────────────────────────────────────────────

    /// <summary>就绪</summary>
    public string StatusReady => T(nameof(StatusReady));

    /// <summary>未连接</summary>
    public string StatusNotConnected => T(nameof(StatusNotConnected));

    /// <summary>连接中</summary>
    public string StatusConnecting => T(nameof(StatusConnecting));

    /// <summary>连接成功</summary>
    public string StatusConnectedSuccess => T(nameof(StatusConnectedSuccess));

    /// <summary>连接失败模板 {0}=消息</summary>
    public string StatusConnectionFailed => T(nameof(StatusConnectionFailed));

    /// <summary>已断开连接</summary>
    public string StatusDisconnected => T(nameof(StatusDisconnected));

    /// <summary>正在刷新资源</summary>
    public string StatusRefreshing => T(nameof(StatusRefreshing));

    /// <summary>资源已刷新</summary>
    public string StatusRefreshed => T(nameof(StatusRefreshed));

    /// <summary>刷新失败模板 {0}=消息</summary>
    public string StatusRefreshFailed => T(nameof(StatusRefreshFailed));

    /// <summary>正在加载仪表盘</summary>
    public string StatusDashboardLoading => T(nameof(StatusDashboardLoading));

    /// <summary>仪表盘已加载</summary>
    public string StatusDashboardLoaded => T(nameof(StatusDashboardLoaded));

    /// <summary>仪表盘加载失败模板 {0}=消息</summary>
    public string StatusDashboardLoadFailed => T(nameof(StatusDashboardLoadFailed));

    /// <summary>关于 GeoServer 状态</summary>
    public string StatusAbout => T(nameof(StatusAbout));

    /// <summary>图层预览状态</summary>
    public string StatusLayerPreview => T(nameof(StatusLayerPreview));

    /// <summary>工作空间状态</summary>
    public string StatusWorkspaces => T(nameof(StatusWorkspaces));

    /// <summary>数据存储状态</summary>
    public string StatusDataStores => T(nameof(StatusDataStores));

    /// <summary>图层状态</summary>
    public string StatusLayers => T(nameof(StatusLayers));

    /// <summary>图层组状态</summary>
    public string StatusLayerGroups => T(nameof(StatusLayerGroups));

    /// <summary>样式状态</summary>
    public string StatusStyles => T(nameof(StatusStyles));

    /// <summary>WMS 设置状态</summary>
    public string StatusWmsSettings => T(nameof(StatusWmsSettings));

    /// <summary>WFS 设置状态</summary>
    public string StatusWfsSettings => T(nameof(StatusWfsSettings));

    /// <summary>WCS 设置状态</summary>
    public string StatusWcsSettings => T(nameof(StatusWcsSettings));

    /// <summary>全局设置状态</summary>
    public string StatusGlobalSettings => T(nameof(StatusGlobalSettings));

    /// <summary>日志设置状态</summary>
    public string StatusLoggingSettings => T(nameof(StatusLoggingSettings));

    /// <summary>切片缓存默认值状态</summary>
    public string StatusTileCachingDefaults => T(nameof(StatusTileCachingDefaults));

    /// <summary>格网集状态</summary>
    public string StatusGridsets => T(nameof(StatusGridsets));

    /// <summary>磁盘配额状态</summary>
    public string StatusDiskQuota => T(nameof(StatusDiskQuota));

    /// <summary>安全设置状态</summary>
    public string StatusSecuritySettings => T(nameof(StatusSecuritySettings));

    /// <summary>用户组角色状态</summary>
    public string StatusUsersGroupsAndRoles => T(nameof(StatusUsersGroupsAndRoles));

    /// <summary>请先连接到 GeoServer</summary>
    public string StatusPleaseConnect => T(nameof(StatusPleaseConnect));

    /// <summary>正在加载工作空间</summary>
    public string StatusLoadingWorkspaces => T(nameof(StatusLoadingWorkspaces));

    /// <summary>已加载工作空间模板 {0}=数量</summary>
    public string StatusWorkspacesLoaded => T(nameof(StatusWorkspacesLoaded));

    /// <summary>工作空间名称必填</summary>
    public string StatusWorkspaceNameRequired => T(nameof(StatusWorkspaceNameRequired));

    /// <summary>正在创建工作空间模板 {0}=名称</summary>
    public string StatusCreatingWorkspace => T(nameof(StatusCreatingWorkspace));

    /// <summary>工作空间创建成功模板 {0}=名称</summary>
    public string StatusWorkspaceCreated => T(nameof(StatusWorkspaceCreated));

    /// <summary>工作空间创建失败模板 {0}=消息</summary>
    public string StatusWorkspaceCreateFailed => T(nameof(StatusWorkspaceCreateFailed));

    /// <summary>未选中工作空间</summary>
    public string StatusNoWorkspaceSelected => T(nameof(StatusNoWorkspaceSelected));

    /// <summary>正在删除工作空间模板 {0}=名称</summary>
    public string StatusDeletingWorkspace => T(nameof(StatusDeletingWorkspace));

    /// <summary>工作空间删除成功模板 {0}=名称</summary>
    public string StatusWorkspaceDeleted => T(nameof(StatusWorkspaceDeleted));

    /// <summary>工作空间删除失败模板 {0}=消息</summary>
    public string StatusWorkspaceDeleteFailed => T(nameof(StatusWorkspaceDeleteFailed));

    /// <summary>加载工作空间失败模板 {0}=消息</summary>
    public string StatusWorkspacesLoadFailed => T(nameof(StatusWorkspacesLoadFailed));

    /// <summary>正在加载数据存储</summary>
    public string StatusLoadingDataStores => T(nameof(StatusLoadingDataStores));

    /// <summary>已加载数据存储模板 {0}=数量, {1}=工作空间名</summary>
    public string StatusDataStoresLoaded => T(nameof(StatusDataStoresLoaded));

    /// <summary>未选中数据存储</summary>
    public string StatusNoDataStoreSelected => T(nameof(StatusNoDataStoreSelected));

    /// <summary>正在删除数据存储模板 {0}=名称</summary>
    public string StatusDeletingDataStore => T(nameof(StatusDeletingDataStore));

    /// <summary>数据存储删除成功模板 {0}=名称</summary>
    public string StatusDataStoreDeleted => T(nameof(StatusDataStoreDeleted));

    /// <summary>数据存储删除失败模板 {0}=消息</summary>
    public string StatusDataStoreDeleteFailed => T(nameof(StatusDataStoreDeleteFailed));

    /// <summary>数据存储名称必填</summary>
    public string StatusDataStoreNameRequired => T(nameof(StatusDataStoreNameRequired));

    /// <summary>请先选择工作空间</summary>
    public string StatusPleaseSelectWorkspace => T(nameof(StatusPleaseSelectWorkspace));

    /// <summary>正在创建数据存储模板 {0}=名称</summary>
    public string StatusCreatingDataStore => T(nameof(StatusCreatingDataStore));

    /// <summary>数据存储创建成功</summary>
    public string StatusDataStoreCreated => T(nameof(StatusDataStoreCreated));

    /// <summary>数据存储创建失败模板 {0}=消息</summary>
    public string StatusDataStoreCreateFailed => T(nameof(StatusDataStoreCreateFailed));

    /// <summary>加载数据存储失败模板 {0}=消息</summary>
    public string StatusDataStoresLoadFailed => T(nameof(StatusDataStoresLoadFailed));

    /// <summary>正在加载图层</summary>
    public string StatusLoadingLayers => T(nameof(StatusLoadingLayers));

    /// <summary>已加载图层模板 {0}=数量</summary>
    public string StatusLayersLoaded => T(nameof(StatusLayersLoaded));

    /// <summary>未选中图层</summary>
    public string StatusNoLayerSelected => T(nameof(StatusNoLayerSelected));

    /// <summary>正在删除图层模板 {0}=名称</summary>
    public string StatusDeletingLayer => T(nameof(StatusDeletingLayer));

    /// <summary>图层删除成功模板 {0}=名称</summary>
    public string StatusLayerDeleted => T(nameof(StatusLayerDeleted));

    /// <summary>图层删除失败模板 {0}=消息</summary>
    public string StatusLayerDeleteFailed => T(nameof(StatusLayerDeleteFailed));

    /// <summary>图层名称必填</summary>
    public string StatusLayerNameRequired => T(nameof(StatusLayerNameRequired));

    /// <summary>请选择数据存储</summary>
    public string StatusPleaseSelectDataStore => T(nameof(StatusPleaseSelectDataStore));

    /// <summary>请先选择特定工作空间</summary>
    public string StatusPleaseSelectSpecificWorkspace => T(nameof(StatusPleaseSelectSpecificWorkspace));

    /// <summary>正在发布图层模板 {0}=名称</summary>
    public string StatusPublishingLayer => T(nameof(StatusPublishingLayer));

    /// <summary>图层发布成功</summary>
    public string StatusLayerPublished => T(nameof(StatusLayerPublished));

    /// <summary>图层发布失败模板 {0}=消息</summary>
    public string StatusLayerPublishFailed => T(nameof(StatusLayerPublishFailed));

    /// <summary>加载图层失败模板 {0}=消息</summary>
    public string StatusLayersLoadFailed => T(nameof(StatusLayersLoadFailed));

    /// <summary>正在加载图层组</summary>
    public string StatusLoadingLayerGroups => T(nameof(StatusLoadingLayerGroups));

    /// <summary>已加载图层组模板 {0}=数量</summary>
    public string StatusLayerGroupsLoaded => T(nameof(StatusLayerGroupsLoaded));

    /// <summary>图层组名称必填</summary>
    public string StatusLayerGroupNameRequired => T(nameof(StatusLayerGroupNameRequired));

    /// <summary>正在创建图层组模板 {0}=名称</summary>
    public string StatusCreatingLayerGroup => T(nameof(StatusCreatingLayerGroup));

    /// <summary>图层组创建成功模板 {0}=名称</summary>
    public string StatusLayerGroupCreated => T(nameof(StatusLayerGroupCreated));

    /// <summary>图层组创建失败模板 {0}=消息</summary>
    public string StatusLayerGroupCreateFailed => T(nameof(StatusLayerGroupCreateFailed));

    /// <summary>未选中图层组</summary>
    public string StatusNoLayerGroupSelected => T(nameof(StatusNoLayerGroupSelected));

    /// <summary>正在删除图层组模板 {0}=名称</summary>
    public string StatusDeletingLayerGroup => T(nameof(StatusDeletingLayerGroup));

    /// <summary>图层组删除成功模板 {0}=名称</summary>
    public string StatusLayerGroupDeleted => T(nameof(StatusLayerGroupDeleted));

    /// <summary>图层组删除失败模板 {0}=消息</summary>
    public string StatusLayerGroupDeleteFailed => T(nameof(StatusLayerGroupDeleteFailed));

    /// <summary>加载图层组失败模板 {0}=消息</summary>
    public string StatusLayerGroupsLoadFailed => T(nameof(StatusLayerGroupsLoadFailed));

    /// <summary>正在加载系统信息</summary>
    public string StatusLoadingSystemInfo => T(nameof(StatusLoadingSystemInfo));

    /// <summary>系统信息加载成功</summary>
    public string StatusSystemInfoLoaded => T(nameof(StatusSystemInfoLoaded));

    /// <summary>加载系统信息失败模板 {0}=消息</summary>
    public string StatusSystemInfoLoadFailed => T(nameof(StatusSystemInfoLoadFailed));

    /// <summary>正在加载 WMS 设置</summary>
    public string StatusLoadingWmsSettings => T(nameof(StatusLoadingWmsSettings));

    /// <summary>WMS 设置已加载</summary>
    public string StatusWmsSettingsLoaded => T(nameof(StatusWmsSettingsLoaded));

    /// <summary>加载 WMS 设置失败模板 {0}=消息</summary>
    public string StatusWmsSettingsLoadFailed => T(nameof(StatusWmsSettingsLoadFailed));

    /// <summary>正在保存 WMS 设置</summary>
    public string StatusSavingWmsSettings => T(nameof(StatusSavingWmsSettings));

    /// <summary>WMS 设置保存成功</summary>
    public string StatusWmsSettingsSaved => T(nameof(StatusWmsSettingsSaved));

    /// <summary>保存 WMS 设置失败模板 {0}=消息</summary>
    public string StatusWmsSettingsSaveFailed => T(nameof(StatusWmsSettingsSaveFailed));

    /// <summary>正在加载 WFS 设置</summary>
    public string StatusLoadingWfsSettings => T(nameof(StatusLoadingWfsSettings));

    /// <summary>WFS 设置已加载</summary>
    public string StatusWfsSettingsLoaded => T(nameof(StatusWfsSettingsLoaded));

    /// <summary>加载 WFS 设置失败模板 {0}=消息</summary>
    public string StatusWfsSettingsLoadFailed => T(nameof(StatusWfsSettingsLoadFailed));

    /// <summary>正在保存 WFS 设置</summary>
    public string StatusSavingWfsSettings => T(nameof(StatusSavingWfsSettings));

    /// <summary>WFS 设置保存成功</summary>
    public string StatusWfsSettingsSaved => T(nameof(StatusWfsSettingsSaved));

    /// <summary>保存 WFS 设置失败模板 {0}=消息</summary>
    public string StatusWfsSettingsSaveFailed => T(nameof(StatusWfsSettingsSaveFailed));

    /// <summary>正在加载 WCS 设置</summary>
    public string StatusLoadingWcsSettings => T(nameof(StatusLoadingWcsSettings));

    /// <summary>WCS 设置已加载</summary>
    public string StatusWcsSettingsLoaded => T(nameof(StatusWcsSettingsLoaded));

    /// <summary>加载 WCS 设置失败模板 {0}=消息</summary>
    public string StatusWcsSettingsLoadFailed => T(nameof(StatusWcsSettingsLoadFailed));

    /// <summary>正在保存 WCS 设置</summary>
    public string StatusSavingWcsSettings => T(nameof(StatusSavingWcsSettings));

    /// <summary>WCS 设置保存成功</summary>
    public string StatusWcsSettingsSaved => T(nameof(StatusWcsSettingsSaved));

    /// <summary>保存 WCS 设置失败模板 {0}=消息</summary>
    public string StatusWcsSettingsSaveFailed => T(nameof(StatusWcsSettingsSaveFailed));

    /// <summary>正在加载全局设置</summary>
    public string StatusLoadingGlobalSettings => T(nameof(StatusLoadingGlobalSettings));

    /// <summary>全局设置已加载</summary>
    public string StatusGlobalSettingsLoaded => T(nameof(StatusGlobalSettingsLoaded));

    /// <summary>加载全局设置失败模板 {0}=消息</summary>
    public string StatusGlobalSettingsLoadFailed => T(nameof(StatusGlobalSettingsLoadFailed));

    /// <summary>正在保存全局设置</summary>
    public string StatusSavingGlobalSettings => T(nameof(StatusSavingGlobalSettings));

    /// <summary>全局设置保存成功</summary>
    public string StatusGlobalSettingsSaved => T(nameof(StatusGlobalSettingsSaved));

    /// <summary>保存全局设置失败模板 {0}=消息</summary>
    public string StatusGlobalSettingsSaveFailed => T(nameof(StatusGlobalSettingsSaveFailed));

    /// <summary>正在加载日志设置</summary>
    public string StatusLoadingLoggingSettings => T(nameof(StatusLoadingLoggingSettings));

    /// <summary>日志设置已加载</summary>
    public string StatusLoggingSettingsLoaded => T(nameof(StatusLoggingSettingsLoaded));

    /// <summary>加载日志设置失败模板 {0}=消息</summary>
    public string StatusLoggingSettingsLoadFailed => T(nameof(StatusLoggingSettingsLoadFailed));

    /// <summary>正在保存日志设置</summary>
    public string StatusSavingLoggingSettings => T(nameof(StatusSavingLoggingSettings));

    /// <summary>日志设置保存成功</summary>
    public string StatusLoggingSettingsSaved => T(nameof(StatusLoggingSettingsSaved));

    /// <summary>保存日志设置失败模板 {0}=消息</summary>
    public string StatusLoggingSettingsSaveFailed => T(nameof(StatusLoggingSettingsSaveFailed));

    /// <summary>正在加载日志文件</summary>
    public string StatusLoadingLogFile => T(nameof(StatusLoadingLogFile));

    /// <summary>日志文件已加载模板 {0}=行数</summary>
    public string StatusLogFileLoaded => T(nameof(StatusLogFileLoaded));

    /// <summary>加载日志文件失败模板 {0}=消息</summary>
    public string StatusLogFileLoadFailed => T(nameof(StatusLogFileLoadFailed));

    /// <summary>日志位置无法通过 REST 读取模板 {0}=位置</summary>
    public string StatusLogFileUnresolvable => T(nameof(StatusLogFileUnresolvable));

    /// <summary>正在加载缓存默认值</summary>
    public string StatusLoadingCachingDefaults => T(nameof(StatusLoadingCachingDefaults));

    /// <summary>缓存默认值已加载</summary>
    public string StatusCachingDefaultsLoaded => T(nameof(StatusCachingDefaultsLoaded));

    /// <summary>加载缓存默认值失败模板 {0}=消息</summary>
    public string StatusCachingDefaultsLoadFailed => T(nameof(StatusCachingDefaultsLoadFailed));

    /// <summary>正在保存缓存默认值</summary>
    public string StatusSavingCachingDefaults => T(nameof(StatusSavingCachingDefaults));

    /// <summary>缓存默认值保存成功</summary>
    public string StatusCachingDefaultsSaved => T(nameof(StatusCachingDefaultsSaved));

    /// <summary>保存缓存默认值失败模板 {0}=消息</summary>
    public string StatusCachingDefaultsSaveFailed => T(nameof(StatusCachingDefaultsSaveFailed));

    /// <summary>正在加载磁盘配额设置</summary>
    public string StatusLoadingDiskQuota => T(nameof(StatusLoadingDiskQuota));

    /// <summary>磁盘配额设置已加载</summary>
    public string StatusDiskQuotaLoaded => T(nameof(StatusDiskQuotaLoaded));

    /// <summary>加载磁盘配额设置失败模板 {0}=消息</summary>
    public string StatusDiskQuotaLoadFailed => T(nameof(StatusDiskQuotaLoadFailed));

    /// <summary>正在保存磁盘配额设置</summary>
    public string StatusSavingDiskQuota => T(nameof(StatusSavingDiskQuota));

    /// <summary>磁盘配额设置保存成功</summary>
    public string StatusDiskQuotaSaved => T(nameof(StatusDiskQuotaSaved));

    /// <summary>保存磁盘配额设置失败模板 {0}=消息</summary>
    public string StatusDiskQuotaSaveFailed => T(nameof(StatusDiskQuotaSaveFailed));

    /// <summary>正在加载格网集</summary>
    public string StatusLoadingGridsets => T(nameof(StatusLoadingGridsets));

    /// <summary>已加载格网集模板 {0}=数量</summary>
    public string StatusGridsetsLoaded => T(nameof(StatusGridsetsLoaded));

    /// <summary>加载格网集失败模板 {0}=消息</summary>
    public string StatusGridsetsLoadFailed => T(nameof(StatusGridsetsLoadFailed));

    /// <summary>未选中格网集</summary>
    public string StatusNoGridsetSelected => T(nameof(StatusNoGridsetSelected));

    /// <summary>正在删除格网集模板 {0}=名称</summary>
    public string StatusDeletingGridset => T(nameof(StatusDeletingGridset));

    /// <summary>格网集删除成功模板 {0}=名称</summary>
    public string StatusGridsetDeleted => T(nameof(StatusGridsetDeleted));

    /// <summary>格网集删除失败模板 {0}=消息</summary>
    public string StatusGridsetDeleteFailed => T(nameof(StatusGridsetDeleteFailed));

    /// <summary>正在加载安全设置</summary>
    public string StatusLoadingSecuritySettings => T(nameof(StatusLoadingSecuritySettings));

    /// <summary>安全设置已加载</summary>
    public string StatusSecuritySettingsLoaded => T(nameof(StatusSecuritySettingsLoaded));

    /// <summary>加载安全设置失败模板 {0}=消息</summary>
    public string StatusSecuritySettingsLoadFailed => T(nameof(StatusSecuritySettingsLoadFailed));

    /// <summary>正在加载样式</summary>
    public string StatusLoadingStyles => T(nameof(StatusLoadingStyles));

    /// <summary>已加载样式模板 {0}=数量</summary>
    public string StatusStylesLoaded => T(nameof(StatusStylesLoaded));

    /// <summary>加载样式失败模板 {0}=消息</summary>
    public string StatusStylesLoadFailed => T(nameof(StatusStylesLoadFailed));

    /// <summary>未选中样式</summary>
    public string StatusNoStyleSelected => T(nameof(StatusNoStyleSelected));

    /// <summary>正在加载样式内容</summary>
    public string StatusLoadingStyleContent => T(nameof(StatusLoadingStyleContent));

    /// <summary>样式内容已加载</summary>
    public string StatusStyleContentLoaded => T(nameof(StatusStyleContentLoaded));

    /// <summary>加载样式内容失败模板 {0}=消息</summary>
    public string StatusStyleContentLoadFailed => T(nameof(StatusStyleContentLoadFailed));

    /// <summary>正在上传样式</summary>
    public string StatusUploadingStyle => T(nameof(StatusUploadingStyle));

    /// <summary>样式上传成功</summary>
    public string StatusStyleUploaded => T(nameof(StatusStyleUploaded));

    /// <summary>上传样式失败模板 {0}=消息</summary>
    public string StatusStyleUploadFailed => T(nameof(StatusStyleUploadFailed));

    /// <summary>正在删除样式</summary>
    public string StatusDeletingStyle => T(nameof(StatusDeletingStyle));

    /// <summary>样式删除成功</summary>
    public string StatusStyleDeleted => T(nameof(StatusStyleDeleted));

    /// <summary>样式删除失败模板 {0}=消息</summary>
    public string StatusStyleDeleteFailed => T(nameof(StatusStyleDeleteFailed));

    /// <summary>图层预览就绪模板 {0}=工作空间:图层</summary>
    public string StatusPreviewReady => T(nameof(StatusPreviewReady));

    /// <summary>图层预览失败模板 {0}=消息</summary>
    public string StatusPreviewFailed => T(nameof(StatusPreviewFailed));

    /// <summary>加载资源失败模板 {0}=消息</summary>
    public string StatusResourcesLoadFailed => T(nameof(StatusResourcesLoadFailed));

    /// <summary>加载用户失败模板 {0}=消息</summary>
    public string StatusUsersLoadFailed => T(nameof(StatusUsersLoadFailed));

    /// <summary>加载组失败模板 {0}=消息</summary>
    public string StatusGroupsLoadFailed => T(nameof(StatusGroupsLoadFailed));

    /// <summary>加载角色失败模板 {0}=消息</summary>
    public string StatusRolesLoadFailed => T(nameof(StatusRolesLoadFailed));

    /// <summary>正在加载用户名密码等</summary>
    public string StatusLoadingAll => T(nameof(StatusLoadingAll));

    /// <summary>创建用户需要用户名和密码</summary>
    public string StatusUsernamePasswordRequired => T(nameof(StatusUsernamePasswordRequired));

    /// <summary>正在创建用户模板 {0}=名称</summary>
    public string StatusCreatingUser => T(nameof(StatusCreatingUser));

    /// <summary>用户创建成功模板 {0}=名称</summary>
    public string StatusUserCreated => T(nameof(StatusUserCreated));

    /// <summary>用户创建失败模板 {0}=消息</summary>
    public string StatusUserCreateFailed => T(nameof(StatusUserCreateFailed));

    /// <summary>未选中用户</summary>
    public string StatusNoUserSelected => T(nameof(StatusNoUserSelected));

    /// <summary>正在删除用户模板 {0}=名称</summary>
    public string StatusDeletingUser => T(nameof(StatusDeletingUser));

    /// <summary>用户删除成功模板 {0}=名称</summary>
    public string StatusUserDeleted => T(nameof(StatusUserDeleted));

    /// <summary>用户删除失败模板 {0}=消息</summary>
    public string StatusUserDeleteFailed => T(nameof(StatusUserDeleteFailed));

    /// <summary>正在更新用户模板 {0}=名称</summary>
    public string StatusUpdatingUser => T(nameof(StatusUpdatingUser));

    /// <summary>用户更新成功模板 {0}=名称</summary>
    public string StatusUserUpdated => T(nameof(StatusUserUpdated));

    /// <summary>用户更新失败模板 {0}=消息</summary>
    public string StatusUserUpdateFailed => T(nameof(StatusUserUpdateFailed));

    /// <summary>组名称必填</summary>
    public string StatusGroupNameRequired => T(nameof(StatusGroupNameRequired));

    /// <summary>正在创建组模板 {0}=名称</summary>
    public string StatusCreatingGroup => T(nameof(StatusCreatingGroup));

    /// <summary>组创建成功模板 {0}=名称</summary>
    public string StatusGroupCreated => T(nameof(StatusGroupCreated));

    /// <summary>组创建失败模板 {0}=消息</summary>
    public string StatusGroupCreateFailed => T(nameof(StatusGroupCreateFailed));

    /// <summary>未选中组</summary>
    public string StatusNoGroupSelected => T(nameof(StatusNoGroupSelected));

    /// <summary>正在删除组模板 {0}=名称</summary>
    public string StatusDeletingGroup => T(nameof(StatusDeletingGroup));

    /// <summary>组删除成功模板 {0}=名称</summary>
    public string StatusGroupDeleted => T(nameof(StatusGroupDeleted));

    /// <summary>组删除失败模板 {0}=消息</summary>
    public string StatusGroupDeleteFailed => T(nameof(StatusGroupDeleteFailed));

    // ── E32：连接后真实探测 ─────────────────────────────────────────────────

    /// <summary>连接后连通性探测失败模板 {0}=消息（E32 修复新增）</summary>
    public string StatusConnectVerifyFailed => T(nameof(StatusConnectVerifyFailed));

    // ── E37：GWC 默认设置无 REST 端点 ───────────────────────────────────────

    /// <summary>GWC 默认设置在 GeoServer 3.0.1 无 REST 端点、不可配置的显式提示（E37 显式化）</summary>
    public string StatusGwcNotConfigurable => T(nameof(StatusGwcNotConfigurable));

    // ── 新建数据存储对话框（E40：shapefile 目录约定） ────────────────────────

    /// <summary>数据目录标签（新建存储对话框）</summary>
    public string DataStoreDirectoryLabel => T(nameof(DataStoreDirectoryLabel));

    /// <summary>数据目录水印（相对 data_dir，可选；空=与存储同名约定）</summary>
    public string DataStoreDirectoryHint => T(nameof(DataStoreDirectoryHint));

    // ── SecuritySettings 状态串（E36 本地化） ────────────────────────────────

    /// <summary>已加载 ACL 规则模板 {0}=数量</summary>
    public string StatusAclRulesLoaded => T(nameof(StatusAclRulesLoaded));

    /// <summary>未找到 ACL 规则</summary>
    public string StatusNoAclRules => T(nameof(StatusNoAclRules));

    // ── StyleManagement 状态串（E36 本地化） ─────────────────────────────────

    /// <summary>样式名称必填</summary>
    public string StatusStyleNameRequired => T(nameof(StatusStyleNameRequired));

    /// <summary>SLD 内容必填</summary>
    public string StatusSldContentRequired => T(nameof(StatusSldContentRequired));

    /// <summary>正在加载指定样式模板 {0}=名称</summary>
    public string StatusLoadingStyleNamed => T(nameof(StatusLoadingStyleNamed));

    /// <summary>已加载指定样式 SLD 模板 {0}=名称</summary>
    public string StatusLoadedSldFor => T(nameof(StatusLoadedSldFor));

    /// <summary>加载指定样式失败模板 {0}=消息</summary>
    public string StatusStyleLoadFailed => T(nameof(StatusStyleLoadFailed));

    /// <summary>正在上传指定样式模板 {0}=名称</summary>
    public string StatusUploadingStyleNamed => T(nameof(StatusUploadingStyleNamed));

    /// <summary>样式更新成功模板 {0}=名称</summary>
    public string StatusStyleUpdated => T(nameof(StatusStyleUpdated));

    /// <summary>样式创建成功模板 {0}=名称</summary>
    public string StatusStyleCreated => T(nameof(StatusStyleCreated));

    /// <summary>正在删除指定样式模板 {0}=名称</summary>
    public string StatusDeletingStyleNamed => T(nameof(StatusDeletingStyleNamed));

    /// <summary>样式删除成功模板 {0}=名称</summary>
    public string StatusStyleDeletedNamed => T(nameof(StatusStyleDeletedNamed));

    /// <summary>示例 SLD 已创建</summary>
    public string StatusSampleSldCreated => T(nameof(StatusSampleSldCreated));

    // ── MapPreview 状态串（E36 本地化） ──────────────────────────────────────

    /// <summary>地图预览初态（构造字段默认值）</summary>
    public string StatusMapPreviewReady => T(nameof(StatusMapPreviewReady));

    /// <summary>地图已初始化</summary>
    public string StatusMapInitialized => T(nameof(StatusMapInitialized));

    /// <summary>正在准备图层模板 {0}=工作空间:图层</summary>
    public string StatusPreparingLayer => T(nameof(StatusPreparingLayer));

    /// <summary>WMS URL 已生成模板 {0}=图层全名</summary>
    public string StatusWmsUrlGenerated => T(nameof(StatusWmsUrlGenerated));

    /// <summary>生成预览失败模板 {0}=消息</summary>
    public string StatusPreviewGenerateFailed => T(nameof(StatusPreviewGenerateFailed));

    /// <summary>预览已清除</summary>
    public string StatusPreviewCleared => T(nameof(StatusPreviewCleared));

    /// <summary>地图就绪（WMS 图层预览）</summary>
    public string StatusMapReadyForPreview => T(nameof(StatusMapReadyForPreview));

    // ── ImportWizard（数据导入向导，M2） ─────────────────────────────────────

    /// <summary>向导标题</summary>
    public string WizardTitle => T(nameof(WizardTitle));

    /// <summary>步骤指示模板 {0}=当前步</summary>
    public string WizardStepIndicator => T(nameof(WizardStepIndicator));

    /// <summary>第 1 步标题</summary>
    public string WizardStep1Title => T(nameof(WizardStep1Title));

    /// <summary>第 2 步标题</summary>
    public string WizardStep2Title => T(nameof(WizardStep2Title));

    /// <summary>第 3 步标题</summary>
    public string WizardStep3Title => T(nameof(WizardStep3Title));

    /// <summary>数据源类型标签</summary>
    public string WizardSourceKindLabel => T(nameof(WizardSourceKindLabel));

    /// <summary>类型：Shapefile 目录</summary>
    public string WizardKindShapefile => T(nameof(WizardKindShapefile));

    /// <summary>类型：GeoTIFF 文件</summary>
    public string WizardKindGeoTiff => T(nameof(WizardKindGeoTiff));

    /// <summary>类型：PostGIS 表</summary>
    public string WizardKindPostgis => T(nameof(WizardKindPostgis));

    /// <summary>文件引用标签</summary>
    public string WizardFileRefLabel => T(nameof(WizardFileRefLabel));

    /// <summary>文件引用提示</summary>
    public string WizardFileRefWatermark => T(nameof(WizardFileRefWatermark));

    /// <summary>原始名称标签</summary>
    public string WizardNativeNameLabel => T(nameof(WizardNativeNameLabel));

    /// <summary>PostGIS 主机标签</summary>
    public string WizardPgHostLabel => T(nameof(WizardPgHostLabel));

    /// <summary>PostGIS 端口标签</summary>
    public string WizardPgPortLabel => T(nameof(WizardPgPortLabel));

    /// <summary>PostGIS 数据库标签</summary>
    public string WizardPgDatabaseLabel => T(nameof(WizardPgDatabaseLabel));

    /// <summary>PostGIS 用户标签</summary>
    public string WizardPgUserLabel => T(nameof(WizardPgUserLabel));

    /// <summary>PostGIS 密码标签</summary>
    public string WizardPgPasswordLabel => T(nameof(WizardPgPasswordLabel));

    /// <summary>测试连接按钮</summary>
    public string WizardProbeButton => T(nameof(WizardProbeButton));

    /// <summary>工作空间标签</summary>
    public string WizardWorkspaceLabel => T(nameof(WizardWorkspaceLabel));

    /// <summary>刷新按钮</summary>
    public string WizardRefreshButton => T(nameof(WizardRefreshButton));

    /// <summary>发布名标签</summary>
    public string WizardPublishNameLabel => T(nameof(WizardPublishNameLabel));

    /// <summary>存储名标签</summary>
    public string WizardStoreNameLabel => T(nameof(WizardStoreNameLabel));

    /// <summary>SRS 标签</summary>
    public string WizardSrsLabel => T(nameof(WizardSrsLabel));

    /// <summary>本地预检标签</summary>
    public string WizardLocalPreviewLabel => T(nameof(WizardLocalPreviewLabel));

    /// <summary>解析按钮</summary>
    public string WizardInspectButton => T(nameof(WizardInspectButton));

    /// <summary>上一步按钮</summary>
    public string WizardBackButton => T(nameof(WizardBackButton));

    /// <summary>下一步按钮</summary>
    public string WizardNextButton => T(nameof(WizardNextButton));

    /// <summary>发布按钮</summary>
    public string WizardPublishButton => T(nameof(WizardPublishButton));

    /// <summary>重新开始按钮</summary>
    public string WizardRestartButton => T(nameof(WizardRestartButton));

    /// <summary>结果标签</summary>
    public string WizardResultLabel => T(nameof(WizardResultLabel));

    /// <summary>提示：需要 file: 引用</summary>
    public string WizardStatusNeedFileRef => T(nameof(WizardStatusNeedFileRef));

    /// <summary>提示：需要原始名称</summary>
    public string WizardStatusNeedNativeName => T(nameof(WizardStatusNeedNativeName));

    /// <summary>提示：需要 PostGIS 参数</summary>
    public string WizardStatusNeedPgParams => T(nameof(WizardStatusNeedPgParams));

    /// <summary>提示：需要工作空间</summary>
    public string WizardStatusNeedWorkspace => T(nameof(WizardStatusNeedWorkspace));

    /// <summary>提示：需要发布名</summary>
    public string WizardStatusNeedPublishName => T(nameof(WizardStatusNeedPublishName));

    /// <summary>提示：需要本地路径</summary>
    public string WizardStatusNeedLocalPath => T(nameof(WizardStatusNeedLocalPath));

    /// <summary>预检：路径不存在</summary>
    public string WizardPreviewNotFound => T(nameof(WizardPreviewNotFound));

    /// <summary>预检：未知类型</summary>
    public string WizardPreviewUnknown => T(nameof(WizardPreviewUnknown));

    /// <summary>预检：Shapefile 摘要模板 {0}=记录数 {1}=字段数 {2}=类型名</summary>
    public string WizardPreviewShapefile => T(nameof(WizardPreviewShapefile));

    /// <summary>预检：GeoTIFF 摘要模板 {0}=宽 {1}=高 {2}=CRS</summary>
    public string WizardPreviewGeoTiff => T(nameof(WizardPreviewGeoTiff));

    /// <summary>预检：目录条目行模板 {0}=文件名 {1}=类型 {2}=大小</summary>
    public string WizardPreviewDirEntry => T(nameof(WizardPreviewDirEntry));

    /// <summary>发布成功模板 {0}=限定名</summary>
    public string WizardPublishSuccess => T(nameof(WizardPublishSuccess));

    /// <summary>发布失败模板 {0}=消息</summary>
    public string WizardPublishFailed => T(nameof(WizardPublishFailed));

    /// <summary>探测成功</summary>
    public string WizardProbeOk => T(nameof(WizardProbeOk));

    /// <summary>探测失败模板 {0}=消息</summary>
    public string WizardProbeFailed => T(nameof(WizardProbeFailed));

    // ── SLD Editor（SLD 编辑器，M3） ─────────────────────────────────────

    /// <summary>导航：SLD 编辑器</summary>
    public string NavSldEditor => T(nameof(NavSldEditor));

    /// <summary>编辑器标题</summary>
    public string SldEditorTitle => T(nameof(SldEditorTitle));

    /// <summary>编辑器副标题</summary>
    public string SldEditorSubtitle => T(nameof(SldEditorSubtitle));

    /// <summary>样式选择下拉水印</summary>
    public string SldEditorStyleSelectLabel => T(nameof(SldEditorStyleSelectLabel));

    /// <summary>加载按钮</summary>
    public string SldEditorLoadButton => T(nameof(SldEditorLoadButton));

    /// <summary>新建按钮</summary>
    public string SldEditorNewButton => T(nameof(SldEditorNewButton));

    /// <summary>样式名标签</summary>
    public string SldEditorNameLabel => T(nameof(SldEditorNameLabel));

    /// <summary>保存按钮</summary>
    public string SldEditorSaveButton => T(nameof(SldEditorSaveButton));

    /// <summary>校验按钮</summary>
    public string SldEditorValidateButton => T(nameof(SldEditorValidateButton));

    /// <summary>模式标签</summary>
    public string SldEditorModeLabel => T(nameof(SldEditorModeLabel));

    /// <summary>切换到源码按钮</summary>
    public string SldEditorSwitchToSource => T(nameof(SldEditorSwitchToSource));

    /// <summary>切换到结构化按钮</summary>
    public string SldEditorSwitchToStructured => T(nameof(SldEditorSwitchToStructured));

    /// <summary>规则列表标签</summary>
    public string SldEditorRulesLabel => T(nameof(SldEditorRulesLabel));

    /// <summary>添加规则按钮</summary>
    public string SldEditorAddRuleButton => T(nameof(SldEditorAddRuleButton));

    /// <summary>删除规则按钮</summary>
    public string SldEditorRemoveRuleButton => T(nameof(SldEditorRemoveRuleButton));

    /// <summary>规则名标签</summary>
    public string SldEditorRuleNameLabel => T(nameof(SldEditorRuleNameLabel));

    /// <summary>符号化器标签</summary>
    public string SldEditorSymbolizerKindLabel => T(nameof(SldEditorSymbolizerKindLabel));

    /// <summary>点形状标签</summary>
    public string SldEditorWellKnownNameLabel => T(nameof(SldEditorWellKnownNameLabel));

    /// <summary>尺寸标签</summary>
    public string SldEditorSizeLabel => T(nameof(SldEditorSizeLabel));

    /// <summary>填充颜色标签</summary>
    public string SldEditorFillColorLabel => T(nameof(SldEditorFillColorLabel));

    /// <summary>填充不透明度标签</summary>
    public string SldEditorFillOpacityLabel => T(nameof(SldEditorFillOpacityLabel));

    /// <summary>描边颜色标签</summary>
    public string SldEditorStrokeColorLabel => T(nameof(SldEditorStrokeColorLabel));

    /// <summary>描边宽度标签</summary>
    public string SldEditorStrokeWidthLabel => T(nameof(SldEditorStrokeWidthLabel));

    /// <summary>过滤条件标签</summary>
    public string SldEditorFilterLabel => T(nameof(SldEditorFilterLabel));

    /// <summary>过滤属性标签</summary>
    public string SldEditorFilterPropertyLabel => T(nameof(SldEditorFilterPropertyLabel));

    /// <summary>过滤操作符标签</summary>
    public string SldEditorFilterOperatorLabel => T(nameof(SldEditorFilterOperatorLabel));

    /// <summary>过滤值标签</summary>
    public string SldEditorFilterValueLabel => T(nameof(SldEditorFilterValueLabel));

    /// <summary>源码标签</summary>
    public string SldEditorSourceLabel => T(nameof(SldEditorSourceLabel));

    /// <summary>预览区标签</summary>
    public string SldEditorPreviewSectionLabel => T(nameof(SldEditorPreviewSectionLabel));

    /// <summary>预览图层水印</summary>
    public string SldEditorPreviewLayerLabel => T(nameof(SldEditorPreviewLayerLabel));

    /// <summary>生成预览按钮</summary>
    public string SldEditorPreviewButton => T(nameof(SldEditorPreviewButton));

    /// <summary>就绪</summary>
    public string SldEditorReady => T(nameof(SldEditorReady));

    /// <summary>样式列表已加载 {0}=数量</summary>
    public string SldEditorStatusStylesLoaded => T(nameof(SldEditorStatusStylesLoaded));

    /// <summary>提示：先选择样式</summary>
    public string SldEditorStatusSelectStyle => T(nameof(SldEditorStatusSelectStyle));

    /// <summary>样式已加载 {0}=样式名</summary>
    public string SldEditorStatusStyleLoaded => T(nameof(SldEditorStatusStyleLoaded));

    /// <summary>样式已加载（带警告） {0}=样式名 {1}=警告</summary>
    public string SldEditorStatusStyleLoadedWarn => T(nameof(SldEditorStatusStyleLoadedWarn));

    /// <summary>解析失败 {0}=错误</summary>
    public string SldEditorStatusParseFailed => T(nameof(SldEditorStatusParseFailed));

    /// <summary>新建就绪</summary>
    public string SldEditorStatusNewReady => T(nameof(SldEditorStatusNewReady));

    /// <summary>规则已添加</summary>
    public string SldEditorStatusRuleAdded => T(nameof(SldEditorStatusRuleAdded));

    /// <summary>规则已删除</summary>
    public string SldEditorStatusRuleRemoved => T(nameof(SldEditorStatusRuleRemoved));

    /// <summary>提示：先选择规则</summary>
    public string SldEditorStatusNoRuleSelected => T(nameof(SldEditorStatusNoRuleSelected));

    /// <summary>提示：先填写样式名</summary>
    public string SldEditorStatusNeedName => T(nameof(SldEditorStatusNeedName));

    /// <summary>已切换到源码模式</summary>
    public string SldEditorStatusSwitchedToSource => T(nameof(SldEditorStatusSwitchedToSource));

    /// <summary>已切换到结构化模式</summary>
    public string SldEditorStatusSwitchedToStructured => T(nameof(SldEditorStatusSwitchedToStructured));

    /// <summary>已切换到结构化模式（带警告） {0}=警告</summary>
    public string SldEditorStatusSwitchedWarn => T(nameof(SldEditorStatusSwitchedWarn));

    /// <summary>切换失败 {0}=错误</summary>
    public string SldEditorStatusSwitchFailed => T(nameof(SldEditorStatusSwitchFailed));

    /// <summary>校验通过</summary>
    public string SldEditorStatusValid => T(nameof(SldEditorStatusValid));

    /// <summary>校验未通过 {0}=错误列表</summary>
    public string SldEditorStatusInvalid => T(nameof(SldEditorStatusInvalid));

    /// <summary>已保存 {0}=样式名</summary>
    public string SldEditorStatusSaved => T(nameof(SldEditorStatusSaved));

    /// <summary>保存失败 {0}=消息</summary>
    public string SldEditorStatusSaveFailed => T(nameof(SldEditorStatusSaveFailed));

    /// <summary>加载失败 {0}=消息</summary>
    public string SldEditorStatusLoadFailed => T(nameof(SldEditorStatusLoadFailed));

    /// <summary>提示：先保存再预览</summary>
    public string SldEditorStatusNeedSaveForPreview => T(nameof(SldEditorStatusNeedSaveForPreview));

    /// <summary>提示：先选择预览图层</summary>
    public string SldEditorStatusNeedLayer => T(nameof(SldEditorStatusNeedLayer));

    /// <summary>预览地址已生成 {0}=图层名</summary>
    public string SldEditorStatusPreviewReady => T(nameof(SldEditorStatusPreviewReady));

    // ── Style Library（样式库，M3） ─────────────────────────────────────

    /// <summary>导航：样式库</summary>
    public string NavStyleLibrary => T(nameof(NavStyleLibrary));

    /// <summary>样式库标题</summary>
    public string StyleLibTitle => T(nameof(StyleLibTitle));

    /// <summary>样式库副标题</summary>
    public string StyleLibSubtitle => T(nameof(StyleLibSubtitle));

    /// <summary>刷新按钮</summary>
    public string StyleLibRefreshButton => T(nameof(StyleLibRefreshButton));

    /// <summary>仅显示未引用</summary>
    public string StyleLibUnusedOnly => T(nameof(StyleLibUnusedOnly));

    /// <summary>删除选中按钮</summary>
    public string StyleLibDeleteButton => T(nameof(StyleLibDeleteButton));

    /// <summary>列头：样式</summary>
    public string StyleLibColumnStyle => T(nameof(StyleLibColumnStyle));

    /// <summary>列头：引用图层</summary>
    public string StyleLibColumnLayers => T(nameof(StyleLibColumnLayers));

    /// <summary>未引用标记</summary>
    public string StyleLibUnused => T(nameof(StyleLibUnused));

    /// <summary>就绪</summary>
    public string StyleLibReady => T(nameof(StyleLibReady));

    /// <summary>已加载 {0}=总数 {1}=未引用数</summary>
    public string StyleLibStatusLoaded => T(nameof(StyleLibStatusLoaded));

    /// <summary>加载失败 {0}=消息</summary>
    public string StyleLibStatusLoadFailed => T(nameof(StyleLibStatusLoadFailed));

    /// <summary>提示：先选择样式</summary>
    public string StyleLibStatusSelectFirst => T(nameof(StyleLibStatusSelectFirst));

    /// <summary>无法删除：仍被引用</summary>
    public string StyleLibStatusDeleteInUse => T(nameof(StyleLibStatusDeleteInUse));

    /// <summary>已删除 {0}=样式名</summary>
    public string StyleLibStatusDeleted => T(nameof(StyleLibStatusDeleted));

    /// <summary>删除失败 {0}=消息</summary>
    public string StyleLibStatusDeleteFailed => T(nameof(StyleLibStatusDeleteFailed));

    // ── M4：批量操作 ───────────────────────────────────────────────────────

    /// <summary>导航分组：工具</summary>
    public string NavTools => T(nameof(NavTools));

    /// <summary>导航/标题：批量操作</summary>
    public string NavBatch => T(nameof(NavBatch));

    /// <summary>批量操作副标题</summary>
    public string BatchSubtitle => T(nameof(BatchSubtitle));

    /// <summary>资源类型</summary>
    public string BatchScopeLabel => T(nameof(BatchScopeLabel));

    /// <summary>工作空间筛选</summary>
    public string BatchWorkspaceLabel => T(nameof(BatchWorkspaceLabel));

    /// <summary>刷新</summary>
    public string BatchRefresh => T(nameof(BatchRefresh));

    /// <summary>全选/全不选</summary>
    public string BatchToggleAll => T(nameof(BatchToggleAll));

    /// <summary>级联选项</summary>
    public string BatchCascade => T(nameof(BatchCascade));

    /// <summary>新默认样式选择</summary>
    public string BatchNewStyle => T(nameof(BatchNewStyle));

    /// <summary>应用默认样式按钮</summary>
    public string BatchApplyStyle => T(nameof(BatchApplyStyle));

    /// <summary>启用按钮</summary>
    public string BatchEnable => T(nameof(BatchEnable));

    /// <summary>禁用按钮</summary>
    public string BatchDisable => T(nameof(BatchDisable));

    /// <summary>删除按钮</summary>
    public string BatchDelete => T(nameof(BatchDelete));

    /// <summary>列：名称</summary>
    public string BatchColumnName => T(nameof(BatchColumnName));

    /// <summary>列：当前默认样式</summary>
    public string BatchColumnStyle => T(nameof(BatchColumnStyle));

    /// <summary>列：类型</summary>
    public string BatchColumnType => T(nameof(BatchColumnType));

    /// <summary>提示：选择图层后再改样式</summary>
    public string BatchStatusNeedLayers => T(nameof(BatchStatusNeedLayers));

    /// <summary>提示：选择新样式</summary>
    public string BatchStatusNeedStyle => T(nameof(BatchStatusNeedStyle));

    /// <summary>提示：选择存储后再启停</summary>
    public string BatchStatusNeedStores => T(nameof(BatchStatusNeedStores));

    /// <summary>提示：先选择条目</summary>
    public string BatchStatusNeedSelection => T(nameof(BatchStatusNeedSelection));

    /// <summary>提示：该类型不支持批量删除</summary>
    public string BatchStatusDeleteUnsupported => T(nameof(BatchStatusDeleteUnsupported));

    /// <summary>就绪</summary>
    public string BatchReady => T(nameof(BatchReady));

    /// <summary>已加载 {0} 条（已选 {1}）</summary>
    public string BatchStatusLoaded => T(nameof(BatchStatusLoaded));

    /// <summary>全部成功 {0} 项</summary>
    public string BatchStatusAllOk => T(nameof(BatchStatusAllOk));

    /// <summary>部分失败：成功 {0} 失败 {1}：{2}</summary>
    public string BatchStatusPartial => T(nameof(BatchStatusPartial));

    /// <summary>批量操作失败 {0}</summary>
    public string BatchStatusFailed => T(nameof(BatchStatusFailed));

    // ── M4：工作空间迁移（导入/导出） ─────────────────────────────────────

    /// <summary>导航/标题：工作空间迁移</summary>
    public string NavMigration => T(nameof(NavMigration));

    /// <summary>迁移副标题</summary>
    public string MigSubtitle => T(nameof(MigSubtitle));

    /// <summary>导出分组</summary>
    public string MigExportGroup => T(nameof(MigExportGroup));

    /// <summary>导入分组</summary>
    public string MigImportGroup => T(nameof(MigImportGroup));

    /// <summary>导出按钮</summary>
    public string MigExport => T(nameof(MigExport));

    /// <summary>保存按钮</summary>
    public string MigSave => T(nameof(MigSave));

    /// <summary>浏览按钮</summary>
    public string MigBrowse => T(nameof(MigBrowse));

    /// <summary>导入按钮</summary>
    public string MigImport => T(nameof(MigImport));

    /// <summary>归档路径</summary>
    public string MigArchivePath => T(nameof(MigArchivePath));

    /// <summary>目标工作空间</summary>
    public string MigTargetWs => T(nameof(MigTargetWs));

    /// <summary>目标命名空间前缀</summary>
    public string MigTargetPrefix => T(nameof(MigTargetPrefix));

    /// <summary>目标命名空间 URI</summary>
    public string MigTargetUri => T(nameof(MigTargetUri));

    /// <summary>覆盖已存在</summary>
    public string MigOverwrite => T(nameof(MigOverwrite));

    /// <summary>列：步骤</summary>
    public string MigColumnStep => T(nameof(MigColumnStep));

    /// <summary>列：目标</summary>
    public string MigColumnTarget => T(nameof(MigColumnTarget));

    /// <summary>列：状态</summary>
    public string MigColumnStatus => T(nameof(MigColumnStatus));

    /// <summary>列：说明</summary>
    public string MigColumnMessage => T(nameof(MigColumnMessage));

    /// <summary>状态：已创建</summary>
    public string MigStatusCreated => T(nameof(MigStatusCreated));

    /// <summary>状态：已更新</summary>
    public string MigStatusUpdated => T(nameof(MigStatusUpdated));

    /// <summary>状态：已跳过</summary>
    public string MigStatusSkipped => T(nameof(MigStatusSkipped));

    /// <summary>状态：失败</summary>
    public string MigStatusFailedItem => T(nameof(MigStatusFailedItem));

    /// <summary>就绪</summary>
    public string MigReady => T(nameof(MigReady));

    /// <summary>已加载 {0} 个工作空间</summary>
    public string MigStatusLoaded => T(nameof(MigStatusLoaded));

    /// <summary>导出完成：{0} 工作空间，{1} 项资源，{2} 警告，{3} 字节</summary>
    public string MigExportDone => T(nameof(MigExportDone));

    /// <summary>已保存到 {0}</summary>
    public string MigSaved => T(nameof(MigSaved));

    /// <summary>导入完成：创建 {0} 项，跳过 {1} 项</summary>
    public string MigImportDone => T(nameof(MigImportDone));

    /// <summary>导入有失败：{0} 项失败：{1}</summary>
    public string MigImportPartial => T(nameof(MigImportPartial));

    /// <summary>请先选择工作空间</summary>
    public string MigStatusNeedWorkspace => T(nameof(MigStatusNeedWorkspace));

    /// <summary>请先导出</summary>
    public string MigStatusExportFirst => T(nameof(MigStatusExportFirst));

    /// <summary>未选择保存路径</summary>
    public string MigStatusNoPath => T(nameof(MigStatusNoPath));

    /// <summary>归档文件不存在</summary>
    public string MigStatusNeedArchive => T(nameof(MigStatusNeedArchive));

    /// <summary>失败 {0}</summary>
    public string MigStatusFailed => T(nameof(MigStatusFailed));

    // ── M4：设置同步 ───────────────────────────────────────────────────────

    /// <summary>导航/标题：设置同步</summary>
    public string NavSettingsSync => T(nameof(NavSettingsSync));

    /// <summary>设置同步副标题</summary>
    public string SyncSubtitle => T(nameof(SyncSubtitle));

    /// <summary>源连接分组</summary>
    public string SyncSourceGroup => T(nameof(SyncSourceGroup));

    /// <summary>URL 标签</summary>
    public string SyncUrl => T(nameof(SyncUrl));

    /// <summary>用户标签</summary>
    public string SyncUser => T(nameof(SyncUser));

    /// <summary>密码标签</summary>
    public string SyncPassword => T(nameof(SyncPassword));

    /// <summary>域标签</summary>
    public string SyncDomain => T(nameof(SyncDomain));

    /// <summary>比对按钮</summary>
    public string SyncCompare => T(nameof(SyncCompare));

    /// <summary>应用按钮</summary>
    public string SyncApply => T(nameof(SyncApply));

    /// <summary>列：路径</summary>
    public string SyncColumnPath => T(nameof(SyncColumnPath));

    /// <summary>列：源值</summary>
    public string SyncColumnSource => T(nameof(SyncColumnSource));

    /// <summary>列：目标值</summary>
    public string SyncColumnTarget => T(nameof(SyncColumnTarget));

    /// <summary>列：类型</summary>
    public string SyncColumnKind => T(nameof(SyncColumnKind));

    /// <summary>差异类型：源侧新增</summary>
    public string SyncKindAdded => T(nameof(SyncKindAdded));

    /// <summary>差异类型：源侧删除</summary>
    public string SyncKindRemoved => T(nameof(SyncKindRemoved));

    /// <summary>差异类型：变更</summary>
    public string SyncKindChanged => T(nameof(SyncKindChanged));

    /// <summary>就绪</summary>
    public string SyncReady => T(nameof(SyncReady));

    /// <summary>两侧一致</summary>
    public string SyncStatusIdentical => T(nameof(SyncStatusIdentical));

    /// <summary>发现 {0} 项差异</summary>
    public string SyncStatusLoaded => T(nameof(SyncStatusLoaded));

    /// <summary>已应用 {0} 项</summary>
    public string SyncStatusApplied => T(nameof(SyncStatusApplied));

    /// <summary>请填写源 URL</summary>
    public string SyncStatusNeedUrl => T(nameof(SyncStatusNeedUrl));

    /// <summary>先比对</summary>
    public string SyncStatusCompareFirst => T(nameof(SyncStatusCompareFirst));

    /// <summary>勾选差异项</summary>
    public string SyncStatusNeedSelection => T(nameof(SyncStatusNeedSelection));

    /// <summary>失败 {0}</summary>
    public string SyncStatusFailed => T(nameof(SyncStatusFailed));
}
