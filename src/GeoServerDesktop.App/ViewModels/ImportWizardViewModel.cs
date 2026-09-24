using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoServerDesktop.App.Services;
using GeoServerDesktop.GeoServerClient.Import;

namespace GeoServerDesktop.App.ViewModels
{
    /// <summary>
    /// 数据导入向导视图模型（M2）：三步状态机——选择数据源 → 目标与参数 → 发布。
    /// 发布走 ImportWizardService（服务器侧 file: 引用 / PostGIS 连接参数）；
    /// 本地路径在 App 本机可访问时经 GeoFileInspector 提供文件头预检（尽力而为，失败仅提示不阻断）。
    /// </summary>
    public partial class ImportWizardViewModel : ViewModelBase
    {
        private readonly IGeoServerConnectionService _connectionService;

        // ── 状态机 ──

        /// <summary>当前步骤（1..3）</summary>
        [ObservableProperty]
        private int _currentStep = 1;

        /// <summary>是否正在执行异步操作</summary>
        [ObservableProperty]
        private bool _isBusy;

        /// <summary>状态消息</summary>
        [ObservableProperty]
        private string _statusMessage = string.Empty;

        // ── 第 1 步：数据源 ──

        /// <summary>数据源类型下标（0=Shapefile 目录，1=GeoTIFF 文件，2=PostGIS 表）</summary>
        [ObservableProperty]
        private int _sourceKindIndex;

        /// <summary>服务器侧文件引用（file:...）</summary>
        [ObservableProperty]
        private string _fileRef = "file:";

        /// <summary>原始名称（磁盘文件基名 / 数据库表名）</summary>
        [ObservableProperty]
        private string _nativeName = string.Empty;

        /// <summary>PostGIS 主机</summary>
        [ObservableProperty]
        private string _pgHost = "localhost";

        /// <summary>PostGIS 端口</summary>
        [ObservableProperty]
        private string _pgPort = "5432";

        /// <summary>PostGIS 数据库</summary>
        [ObservableProperty]
        private string _pgDatabase = "postgres";

        /// <summary>PostGIS 用户</summary>
        [ObservableProperty]
        private string _pgUser = "postgres";

        /// <summary>PostGIS 密码</summary>
        [ObservableProperty]
        private string _pgPassword = string.Empty;

        /// <summary>连接探测结果消息</summary>
        [ObservableProperty]
        private string _probeMessage = string.Empty;

        // ── 第 2 步：目标与参数 ──

        /// <summary>可用工作空间列表</summary>
        [ObservableProperty]
        private ObservableCollection<string> _workspaces = new();

        /// <summary>选中的工作空间</summary>
        [ObservableProperty]
        private string? _selectedWorkspace;

        /// <summary>发布名（全局唯一）</summary>
        [ObservableProperty]
        private string _publishName = string.Empty;

        /// <summary>存储名（默认同发布名）</summary>
        [ObservableProperty]
        private string _storeName = string.Empty;

        /// <summary>坐标参考系（可选，留空由 GeoServer 从数据推断）</summary>
        [ObservableProperty]
        private string _srs = string.Empty;

        /// <summary>本地预检路径（可选；App 本机可访问的文件/目录）</summary>
        [ObservableProperty]
        private string _localPreviewPath = string.Empty;

        /// <summary>本地预检结果文本</summary>
        [ObservableProperty]
        private string _previewText = string.Empty;

        // ── 第 3 步：发布 ──

        /// <summary>发布结果消息</summary>
        [ObservableProperty]
        private string _resultMessage = string.Empty;

        /// <summary>发布是否成功</summary>
        [ObservableProperty]
        private bool _publishSucceeded;

        /// <summary>
        /// 初始化 ImportWizardViewModel 类的新实例
        /// </summary>
        /// <param name="connectionService">GeoServer 连接服务</param>
        public ImportWizardViewModel(IGeoServerConnectionService connectionService)
        {
            _connectionService = connectionService;
        }

        /// <summary>是否处于第 1 步</summary>
        public bool IsStep1 => CurrentStep == 1;

        /// <summary>是否处于第 2 步</summary>
        public bool IsStep2 => CurrentStep == 2;

        /// <summary>是否处于第 3 步</summary>
        public bool IsStep3 => CurrentStep == 3;

        /// <summary>数据源是否为 PostGIS</summary>
        public bool IsPostgisSource => SourceKindIndex == 2;

        /// <summary>数据源是否为文件引用（Shapefile/GeoTIFF）</summary>
        public bool IsFileSource => SourceKindIndex != 2;

        /// <summary>数据源类型显示名集合（随语言切换动态生成）</summary>
        public ObservableCollection<string> SourceKinds => new ObservableCollection<string>
        {
            L.WizardKindShapefile,
            L.WizardKindGeoTiff,
            L.WizardKindPostgis,
        };

        /// <summary>步骤指示文本</summary>
        public string StepIndicator => string.Format(L.WizardStepIndicator, CurrentStep);

        /// <summary>当前步骤标题</summary>
        public string StepTitle => CurrentStep == 1
            ? L.WizardStep1Title
            : CurrentStep == 2 ? L.WizardStep2Title : L.WizardStep3Title;

        partial void OnCurrentStepChanged(int value)
        {
            OnPropertyChanged(nameof(IsStep1));
            OnPropertyChanged(nameof(IsStep2));
            OnPropertyChanged(nameof(IsStep3));
            OnPropertyChanged(nameof(StepIndicator));
            OnPropertyChanged(nameof(StepTitle));
        }

        partial void OnSourceKindIndexChanged(int value)
        {
            OnPropertyChanged(nameof(IsPostgisSource));
            OnPropertyChanged(nameof(IsFileSource));
            ProbeMessage = string.Empty;
        }

        /// <summary>
        /// 进入下一步（校验当前步输入）
        /// </summary>
        [RelayCommand]
        private async Task NextStepAsync()
        {
            if (!_connectionService.IsConnected)
            {
                StatusMessage = L.StatusNotConnected;
                return;
            }
            if (CurrentStep == 1)
            {
                if (IsPostgisSource)
                {
                    if (string.IsNullOrWhiteSpace(PgHost) || string.IsNullOrWhiteSpace(PgDatabase) || string.IsNullOrWhiteSpace(PgUser))
                    {
                        StatusMessage = L.WizardStatusNeedPgParams;
                        return;
                    }
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(FileRef) || !FileRef.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
                    {
                        StatusMessage = L.WizardStatusNeedFileRef;
                        return;
                    }
                    if (string.IsNullOrWhiteSpace(NativeName))
                    {
                        StatusMessage = L.WizardStatusNeedNativeName;
                        return;
                    }
                }

                if (string.IsNullOrWhiteSpace(PublishName)) PublishName = NativeName;
                if (string.IsNullOrWhiteSpace(StoreName)) StoreName = PublishName;
                StatusMessage = string.Empty;
                CurrentStep = 2;
                await LoadWorkspacesAsync();
            }
            else if (CurrentStep == 2)
            {
                if (string.IsNullOrWhiteSpace(SelectedWorkspace))
                {
                    StatusMessage = L.WizardStatusNeedWorkspace;
                    return;
                }
                if (string.IsNullOrWhiteSpace(PublishName))
                {
                    StatusMessage = L.WizardStatusNeedPublishName;
                    return;
                }
                StatusMessage = string.Empty;
                ResultMessage = string.Empty;
                PublishSucceeded = false;
                CurrentStep = 3;
            }
        }

        /// <summary>
        /// 返回上一步
        /// </summary>
        [RelayCommand]
        private void BackStep()
        {
            if (CurrentStep > 1)
            {
                StatusMessage = string.Empty;
                CurrentStep--;
            }
        }

        /// <summary>
        /// 加载工作空间列表
        /// </summary>
        [RelayCommand]
        private async Task LoadWorkspacesAsync()
        {
            if (!_connectionService.IsConnected)
            {
                StatusMessage = L.StatusNotConnected;
                return;
            }
            try
            {
                IsBusy = true;
                var list = await _connectionService.GetWorkspaceService().GetWorkspacesAsync();
                Workspaces.Clear();
                foreach (var w in list)
                {
                    Workspaces.Add(w.Name);
                }
                if (Workspaces.Count > 0 && string.IsNullOrWhiteSpace(SelectedWorkspace))
                {
                    SelectedWorkspace = Workspaces[0];
                }
                StatusMessage = string.Format(L.StatusWorkspacesLoaded, Workspaces.Count);
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 测试 PostGIS 连接（经 GeoServer 试连；需已选工作空间）
        /// </summary>
        [RelayCommand]
        private async Task ProbePostgisAsync()
        {
            if (!_connectionService.IsConnected)
            {
                StatusMessage = L.StatusNotConnected;
                return;
            }
            if (string.IsNullOrWhiteSpace(SelectedWorkspace))
            {
                ProbeMessage = L.WizardStatusNeedWorkspace;
                return;
            }
            try
            {
                IsBusy = true;
                var wiz = _connectionService.GetImportWizardService();
                var probe = await wiz.ProbePostgisConnectionAsync(SelectedWorkspace!, BuildPostgisParameters());
                ProbeMessage = probe.Success ? L.WizardProbeOk : string.Format(L.WizardProbeFailed, probe.Message);
            }
            catch (Exception ex)
            {
                ProbeMessage = string.Format(L.WizardProbeFailed, ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 解析本地路径做文件头预检（尽力而为：仅当路径在 App 本机可访问）
        /// </summary>
        [RelayCommand]
        private void InspectLocalPath()
        {
            if (string.IsNullOrWhiteSpace(LocalPreviewPath))
            {
                PreviewText = L.WizardStatusNeedLocalPath;
                return;
            }
            try
            {
                if (Directory.Exists(LocalPreviewPath))
                {
                    var sb = new StringBuilder();
                    foreach (var entry in GeoFileInspector.BrowseDirectory(LocalPreviewPath))
                    {
                        sb.AppendLine(string.Format(L.WizardPreviewDirEntry, entry.Name, KindText(entry.Kind), entry.SizeBytes));
                    }
                    PreviewText = sb.ToString().TrimEnd();
                }
                else if (File.Exists(LocalPreviewPath))
                {
                    var kind = GeoFileInspector.DetectKind(LocalPreviewPath);
                    if (kind == ImportDataSourceKind.ShapefileFile)
                    {
                        var p = GeoFileInspector.InspectShapefile(LocalPreviewPath);
                        PreviewText = string.Format(L.WizardPreviewShapefile, p.RecordCount, p.Fields.Count, p.ShapeTypeName);
                    }
                    else if (kind == ImportDataSourceKind.GeoTiffFile)
                    {
                        var p = GeoFileInspector.InspectGeoTiff(LocalPreviewPath);
                        PreviewText = string.Format(L.WizardPreviewGeoTiff, p.Width, p.Height, p.EpsgCode);
                    }
                    else
                    {
                        PreviewText = L.WizardPreviewUnknown;
                    }
                }
                else
                {
                    PreviewText = L.WizardPreviewNotFound;
                }
            }
            catch (Exception ex)
            {
                PreviewText = ex.Message;
            }
        }

        /// <summary>
        /// 执行发布（按当前数据源类型分发到向导服务）
        /// </summary>
        [RelayCommand]
        private async Task PublishAsync()
        {
            if (!_connectionService.IsConnected)
            {
                StatusMessage = L.StatusNotConnected;
                return;
            }
            if (string.IsNullOrWhiteSpace(SelectedWorkspace) || string.IsNullOrWhiteSpace(PublishName))
            {
                StatusMessage = L.WizardStatusNeedWorkspace;
                return;
            }
            try
            {
                IsBusy = true;
                var wiz = _connectionService.GetImportWizardService();
                var result = await wiz.PublishAsync(BuildRequest());
                PublishSucceeded = result.Success;
                ResultMessage = result.Success
                    ? string.Format(L.WizardPublishSuccess, result.QualifiedName)
                    : string.Format(L.WizardPublishFailed, result.Message);
                StatusMessage = ResultMessage;
            }
            catch (Exception ex)
            {
                PublishSucceeded = false;
                ResultMessage = string.Format(L.WizardPublishFailed, ex.Message);
                StatusMessage = ResultMessage;
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 重新开始（回到第 1 步并清空结果）
        /// </summary>
        [RelayCommand]
        private void Restart()
        {
            CurrentStep = 1;
            StatusMessage = string.Empty;
            ResultMessage = string.Empty;
            PublishSucceeded = false;
            PreviewText = string.Empty;
            ProbeMessage = string.Empty;
        }

        // ── 内部辅助 ──

        private PostgisConnectionParameters BuildPostgisParameters()
        {
            int port;
            if (!int.TryParse(PgPort, out port))
            {
                port = 5432;
            }
            return new PostgisConnectionParameters
            {
                Host = PgHost,
                Port = port,
                Database = PgDatabase,
                User = PgUser,
                Password = PgPassword,
            };
        }

        private ImportSourceRequest BuildRequest()
        {
            var req = new ImportSourceRequest
            {
                Workspace = SelectedWorkspace!,
                LayerName = PublishName,
                NativeName = string.IsNullOrWhiteSpace(NativeName) ? PublishName : NativeName,
                StoreName = string.IsNullOrWhiteSpace(StoreName) ? PublishName : StoreName,
                Srs = string.IsNullOrWhiteSpace(Srs) ? null : Srs,
            };
            if (IsPostgisSource)
            {
                req.Kind = ImportDataSourceKind.Postgis;
                req.Postgis = BuildPostgisParameters();
            }
            else if (SourceKindIndex == 1)
            {
                req.Kind = ImportDataSourceKind.GeoTiffFile;
                req.FileRef = FileRef;
            }
            else
            {
                req.Kind = ImportDataSourceKind.ShapefileDirectory;
                req.FileRef = FileRef;
            }
            return req;
        }

        private string KindText(ImportDataSourceKind kind)
        {
            if (kind == ImportDataSourceKind.ShapefileFile || kind == ImportDataSourceKind.ShapefileDirectory)
            {
                return L.WizardKindShapefile;
            }
            if (kind == ImportDataSourceKind.GeoTiffFile)
            {
                return L.WizardKindGeoTiff;
            }
            return kind.ToString();
        }
    }
}
