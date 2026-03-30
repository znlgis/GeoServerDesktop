using CommunityToolkit.Mvvm.ComponentModel;

namespace GeoServerDesktop.App.Services;

/// <summary>
/// 应用程序本地化服务，支持中英文切换
/// </summary>
public class LocalizationService : ObservableObject
{
    private static readonly LocalizationService _instance = new();

    /// <summary>
    /// 获取单例实例
    /// </summary>
    public static LocalizationService Instance => _instance;

    private bool _isChinese;

    /// <summary>
    /// 切换语言
    /// </summary>
    public void ToggleLanguage()
    {
        _isChinese = !_isChinese;
        OnPropertyChanged(string.Empty);
    }

    private string T(string en, string zh) => _isChinese ? zh : en;

    // ── MainWindow ─────────────────────────────────────────────────────────

    /// <summary>语言切换按钮文本</summary>
    public string LanguageToggleText => T("中文", "English");

    /// <summary>语言切换提示</summary>
    public string LanguageToggleTooltip => T("切换为中文", "Switch to English");

    /// <summary>应用程序副标题</summary>
    public string AppHeaderSubtitle => T("Administration", "管理");

    /// <summary>URL 标签</summary>
    public string LabelUrl => T("URL:", "URL:");

    /// <summary>用户名标签</summary>
    public string LabelUser => T("User:", "用户:");

    /// <summary>密码标签</summary>
    public string LabelPassword => T("Password:", "密码:");

    /// <summary>登录按钮</summary>
    public string Login => T("Login", "登录");

    /// <summary>已连接状态</summary>
    public string Connected => T("● Connected", "● 已连接");

    /// <summary>注销按钮</summary>
    public string Logout => T("Logout", "注销");

    // ── 左侧导航 ────────────────────────────────────────────────────────────

    /// <summary>关于与状态</summary>
    public string NavAboutAndStatus => T("About & Status", "关于 & 状态");

    /// <summary>关于 GeoServer</summary>
    public string NavAboutGeoServer => T("About GeoServer", "关于 GeoServer");

    /// <summary>数据</summary>
    public string NavData => T("Data", "数据");

    /// <summary>图层预览</summary>
    public string NavLayerPreview => T("Layer Preview", "图层预览");

    /// <summary>工作空间</summary>
    public string NavWorkspaces => T("Workspaces", "工作空间");

    /// <summary>数据存储</summary>
    public string NavStores => T("Stores", "数据存储");

    /// <summary>图层</summary>
    public string NavLayers => T("Layers", "图层");

    /// <summary>图层组</summary>
    public string NavLayerGroups => T("Layer Groups", "图层组");

    /// <summary>样式</summary>
    public string NavStyles => T("Styles", "样式");

    /// <summary>服务</summary>
    public string NavServices => T("Services", "服务");

    /// <summary>设置</summary>
    public string NavSettings => T("Settings", "设置");

    /// <summary>全局</summary>
    public string NavGlobal => T("Global", "全局");

    /// <summary>日志</summary>
    public string NavLogging => T("Logging", "日志");

    /// <summary>切片缓存</summary>
    public string NavTileCaching => T("Tile Caching", "切片缓存");

    /// <summary>缓存默认值</summary>
    public string NavCachingDefaults => T("Caching Defaults", "缓存默认值");

    /// <summary>格网集</summary>
    public string NavGridsets => T("Gridsets", "格网集");

    /// <summary>磁盘配额</summary>
    public string NavDiskQuota => T("Disk Quota", "磁盘配额");

    /// <summary>安全</summary>
    public string NavSecurity => T("Security", "安全");

    /// <summary>安全设置（导航）</summary>
    public string NavSecuritySettings => T("Settings", "设置");

    /// <summary>用户组角色（导航）</summary>
    public string NavUsersGroupsRoles => T("Users, Groups, Roles", "用户、组、角色");

    // ── 通用 ────────────────────────────────────────────────────────────────

    /// <summary>刷新</summary>
    public string Refresh => T("🔄 Refresh", "🔄 刷新");

    /// <summary>创建</summary>
    public string Create => T("Create", "创建");

    /// <summary>删除</summary>
    public string Delete => T("Delete", "删除");

    /// <summary>取消</summary>
    public string Cancel => T("Cancel", "取消");

    /// <summary>保存设置</summary>
    public string SaveSettings => T("Save Settings", "保存设置");

    /// <summary>加载</summary>
    public string Load => T("Load", "加载");

    /// <summary>操作</summary>
    public string Actions => T("Actions", "操作");

    /// <summary>处理中</summary>
    public string Processing => T("Processing...", "处理中...");

    /// <summary>加载中</summary>
    public string Loading => T("Loading...", "加载中...");

    /// <summary>名称</summary>
    public string NameLabel => T("Name:", "名称:");

    /// <summary>描述</summary>
    public string DescriptionLabel => T("Description:", "描述:");

    /// <summary>标题标签</summary>
    public string TitleLabel => T("Title:", "标题:");

    /// <summary>删除选中</summary>
    public string DeleteSelected => T("Delete Selected", "删除选中");

    /// <summary>已启用</summary>
    public string EnabledLabel => T("Enabled:", "已启用:");

    /// <summary>工作空间标签</summary>
    public string WorkspaceLabel => T("Workspace", "工作空间");

    /// <summary>服务配置</summary>
    public string ServiceConfiguration => T("Service Configuration", "服务配置");

    /// <summary>服务信息</summary>
    public string ServiceInformation => T("Service Information", "服务信息");

    /// <summary>名称输入框</summary>
    public string ServiceNameHint => T("Service name", "服务名称");

    /// <summary>标题输入框</summary>
    public string ServiceTitleHint => T("Service title", "服务标题");

    /// <summary>摘要输入框</summary>
    public string ServiceAbstractHint => T("Service abstract", "服务摘要");

    /// <summary>维护者输入框</summary>
    public string MaintainerHint => T("Maintainer", "维护者");

    /// <summary>在线资源 URL 输入框</summary>
    public string OnlineResourceUrlHint => T("Online resource URL", "在线资源 URL");

    /// <summary>在线资源标签</summary>
    public string OnlineResourceLabel => T("Online Resource:", "在线资源:");

    /// <summary>维护者标签</summary>
    public string MaintainerLabel => T("Maintainer:", "维护者:");

    /// <summary>摘要标签</summary>
    public string AbstractLabel => T("Abstract:", "摘要:");

    /// <summary>CITE 合规</summary>
    public string CiteCompliant => T("CITE Compliant", "CITE 合规");

    // ── WorkspaceManagement ────────────────────────────────────────────────

    /// <summary>工作空间管理标题</summary>
    public string WorkspaceManagementTitle => T("Workspace Management", "工作空间管理");

    /// <summary>重新加载工作空间提示</summary>
    public string ReloadWorkspacesToolTip => T("Reload workspaces", "重新加载工作空间");

    /// <summary>已有工作空间</summary>
    public string ExistingWorkspaces => T("Existing Workspaces", "已有工作空间");

    /// <summary>新建工作空间</summary>
    public string CreateNewWorkspace => T("Create New Workspace", "新建工作空间");

    /// <summary>输入工作空间名称水印</summary>
    public string EnterWorkspaceNameHint => T("Enter workspace name", "输入工作空间名称");

    /// <summary>删除工作空间</summary>
    public string DeleteWorkspace => T("Delete Workspace", "删除工作空间");

    /// <summary>删除工作空间警告</summary>
    public string DeleteWorkspaceWarning => T("Warning: This will delete the selected workspace.", "警告：这将删除所选工作空间。");

    // ── StoresManagement ───────────────────────────────────────────────────

    /// <summary>数据存储标题</summary>
    public string DataStoresTitle => T("Data Stores", "数据存储");

    /// <summary>数据存储副标题</summary>
    public string DataStoresSubtitle => T("Manage data store connections to vector and raster data sources", "管理矢量和栅格数据源的数据存储连接");

    /// <summary>添加存储按钮</summary>
    public string AddStore => T("+ Add Store", "+ 添加存储");

    /// <summary>选择工作空间提示（存储）</summary>
    public string SelectWorkspaceToViewStoresHint => T("Select a workspace to view its data stores", "选择工作空间以查看数据存储");

    /// <summary>选择工作空间提示（列表）</summary>
    public string SelectWorkspaceToViewStores => T("Select a workspace to view data stores", "请先选择工作空间");

    /// <summary>添加数据存储对话框标题</summary>
    public string AddDataStoreTitle => T("Add Data Store", "添加数据存储");

    /// <summary>数据存储名称水印</summary>
    public string DataStoreNameHint => T("Data store name", "数据存储名称");

    /// <summary>可选描述水印</summary>
    public string OptionalDescriptionHint => T("Optional description", "可选描述");

    // ── LayersManagement ───────────────────────────────────────────────────

    /// <summary>图层标题</summary>
    public string LayersTitle => T("Layers", "图层");

    /// <summary>图层副标题</summary>
    public string LayersSubtitle => T("Manage published vector and raster layers", "管理已发布的矢量和栅格图层");

    /// <summary>发布图层按钮</summary>
    public string PublishLayer => T("+ Publish Layer", "+ 发布图层");

    /// <summary>选择工作空间过滤图层</summary>
    public string SelectWorkspaceFilterLayersHint => T("Select a workspace to filter layers", "选择工作空间以过滤图层");

    /// <summary>选择工作空间查看图层</summary>
    public string SelectWorkspaceToViewLayers => T("Select a workspace to view layers", "请先选择工作空间");

    /// <summary>发布新图层对话框标题</summary>
    public string PublishNewLayerTitle => T("Publish New Layer", "发布新图层");

    /// <summary>数据存储标签</summary>
    public string DataStoreLabel => T("Data Store:", "数据存储:");

    /// <summary>图层名称标签</summary>
    public string LayerNameLabel => T("Layer Name:", "图层名称:");

    /// <summary>原生名称标签</summary>
    public string NativeNameLabel => T("Native Name:", "原生名称:");

    /// <summary>SRS 标签</summary>
    public string SrsLabel => T("SRS:", "SRS:");

    /// <summary>已发布图层名称水印</summary>
    public string PublishedLayerNameHint => T("Published layer name", "发布图层名称");

    /// <summary>原生名称水印</summary>
    public string NativeNameHint => T("Native name in data source (optional)", "数据源中的原生名称（可选）");

    /// <summary>发布按钮</summary>
    public string PublishButton => T("Publish", "发布");

    /// <summary>编辑按钮</summary>
    public string EditButton => T("Edit", "编辑");

    /// <summary>默认样式标签</summary>
    public string DefaultStyleLabel => T("Default Style:", "默认样式:");

    /// <summary>资源标签</summary>
    public string ResourceLabel => T("Resource:", "资源:");

    // ── LayerGroupsManagement ──────────────────────────────────────────────

    /// <summary>图层组标题</summary>
    public string LayerGroupsTitle => T("Layer Groups", "图层组");

    /// <summary>图层组副标题</summary>
    public string LayerGroupsSubtitle => T("Manage layer groups to organize multiple layers together", "管理图层组以将多个图层组织在一起");

    /// <summary>创建按钮（图层组）</summary>
    public string CreateLayerGroupButton => T("➕ Create", "➕ 创建");

    /// <summary>选择工作空间过滤图层组</summary>
    public string SelectWorkspaceFilterLayerGroupsHint => T("Select a workspace to filter layer groups", "选择工作空间以过滤图层组");

    /// <summary>选择工作空间查看图层组</summary>
    public string SelectWorkspaceToViewLayerGroups => T("Select a workspace to view layer groups", "请先选择工作空间");

    /// <summary>创建图层组对话框标题</summary>
    public string CreateLayerGroupTitle => T("Create Layer Group", "创建图层组");

    /// <summary>名称必填标签</summary>
    public string NameRequiredLabel => T("Name *", "名称 *");

    /// <summary>图层组名称水印</summary>
    public string EnterLayerGroupNameHint => T("Enter layer group name", "输入图层组名称");

    /// <summary>图层组标题水印</summary>
    public string EnterLayerGroupTitleHint => T("Enter layer group title (optional)", "输入图层组标题（可选）");

    /// <summary>模式标签</summary>
    public string ModeLabel => T("Mode:", "模式:");

    /// <summary>图层数量标签</summary>
    public string LayersCountLabel => T("Layers:", "图层数:");

    /// <summary>工作区标签</summary>
    public string WorkspaceItemLabel => T("Workspace:", "工作空间:");

    // ── StyleManagement ────────────────────────────────────────────────────

    /// <summary>样式管理标题</summary>
    public string StyleManagementTitle => T("Style Management", "样式管理");

    /// <summary>重新加载样式提示</summary>
    public string ReloadStylesToolTip => T("Reload styles", "重新加载样式");

    /// <summary>已有样式</summary>
    public string ExistingStyles => T("Existing Styles", "已有样式");

    /// <summary>样式名称水印</summary>
    public string StyleNameHint => T("Style name", "样式名称");

    /// <summary>加载选中</summary>
    public string LoadSelected => T("Load Selected", "加载选中");

    /// <summary>示例 SLD</summary>
    public string SampleSld => T("Sample SLD", "示例 SLD");

    /// <summary>上传/更新</summary>
    public string UploadUpdate => T("Upload/Update", "上传/更新");

    /// <summary>SLD 内容</summary>
    public string SldContent => T("SLD Content", "SLD 内容");

    /// <summary>SLD 内容水印</summary>
    public string SldContentHint => T("Enter SLD XML content here...", "在此输入 SLD XML 内容...");

    // ── MapPreview ─────────────────────────────────────────────────────────

    /// <summary>地图预览标题</summary>
    public string MapPreviewTitle => T("Map Preview", "地图预览");

    /// <summary>重置视图提示</summary>
    public string ResetViewToolTip => T("Reset view", "重置视图");

    /// <summary>清除图层提示</summary>
    public string ClearLayerToolTip => T("Clear current layer", "清除当前图层");

    /// <summary>重置按钮</summary>
    public string ResetButton => T("🏠 Reset", "🏠 重置");

    /// <summary>清除按钮</summary>
    public string ClearButton => T("🗑️ Clear", "🗑️ 清除");

    /// <summary>地图预览描述</summary>
    public string MapPreviewDescription => T("This view provides WMS layer preview capabilities.", "本视图提供 WMS 图层预览功能。");

    /// <summary>当前图层 WMS URL</summary>
    public string CurrentLayerWmsUrl => T("Current Layer WMS URL:", "当前图层 WMS URL：");

    /// <summary>WMS URL 使用提示</summary>
    public string WmsUrlUsageHint => T("You can use this URL in GIS applications like QGIS, ArcGIS, or web mapping libraries.", "您可以在 QGIS、ArcGIS 等 GIS 应用或 Web 地图库中使用此 URL。");

    /// <summary>预览图层方法标题</summary>
    public string HowToPreviewTitle => T("How to Preview Layers:", "如何预览图层：");

    /// <summary>预览步骤 1</summary>
    public string PreviewStep1 => T("1. Connect to a GeoServer instance", "1. 连接到 GeoServer 实例");

    /// <summary>预览步骤 2</summary>
    public string PreviewStep2 => T("2. Select a layer from the resource tree", "2. 从资源树中选择图层");

    /// <summary>预览步骤 3</summary>
    public string PreviewStep3 => T("3. The WMS URL will be generated and displayed here", "3. WMS URL 将在此处生成并显示");

    /// <summary>预览步骤 4</summary>
    public string PreviewStep4 => T("4. Use the URL in your preferred GIS client", "4. 在您偏好的 GIS 客户端中使用该 URL");

    /// <summary>地图预览底栏信息</summary>
    public string MapPreviewInfo => T("WMS layer preview with URL generation for external GIS applications", "WMS 图层预览，为外部 GIS 应用生成 URL");

    // ── About ──────────────────────────────────────────────────────────────

    /// <summary>关于 GeoServer 标题</summary>
    public string AboutGeoServerTitle => T("About GeoServer", "关于 GeoServer");

    /// <summary>关于副标题</summary>
    public string AboutSubtitle => T("System information and version details", "系统信息与版本详情");

    /// <summary>版本信息</summary>
    public string VersionInformation => T("Version Information", "版本信息");

    /// <summary>GeoServer 版本标签</summary>
    public string GeoServerVersionLabel => T("GeoServer Version:", "GeoServer 版本:");

    /// <summary>GeoTools 版本标签</summary>
    public string GeoToolsVersionLabel => T("GeoTools Version:", "GeoTools 版本:");

    /// <summary>GeoWebCache 版本标签</summary>
    public string GeoWebCacheVersionLabel => T("GeoWebCache Version:", "GeoWebCache 版本:");

    // ── GlobalSettings ─────────────────────────────────────────────────────

    /// <summary>全局设置标题</summary>
    public string GlobalSettingsTitle => T("Global Settings", "全局设置");

    /// <summary>加载全局设置提示</summary>
    public string LoadGlobalSettingsToolTip => T("Load current global settings", "加载当前全局设置");

    /// <summary>联系信息</summary>
    public string ContactInformation => T("Contact Information", "联系信息");

    /// <summary>联系人标签</summary>
    public string ContactPersonLabel => T("Contact Person:", "联系人:");

    /// <summary>组织标签</summary>
    public string OrganizationLabel => T("Organization:", "组织:");

    /// <summary>电子邮件标签</summary>
    public string EmailLabel => T("Email:", "电子邮件:");

    /// <summary>联系人水印</summary>
    public string ContactPersonHint => T("Contact person name", "联系人姓名");

    /// <summary>组织名称水印</summary>
    public string OrganizationNameHint => T("Organization name", "组织名称");

    /// <summary>电子邮件地址水印</summary>
    public string EmailAddressHint => T("Email address", "电子邮件地址");

    /// <summary>服务器配置</summary>
    public string ServerConfiguration => T("Server Configuration", "服务器配置");

    /// <summary>代理基础 URL 标签</summary>
    public string ProxyBaseUrlLabel => T("Proxy Base URL:", "代理基础 URL:");

    /// <summary>代理基础 URL 水印</summary>
    public string ProxyBaseUrlHint => T("Proxy base URL", "代理基础 URL");

    /// <summary>详细异常</summary>
    public string VerboseExceptions => T("Verbose Exceptions", "详细异常信息");

    // ── WMS Settings ───────────────────────────────────────────────────────

    /// <summary>WMS 设置标题</summary>
    public string WmsSettingsTitle => T("WMS Settings", "WMS 设置");

    /// <summary>加载 WMS 设置提示</summary>
    public string LoadWmsSettingsToolTip => T("Load current WMS settings", "加载当前 WMS 设置");

    /// <summary>启用 WMS 服务</summary>
    public string EnableWmsService => T("Enable WMS Service", "启用 WMS 服务");

    // ── WFS Settings ───────────────────────────────────────────────────────

    /// <summary>WFS 设置标题</summary>
    public string WfsSettingsTitle => T("WFS Settings", "WFS 设置");

    /// <summary>加载 WFS 设置提示</summary>
    public string LoadWfsSettingsToolTip => T("Load current WFS settings", "加载当前 WFS 设置");

    /// <summary>启用 WFS 服务</summary>
    public string EnableWfsService => T("Enable WFS Service", "启用 WFS 服务");

    /// <summary>WFS 参数</summary>
    public string WfsParameters => T("WFS Parameters", "WFS 参数");

    /// <summary>最大要素数标签</summary>
    public string MaxFeaturesLabel => T("Max Features:", "最大要素数:");

    /// <summary>服务级别标签</summary>
    public string ServiceLevelLabel => T("Service Level:", "服务级别:");

    /// <summary>要素边界</summary>
    public string FeatureBounding => T("Feature Bounding", "要素边界");

    // ── WCS Settings ───────────────────────────────────────────────────────

    /// <summary>WCS 设置标题</summary>
    public string WcsSettingsTitle => T("WCS Settings", "WCS 设置");

    /// <summary>加载 WCS 设置提示</summary>
    public string LoadWcsSettingsToolTip => T("Load current WCS settings", "加载当前 WCS 设置");

    /// <summary>启用 WCS 服务</summary>
    public string EnableWcsService => T("Enable WCS Service", "启用 WCS 服务");

    /// <summary>WCS 参数</summary>
    public string WcsParameters => T("WCS Parameters", "WCS 参数");

    /// <summary>最大输入内存标签</summary>
    public string MaxInputMemoryLabel => T("Max Input Memory (KB):", "最大输入内存 (KB):");

    /// <summary>最大输出内存标签</summary>
    public string MaxOutputMemoryLabel => T("Max Output Memory (KB):", "最大输出内存 (KB):");

    /// <summary>子采样启用</summary>
    public string SubsamplingEnabled => T("Subsampling Enabled", "启用子采样");

    // ── Logging ────────────────────────────────────────────────────────────

    /// <summary>日志设置标题</summary>
    public string LoggingSettingsTitle => T("Logging Settings", "日志设置");

    /// <summary>加载日志设置提示</summary>
    public string LoadLoggingSettingsToolTip => T("Load current logging settings", "加载当前日志设置");

    /// <summary>日志配置</summary>
    public string LoggingConfiguration => T("Logging Configuration", "日志配置");

    /// <summary>日志级别标签</summary>
    public string LogLevelLabel => T("Log Level:", "日志级别:");

    /// <summary>日志位置标签</summary>
    public string LogLocationLabel => T("Log Location:", "日志位置:");

    /// <summary>日志文件路径水印</summary>
    public string LogFilePathHint => T("Log file path", "日志文件路径");

    /// <summary>标准输出日志</summary>
    public string StdOutLogging => T("Log to Standard Output (stdout)", "输出日志到标准输出 (stdout)");

    /// <summary>启用文件日志</summary>
    public string EnableFileLogging => T("Enable File Logging", "启用文件日志");

    // ── CachingDefaults ────────────────────────────────────────────────────

    /// <summary>切片缓存默认值标题</summary>
    public string TileCachingDefaultsTitle => T("Tile Caching Defaults", "切片缓存默认值");

    /// <summary>加载缓存默认值提示</summary>
    public string LoadCachingDefaultsToolTip => T("Load caching defaults", "加载缓存默认值");

    /// <summary>默认缓存配置</summary>
    public string DefaultCacheConfiguration => T("Default Cache Configuration", "默认缓存配置");

    /// <summary>默认启用切片缓存</summary>
    public string EnableTileCachingByDefault => T("Enable Tile Caching by Default", "默认启用切片缓存");

    /// <summary>默认过期设置</summary>
    public string DefaultExpirySettings => T("Default Expiry Settings", "默认过期设置");

    /// <summary>缓存过期时间标签</summary>
    public string CacheExpiryLabel => T("Cache Expiry (seconds):", "缓存过期时间（秒）:");

    /// <summary>客户端缓存过期时间标签</summary>
    public string ClientCacheExpiryLabel => T("Client Cache Expiry (seconds):", "客户端缓存过期时间（秒）:");

    /// <summary>无过期说明</summary>
    public string NoExpiryNote => T("Set to 0 for no expiry.", "设为 0 表示不过期。");

    /// <summary>缓存注意事项</summary>
    public string CachingNote => T(
        "Note: These are global defaults. Individual layer caching settings can be configured in Layer Management.",
        "注意：这些是全局默认值。各图层的缓存设置可在图层管理中单独配置。");

    // ── DiskQuota ──────────────────────────────────────────────────────────

    /// <summary>磁盘配额标题</summary>
    public string DiskQuotaTitle => T("Disk Quota", "磁盘配额");

    /// <summary>加载磁盘配额设置提示</summary>
    public string LoadDiskQuotaToolTip => T("Load current disk quota settings", "加载当前磁盘配额设置");

    /// <summary>配额配置</summary>
    public string QuotaConfiguration => T("Quota Configuration", "配额配置");

    /// <summary>启用磁盘配额</summary>
    public string EnableDiskQuota => T("Enable Disk Quota", "启用磁盘配额");

    /// <summary>全局配额</summary>
    public string GlobalQuota => T("Global Quota", "全局配额");

    /// <summary>配额值标签</summary>
    public string QuotaValueLabel => T("Quota Value:", "配额值:");

    /// <summary>磁盘块大小标签</summary>
    public string DiskBlockSizeLabel => T("Disk Block Size (B):", "磁盘块大小 (B):");

    /// <summary>清理频率标签</summary>
    public string CleanupFrequencyLabel => T("Cleanup Frequency:", "清理频率:");

    // ── Gridsets ───────────────────────────────────────────────────────────

    /// <summary>格网集标题</summary>
    public string GridsetsTitle => T("Gridsets", "格网集");

    /// <summary>重新加载格网集列表提示</summary>
    public string ReloadGridsetsToolTip => T("Reload gridset list", "重新加载格网集列表");

    /// <summary>已有格网集</summary>
    public string ExistingGridsets => T("Existing Gridsets", "已有格网集");

    /// <summary>选中的格网集</summary>
    public string SelectedGridset => T("Selected Gridset", "已选格网集");

    /// <summary>删除格网集</summary>
    public string DeleteGridset => T("Delete Gridset", "删除格网集");

    /// <summary>删除格网集警告</summary>
    public string DeleteGridsetWarning => T("Warning: Deleting a gridset may affect layers using it.", "警告：删除格网集可能影响使用它的图层。");

    // ── SecuritySettings ───────────────────────────────────────────────────

    /// <summary>安全设置标题</summary>
    public string SecuritySettingsTitle => T("Security Settings", "安全设置");

    /// <summary>加载安全设置提示</summary>
    public string LoadSecuritySettingsToolTip => T("Load security settings", "加载安全设置");

    /// <summary>访问控制规则</summary>
    public string AccessControlRules => T("Access Control Rules", "访问控制规则");

    /// <summary>信息</summary>
    public string InformationLabel => T("Information", "信息");

    /// <summary>安全信息说明</summary>
    public string SecurityInfoText => T(
        "This page shows Access Control List (ACL) rules for GeoServer services and layers.",
        "本页显示 GeoServer 服务和图层的访问控制列表 (ACL) 规则。");

    /// <summary>安全格式说明</summary>
    public string SecurityFormatText => T(
        "Format: [resource] role => access level",
        "格式：[资源] 角色 => 访问级别");

    // ── UsersGroupsRoles ───────────────────────────────────────────────────

    /// <summary>用户组角色标题</summary>
    public string UsersGroupsRolesTitle => T("Users, Groups, and Roles", "用户、组和角色");

    /// <summary>重新加载全部提示</summary>
    public string ReloadAllToolTip => T("Reload all users, groups and roles", "重新加载所有用户、组和角色");

    /// <summary>用户选项卡</summary>
    public string UsersTab => T("Users", "用户");

    /// <summary>组选项卡</summary>
    public string GroupsTab => T("Groups", "组");

    /// <summary>角色选项卡</summary>
    public string RolesTab => T("Roles", "角色");

    /// <summary>用户列表标题</summary>
    public string UsersHeader => T("Users", "用户");

    /// <summary>组列表标题</summary>
    public string GroupsHeader => T("Groups", "组");

    /// <summary>角色列表标题</summary>
    public string RolesHeader => T("Roles", "角色");

    /// <summary>创建用户</summary>
    public string CreateUser => T("Create User", "创建用户");

    /// <summary>用户名水印</summary>
    public string UsernameHint => T("Username", "用户名");

    /// <summary>密码水印</summary>
    public string PasswordHint => T("Password", "密码");

    /// <summary>新密码水印</summary>
    public string NewPasswordHint => T("New password (leave blank to keep)", "新密码（留空则不修改）");

    /// <summary>创建用户按钮</summary>
    public string CreateUserButton => T("Create User", "创建用户");

    /// <summary>删除用户</summary>
    public string DeleteUser => T("Delete User", "删除用户");

    /// <summary>删除选中用户</summary>
    public string DeleteSelectedUser => T("Delete Selected User", "删除选中用户");

    /// <summary>编辑用户</summary>
    public string EditUser => T("Edit User", "编辑用户");

    /// <summary>已启用复选框</summary>
    public string EnabledCheckbox => T("Enabled", "已启用");

    /// <summary>更新用户</summary>
    public string UpdateUser => T("Update User", "更新用户");

    /// <summary>创建组</summary>
    public string CreateGroup => T("Create Group", "创建组");

    /// <summary>组名称水印</summary>
    public string GroupNameHint => T("Group name", "组名称");

    /// <summary>创建组按钮</summary>
    public string CreateGroupButton => T("Create Group", "创建组");

    /// <summary>删除组</summary>
    public string DeleteGroup => T("Delete Group", "删除组");

    /// <summary>删除选中组</summary>
    public string DeleteSelectedGroup => T("Delete Selected Group", "删除选中组");

    /// <summary>角色（只读）</summary>
    public string RolesReadOnly => T("Roles (Read Only)", "角色（只读）");

    /// <summary>角色只读说明</summary>
    public string RolesReadOnlyInfo => T(
        "Roles are read-only. Use the GeoServer REST API directly to create or delete roles.",
        "角色为只读。请直接使用 GeoServer REST API 创建或删除角色。");

    // ── 状态消息模板 ────────────────────────────────────────────────────────

    /// <summary>就绪</summary>
    public string StatusReady => T("Ready", "就绪");

    /// <summary>未连接</summary>
    public string StatusNotConnected => T("Not connected to GeoServer", "未连接到 GeoServer");

    /// <summary>连接中</summary>
    public string StatusConnecting => T("Connecting...", "连接中...");

    /// <summary>连接成功</summary>
    public string StatusConnectedSuccess => T("Connected successfully", "连接成功");

    /// <summary>连接失败模板 {0}=消息</summary>
    public string StatusConnectionFailed => T("Connection failed: {0}", "连接失败: {0}");

    /// <summary>已断开连接</summary>
    public string StatusDisconnected => T("Disconnected", "已断开连接");

    /// <summary>正在刷新资源</summary>
    public string StatusRefreshing => T("Refreshing resources...", "正在刷新资源...");

    /// <summary>资源已刷新</summary>
    public string StatusRefreshed => T("Resources refreshed", "资源已刷新");

    /// <summary>刷新失败模板 {0}=消息</summary>
    public string StatusRefreshFailed => T("Failed to refresh: {0}", "刷新失败: {0}");

    /// <summary>关于 GeoServer 状态</summary>
    public string StatusAbout => T("About GeoServer", "关于 GeoServer");

    /// <summary>图层预览状态</summary>
    public string StatusLayerPreview => T("Layer Preview", "图层预览");

    /// <summary>工作空间状态</summary>
    public string StatusWorkspaces => T("Workspaces", "工作空间");

    /// <summary>数据存储状态</summary>
    public string StatusDataStores => T("Data Stores", "数据存储");

    /// <summary>图层状态</summary>
    public string StatusLayers => T("Layers", "图层");

    /// <summary>图层组状态</summary>
    public string StatusLayerGroups => T("Layer Groups", "图层组");

    /// <summary>样式状态</summary>
    public string StatusStyles => T("Styles", "样式");

    /// <summary>WMS 设置状态</summary>
    public string StatusWmsSettings => T("WMS Settings", "WMS 设置");

    /// <summary>WFS 设置状态</summary>
    public string StatusWfsSettings => T("WFS Settings", "WFS 设置");

    /// <summary>WCS 设置状态</summary>
    public string StatusWcsSettings => T("WCS Settings", "WCS 设置");

    /// <summary>全局设置状态</summary>
    public string StatusGlobalSettings => T("Global Settings", "全局设置");

    /// <summary>日志设置状态</summary>
    public string StatusLoggingSettings => T("Logging Settings", "日志设置");

    /// <summary>切片缓存默认值状态</summary>
    public string StatusTileCachingDefaults => T("Tile Caching Defaults", "切片缓存默认值");

    /// <summary>格网集状态</summary>
    public string StatusGridsets => T("Gridsets", "格网集");

    /// <summary>磁盘配额状态</summary>
    public string StatusDiskQuota => T("Disk Quota", "磁盘配额");

    /// <summary>安全设置状态</summary>
    public string StatusSecuritySettings => T("Security Settings", "安全设置");

    /// <summary>用户组角色状态</summary>
    public string StatusUsersGroupsAndRoles => T("Users, Groups, and Roles", "用户、组和角色");

    /// <summary>请先连接到 GeoServer</summary>
    public string StatusPleaseConnect => T("Please connect to GeoServer first", "请先连接到 GeoServer");

    /// <summary>正在加载工作空间</summary>
    public string StatusLoadingWorkspaces => T("Loading workspaces...", "正在加载工作空间...");

    /// <summary>已加载工作空间模板 {0}=数量</summary>
    public string StatusWorkspacesLoaded => T("Loaded {0} workspaces", "已加载 {0} 个工作空间");

    /// <summary>工作空间名称必填</summary>
    public string StatusWorkspaceNameRequired => T("Workspace name is required", "工作空间名称不能为空");

    /// <summary>正在创建工作空间模板 {0}=名称</summary>
    public string StatusCreatingWorkspace => T("Creating workspace '{0}'...", "正在创建工作空间 '{0}'...");

    /// <summary>工作空间创建成功模板 {0}=名称</summary>
    public string StatusWorkspaceCreated => T("Workspace '{0}' created successfully", "工作空间 '{0}' 创建成功");

    /// <summary>工作空间创建失败模板 {0}=消息</summary>
    public string StatusWorkspaceCreateFailed => T("Failed to create workspace: {0}", "创建工作空间失败: {0}");

    /// <summary>未选中工作空间</summary>
    public string StatusNoWorkspaceSelected => T("No workspace selected", "未选择工作空间");

    /// <summary>正在删除工作空间模板 {0}=名称</summary>
    public string StatusDeletingWorkspace => T("Deleting workspace '{0}'...", "正在删除工作空间 '{0}'...");

    /// <summary>工作空间删除成功模板 {0}=名称</summary>
    public string StatusWorkspaceDeleted => T("Workspace '{0}' deleted successfully", "工作空间 '{0}' 删除成功");

    /// <summary>工作空间删除失败模板 {0}=消息</summary>
    public string StatusWorkspaceDeleteFailed => T("Failed to delete workspace: {0}", "删除工作空间失败: {0}");

    /// <summary>加载工作空间失败模板 {0}=消息</summary>
    public string StatusWorkspacesLoadFailed => T("Failed to load workspaces: {0}", "加载工作空间失败: {0}");

    /// <summary>正在加载数据存储</summary>
    public string StatusLoadingDataStores => T("Loading data stores...", "正在加载数据存储...");

    /// <summary>已加载数据存储模板 {0}=数量, {1}=工作空间名</summary>
    public string StatusDataStoresLoaded => T("Loaded {0} data stores for workspace '{1}'", "已加载工作空间 '{1}' 的 {0} 个数据存储");

    /// <summary>未选中数据存储</summary>
    public string StatusNoDataStoreSelected => T("No data store selected", "未选择数据存储");

    /// <summary>正在删除数据存储模板 {0}=名称</summary>
    public string StatusDeletingDataStore => T("Deleting data store '{0}'...", "正在删除数据存储 '{0}'...");

    /// <summary>数据存储删除成功模板 {0}=名称</summary>
    public string StatusDataStoreDeleted => T("Data store '{0}' deleted successfully", "数据存储 '{0}' 删除成功");

    /// <summary>数据存储删除失败模板 {0}=消息</summary>
    public string StatusDataStoreDeleteFailed => T("Failed to delete data store: {0}", "删除数据存储失败: {0}");

    /// <summary>数据存储名称必填</summary>
    public string StatusDataStoreNameRequired => T("Data store name is required", "数据存储名称不能为空");

    /// <summary>请先选择工作空间</summary>
    public string StatusPleaseSelectWorkspace => T("Please select a workspace first", "请先选择工作空间");

    /// <summary>正在创建数据存储模板 {0}=名称</summary>
    public string StatusCreatingDataStore => T("Creating data store '{0}'...", "正在创建数据存储 '{0}'...");

    /// <summary>数据存储创建成功</summary>
    public string StatusDataStoreCreated => T("Data store created successfully", "数据存储创建成功");

    /// <summary>数据存储创建失败模板 {0}=消息</summary>
    public string StatusDataStoreCreateFailed => T("Failed to create data store: {0}", "创建数据存储失败: {0}");

    /// <summary>加载数据存储失败模板 {0}=消息</summary>
    public string StatusDataStoresLoadFailed => T("Failed to load data stores: {0}", "加载数据存储失败: {0}");

    /// <summary>正在加载图层</summary>
    public string StatusLoadingLayers => T("Loading layers...", "正在加载图层...");

    /// <summary>已加载图层模板 {0}=数量</summary>
    public string StatusLayersLoaded => T("Loaded {0} layers", "已加载 {0} 个图层");

    /// <summary>未选中图层</summary>
    public string StatusNoLayerSelected => T("No layer selected", "未选择图层");

    /// <summary>正在删除图层模板 {0}=名称</summary>
    public string StatusDeletingLayer => T("Deleting layer '{0}'...", "正在删除图层 '{0}'...");

    /// <summary>图层删除成功模板 {0}=名称</summary>
    public string StatusLayerDeleted => T("Layer '{0}' deleted successfully", "图层 '{0}' 删除成功");

    /// <summary>图层删除失败模板 {0}=消息</summary>
    public string StatusLayerDeleteFailed => T("Failed to delete layer: {0}", "删除图层失败: {0}");

    /// <summary>图层名称必填</summary>
    public string StatusLayerNameRequired => T("Layer name is required", "图层名称不能为空");

    /// <summary>请选择数据存储</summary>
    public string StatusPleaseSelectDataStore => T("Please select a data store", "请选择数据存储");

    /// <summary>请先选择特定工作空间</summary>
    public string StatusPleaseSelectSpecificWorkspace => T("Please select a specific workspace first", "请先选择特定工作空间");

    /// <summary>正在发布图层模板 {0}=名称</summary>
    public string StatusPublishingLayer => T("Publishing layer '{0}'...", "正在发布图层 '{0}'...");

    /// <summary>图层发布成功</summary>
    public string StatusLayerPublished => T("Layer published successfully", "图层发布成功");

    /// <summary>图层发布失败模板 {0}=消息</summary>
    public string StatusLayerPublishFailed => T("Failed to publish layer: {0}", "发布图层失败: {0}");

    /// <summary>加载图层失败模板 {0}=消息</summary>
    public string StatusLayersLoadFailed => T("Failed to load layers: {0}", "加载图层失败: {0}");

    /// <summary>正在加载图层组</summary>
    public string StatusLoadingLayerGroups => T("Loading layer groups...", "正在加载图层组...");

    /// <summary>已加载图层组模板 {0}=数量</summary>
    public string StatusLayerGroupsLoaded => T("Loaded {0} layer groups", "已加载 {0} 个图层组");

    /// <summary>图层组名称必填</summary>
    public string StatusLayerGroupNameRequired => T("Layer group name is required", "图层组名称不能为空");

    /// <summary>正在创建图层组模板 {0}=名称</summary>
    public string StatusCreatingLayerGroup => T("Creating layer group '{0}'...", "正在创建图层组 '{0}'...");

    /// <summary>图层组创建成功模板 {0}=名称</summary>
    public string StatusLayerGroupCreated => T("Layer group '{0}' created successfully", "图层组 '{0}' 创建成功");

    /// <summary>图层组创建失败模板 {0}=消息</summary>
    public string StatusLayerGroupCreateFailed => T("Failed to create layer group: {0}", "创建图层组失败: {0}");

    /// <summary>未选中图层组</summary>
    public string StatusNoLayerGroupSelected => T("No layer group selected", "未选择图层组");

    /// <summary>正在删除图层组模板 {0}=名称</summary>
    public string StatusDeletingLayerGroup => T("Deleting layer group '{0}'...", "正在删除图层组 '{0}'...");

    /// <summary>图层组删除成功模板 {0}=名称</summary>
    public string StatusLayerGroupDeleted => T("Layer group '{0}' deleted successfully", "图层组 '{0}' 删除成功");

    /// <summary>图层组删除失败模板 {0}=消息</summary>
    public string StatusLayerGroupDeleteFailed => T("Failed to delete layer group: {0}", "删除图层组失败: {0}");

    /// <summary>加载图层组失败模板 {0}=消息</summary>
    public string StatusLayerGroupsLoadFailed => T("Failed to load layer groups: {0}", "加载图层组失败: {0}");

    /// <summary>正在加载系统信息</summary>
    public string StatusLoadingSystemInfo => T("Loading system information...", "正在加载系统信息...");

    /// <summary>系统信息加载成功</summary>
    public string StatusSystemInfoLoaded => T("System information loaded successfully", "系统信息加载成功");

    /// <summary>加载系统信息失败模板 {0}=消息</summary>
    public string StatusSystemInfoLoadFailed => T("Failed to load system information: {0}", "加载系统信息失败: {0}");

    /// <summary>正在加载 WMS 设置</summary>
    public string StatusLoadingWmsSettings => T("Loading WMS settings...", "正在加载 WMS 设置...");

    /// <summary>WMS 设置已加载</summary>
    public string StatusWmsSettingsLoaded => T("WMS settings loaded", "WMS 设置已加载");

    /// <summary>加载 WMS 设置失败模板 {0}=消息</summary>
    public string StatusWmsSettingsLoadFailed => T("Failed to load WMS settings: {0}", "加载 WMS 设置失败: {0}");

    /// <summary>正在保存 WMS 设置</summary>
    public string StatusSavingWmsSettings => T("Saving WMS settings...", "正在保存 WMS 设置...");

    /// <summary>WMS 设置保存成功</summary>
    public string StatusWmsSettingsSaved => T("WMS settings saved successfully", "WMS 设置保存成功");

    /// <summary>保存 WMS 设置失败模板 {0}=消息</summary>
    public string StatusWmsSettingsSaveFailed => T("Failed to save WMS settings: {0}", "保存 WMS 设置失败: {0}");

    /// <summary>正在加载 WFS 设置</summary>
    public string StatusLoadingWfsSettings => T("Loading WFS settings...", "正在加载 WFS 设置...");

    /// <summary>WFS 设置已加载</summary>
    public string StatusWfsSettingsLoaded => T("WFS settings loaded", "WFS 设置已加载");

    /// <summary>加载 WFS 设置失败模板 {0}=消息</summary>
    public string StatusWfsSettingsLoadFailed => T("Failed to load WFS settings: {0}", "加载 WFS 设置失败: {0}");

    /// <summary>正在保存 WFS 设置</summary>
    public string StatusSavingWfsSettings => T("Saving WFS settings...", "正在保存 WFS 设置...");

    /// <summary>WFS 设置保存成功</summary>
    public string StatusWfsSettingsSaved => T("WFS settings saved successfully", "WFS 设置保存成功");

    /// <summary>保存 WFS 设置失败模板 {0}=消息</summary>
    public string StatusWfsSettingsSaveFailed => T("Failed to save WFS settings: {0}", "保存 WFS 设置失败: {0}");

    /// <summary>正在加载 WCS 设置</summary>
    public string StatusLoadingWcsSettings => T("Loading WCS settings...", "正在加载 WCS 设置...");

    /// <summary>WCS 设置已加载</summary>
    public string StatusWcsSettingsLoaded => T("WCS settings loaded", "WCS 设置已加载");

    /// <summary>加载 WCS 设置失败模板 {0}=消息</summary>
    public string StatusWcsSettingsLoadFailed => T("Failed to load WCS settings: {0}", "加载 WCS 设置失败: {0}");

    /// <summary>正在保存 WCS 设置</summary>
    public string StatusSavingWcsSettings => T("Saving WCS settings...", "正在保存 WCS 设置...");

    /// <summary>WCS 设置保存成功</summary>
    public string StatusWcsSettingsSaved => T("WCS settings saved successfully", "WCS 设置保存成功");

    /// <summary>保存 WCS 设置失败模板 {0}=消息</summary>
    public string StatusWcsSettingsSaveFailed => T("Failed to save WCS settings: {0}", "保存 WCS 设置失败: {0}");

    /// <summary>正在加载全局设置</summary>
    public string StatusLoadingGlobalSettings => T("Loading global settings...", "正在加载全局设置...");

    /// <summary>全局设置已加载</summary>
    public string StatusGlobalSettingsLoaded => T("Global settings loaded", "全局设置已加载");

    /// <summary>加载全局设置失败模板 {0}=消息</summary>
    public string StatusGlobalSettingsLoadFailed => T("Failed to load settings: {0}", "加载设置失败: {0}");

    /// <summary>正在保存全局设置</summary>
    public string StatusSavingGlobalSettings => T("Saving global settings...", "正在保存全局设置...");

    /// <summary>全局设置保存成功</summary>
    public string StatusGlobalSettingsSaved => T("Global settings saved successfully", "全局设置保存成功");

    /// <summary>保存全局设置失败模板 {0}=消息</summary>
    public string StatusGlobalSettingsSaveFailed => T("Failed to save settings: {0}", "保存设置失败: {0}");

    /// <summary>正在加载日志设置</summary>
    public string StatusLoadingLoggingSettings => T("Loading logging settings...", "正在加载日志设置...");

    /// <summary>日志设置已加载</summary>
    public string StatusLoggingSettingsLoaded => T("Logging settings loaded", "日志设置已加载");

    /// <summary>加载日志设置失败模板 {0}=消息</summary>
    public string StatusLoggingSettingsLoadFailed => T("Failed to load logging settings: {0}", "加载日志设置失败: {0}");

    /// <summary>正在保存日志设置</summary>
    public string StatusSavingLoggingSettings => T("Saving logging settings...", "正在保存日志设置...");

    /// <summary>日志设置保存成功</summary>
    public string StatusLoggingSettingsSaved => T("Logging settings saved successfully", "日志设置保存成功");

    /// <summary>保存日志设置失败模板 {0}=消息</summary>
    public string StatusLoggingSettingsSaveFailed => T("Failed to save logging settings: {0}", "保存日志设置失败: {0}");

    /// <summary>正在加载缓存默认值</summary>
    public string StatusLoadingCachingDefaults => T("Loading caching defaults...", "正在加载缓存默认值...");

    /// <summary>缓存默认值已加载</summary>
    public string StatusCachingDefaultsLoaded => T(
        "Caching defaults loaded (configure per-layer settings in Layer Management)",
        "缓存默认值已加载（各图层设置可在图层管理中配置）");

    /// <summary>加载缓存默认值失败模板 {0}=消息</summary>
    public string StatusCachingDefaultsLoadFailed => T("Failed to load caching defaults: {0}", "加载缓存默认值失败: {0}");

    /// <summary>正在保存缓存默认值</summary>
    public string StatusSavingCachingDefaults => T("Saving caching defaults...", "正在保存缓存默认值...");

    /// <summary>缓存默认值保存成功</summary>
    public string StatusCachingDefaultsSaved => T("Caching defaults saved successfully", "缓存默认值保存成功");

    /// <summary>保存缓存默认值失败模板 {0}=消息</summary>
    public string StatusCachingDefaultsSaveFailed => T("Failed to save caching defaults: {0}", "保存缓存默认值失败: {0}");

    /// <summary>正在加载磁盘配额设置</summary>
    public string StatusLoadingDiskQuota => T("Loading disk quota settings...", "正在加载磁盘配额设置...");

    /// <summary>磁盘配额设置已加载</summary>
    public string StatusDiskQuotaLoaded => T("Disk quota settings loaded", "磁盘配额设置已加载");

    /// <summary>加载磁盘配额设置失败模板 {0}=消息</summary>
    public string StatusDiskQuotaLoadFailed => T("Failed to load disk quota settings: {0}", "加载磁盘配额设置失败: {0}");

    /// <summary>正在保存磁盘配额设置</summary>
    public string StatusSavingDiskQuota => T("Saving disk quota settings...", "正在保存磁盘配额设置...");

    /// <summary>磁盘配额设置保存成功</summary>
    public string StatusDiskQuotaSaved => T("Disk quota settings saved successfully", "磁盘配额设置保存成功");

    /// <summary>保存磁盘配额设置失败模板 {0}=消息</summary>
    public string StatusDiskQuotaSaveFailed => T("Failed to save disk quota settings: {0}", "保存磁盘配额设置失败: {0}");

    /// <summary>正在加载格网集</summary>
    public string StatusLoadingGridsets => T("Loading gridsets...", "正在加载格网集...");

    /// <summary>已加载格网集模板 {0}=数量</summary>
    public string StatusGridsetsLoaded => T("Loaded {0} gridsets", "已加载 {0} 个格网集");

    /// <summary>加载格网集失败模板 {0}=消息</summary>
    public string StatusGridsetsLoadFailed => T("Failed to load gridsets: {0}", "加载格网集失败: {0}");

    /// <summary>未选中格网集</summary>
    public string StatusNoGridsetSelected => T("No gridset selected", "未选择格网集");

    /// <summary>正在删除格网集模板 {0}=名称</summary>
    public string StatusDeletingGridset => T("Deleting gridset '{0}'...", "正在删除格网集 '{0}'...");

    /// <summary>格网集删除成功模板 {0}=名称</summary>
    public string StatusGridsetDeleted => T("Gridset '{0}' deleted", "格网集 '{0}' 已删除");

    /// <summary>格网集删除失败模板 {0}=消息</summary>
    public string StatusGridsetDeleteFailed => T("Failed to delete gridset: {0}", "删除格网集失败: {0}");

    /// <summary>正在加载安全设置</summary>
    public string StatusLoadingSecuritySettings => T("Loading security settings...", "正在加载安全设置...");

    /// <summary>安全设置已加载</summary>
    public string StatusSecuritySettingsLoaded => T("Security settings loaded", "安全设置已加载");

    /// <summary>加载安全设置失败模板 {0}=消息</summary>
    public string StatusSecuritySettingsLoadFailed => T("Failed to load security settings: {0}", "加载安全设置失败: {0}");

    /// <summary>正在加载样式</summary>
    public string StatusLoadingStyles => T("Loading styles...", "正在加载样式...");

    /// <summary>已加载样式模板 {0}=数量</summary>
    public string StatusStylesLoaded => T("Loaded {0} styles", "已加载 {0} 个样式");

    /// <summary>加载样式失败模板 {0}=消息</summary>
    public string StatusStylesLoadFailed => T("Failed to load styles: {0}", "加载样式失败: {0}");

    /// <summary>未选中样式</summary>
    public string StatusNoStyleSelected => T("No style selected", "未选择样式");

    /// <summary>正在加载样式内容</summary>
    public string StatusLoadingStyleContent => T("Loading style content...", "正在加载样式内容...");

    /// <summary>样式内容已加载</summary>
    public string StatusStyleContentLoaded => T("Style content loaded", "样式内容已加载");

    /// <summary>加载样式内容失败模板 {0}=消息</summary>
    public string StatusStyleContentLoadFailed => T("Failed to load style content: {0}", "加载样式内容失败: {0}");

    /// <summary>正在上传样式</summary>
    public string StatusUploadingStyle => T("Uploading style...", "正在上传样式...");

    /// <summary>样式上传成功</summary>
    public string StatusStyleUploaded => T("Style uploaded successfully", "样式上传成功");

    /// <summary>上传样式失败模板 {0}=消息</summary>
    public string StatusStyleUploadFailed => T("Failed to upload style: {0}", "上传样式失败: {0}");

    /// <summary>正在删除样式</summary>
    public string StatusDeletingStyle => T("Deleting style...", "正在删除样式...");

    /// <summary>样式删除成功</summary>
    public string StatusStyleDeleted => T("Style deleted successfully", "样式删除成功");

    /// <summary>样式删除失败模板 {0}=消息</summary>
    public string StatusStyleDeleteFailed => T("Failed to delete style: {0}", "删除样式失败: {0}");

    /// <summary>图层预览就绪模板 {0}=工作空间:图层</summary>
    public string StatusPreviewReady => T("Preview ready for {0}", "图层 {0} 预览已就绪");

    /// <summary>图层预览失败模板 {0}=消息</summary>
    public string StatusPreviewFailed => T("Failed to preview layer: {0}", "图层预览失败: {0}");

    /// <summary>加载资源失败模板 {0}=消息</summary>
    public string StatusResourcesLoadFailed => T("Failed to load resources: {0}", "加载资源失败: {0}");

    /// <summary>加载用户失败模板 {0}=消息</summary>
    public string StatusUsersLoadFailed => T("Failed to load users: {0}", "加载用户失败: {0}");

    /// <summary>加载组失败模板 {0}=消息</summary>
    public string StatusGroupsLoadFailed => T("Failed to load groups: {0}", "加载组失败: {0}");

    /// <summary>加载角色失败模板 {0}=消息</summary>
    public string StatusRolesLoadFailed => T("Failed to load roles: {0}", "加载角色失败: {0}");

    /// <summary>正在加载用户名密码等</summary>
    public string StatusLoadingAll => T("Loading users, groups and roles...", "正在加载用户、组和角色...");

    /// <summary>创建用户需要用户名和密码</summary>
    public string StatusUsernamePasswordRequired => T("Username and password are required", "用户名和密码不能为空");

    /// <summary>正在创建用户模板 {0}=名称</summary>
    public string StatusCreatingUser => T("Creating user '{0}'...", "正在创建用户 '{0}'...");

    /// <summary>用户创建成功模板 {0}=名称</summary>
    public string StatusUserCreated => T("User '{0}' created successfully", "用户 '{0}' 创建成功");

    /// <summary>用户创建失败模板 {0}=消息</summary>
    public string StatusUserCreateFailed => T("Failed to create user: {0}", "创建用户失败: {0}");

    /// <summary>未选中用户</summary>
    public string StatusNoUserSelected => T("No user selected", "未选择用户");

    /// <summary>正在删除用户模板 {0}=名称</summary>
    public string StatusDeletingUser => T("Deleting user '{0}'...", "正在删除用户 '{0}'...");

    /// <summary>用户删除成功模板 {0}=名称</summary>
    public string StatusUserDeleted => T("User '{0}' deleted successfully", "用户 '{0}' 删除成功");

    /// <summary>用户删除失败模板 {0}=消息</summary>
    public string StatusUserDeleteFailed => T("Failed to delete user: {0}", "删除用户失败: {0}");

    /// <summary>正在更新用户模板 {0}=名称</summary>
    public string StatusUpdatingUser => T("Updating user '{0}'...", "正在更新用户 '{0}'...");

    /// <summary>用户更新成功模板 {0}=名称</summary>
    public string StatusUserUpdated => T("User '{0}' updated successfully", "用户 '{0}' 更新成功");

    /// <summary>用户更新失败模板 {0}=消息</summary>
    public string StatusUserUpdateFailed => T("Failed to update user: {0}", "更新用户失败: {0}");

    /// <summary>组名称必填</summary>
    public string StatusGroupNameRequired => T("Group name is required", "组名称不能为空");

    /// <summary>正在创建组模板 {0}=名称</summary>
    public string StatusCreatingGroup => T("Creating group '{0}'...", "正在创建组 '{0}'...");

    /// <summary>组创建成功模板 {0}=名称</summary>
    public string StatusGroupCreated => T("Group '{0}' created successfully", "组 '{0}' 创建成功");

    /// <summary>组创建失败模板 {0}=消息</summary>
    public string StatusGroupCreateFailed => T("Failed to create group: {0}", "创建组失败: {0}");

    /// <summary>未选中组</summary>
    public string StatusNoGroupSelected => T("No group selected", "未选择组");

    /// <summary>正在删除组模板 {0}=名称</summary>
    public string StatusDeletingGroup => T("Deleting group '{0}'...", "正在删除组 '{0}'...");

    /// <summary>组删除成功模板 {0}=名称</summary>
    public string StatusGroupDeleted => T("Group '{0}' deleted successfully", "组 '{0}' 删除成功");

    /// <summary>组删除失败模板 {0}=消息</summary>
    public string StatusGroupDeleteFailed => T("Failed to delete group: {0}", "删除组失败: {0}");
}
