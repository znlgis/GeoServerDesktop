# GeoServerDesktop 路线图（Roadmap）

> **本文件是项目唯一的活路线图**，承接《GeoServerDesktop 功能总结与中期开发规划（2026-09）》
> （`docs/GeoServerDesktop功能总结与中期开发规划.md`），随每次合入更新。
> 最后更新：2026-09-25 ｜ 实测基线：GeoServer 3.0.1

## 状态总览

| 里程碑 | 主题 | 状态 |
|---|---|---|
| **M1** | 工程化地基 + 快速补缺 | 已完成 |
| M2 | 数据导入向导 | 已完成 |
| M3 | SLD 编辑器 + 样式体系强化 | 已完成 |
| M4 | 批量操作 + 导入/导出 + 设置同步 | 已完成 |
| M5 | 架构收敛 + 版本发布 | 已完成 |

---

## M1（第 1–4 周）：工程化地基 + 快速补缺

**目标**：让日常开发有回归保障，补掉最小的用户可见洞。

### 1. CI 补全
- [x] `build-test.yml`：push/PR 触发 → `dotnet build` + L1 单元测试 + L4 无头测试（build-test job）
- [x] Lint：`dotnet format --verify-no-changes` 检查（lint job）
- [x] 可选集成 job：容器起 GeoServer + PostGIS 跑 L2/L3 与 harness（缺环境自动跳过协议）
- [x] 推送后实测验证 CI 全绿（build-test / lint / integration 三 job 真实执行；run 35970416303）

### 2. 文档治理
- [x] 过时报告归档至 `docs/archive/`（14 份 2024 年阶段报告）
- [x] 新建本文件（ROADMAP）与 `docs/KNOWN-ISSUES.md`
- [x] README 兼容性章节更新（实测 3.0.1；2.20+ 预期兼容）+ NuGet 安装说明与版本号

### 3. App 小补缺（各带 L2/L4 测试）
- [x] 日志实时查看（读取 GeoServer 日志文件内容展示；`logging` REST + `GEOSERVER_LOG_LOCATION` 状态项）——已实现：LoggingView 日志文件查看器（尾部 500 行，`logs/geoserver.log` 经 /rest/resource 读取），L1+L2+L4 测试就位
- [x] 资源树浏览视图（基于 `ResourceTreeNode` + ResourceService）——已实现：主窗口常驻资源树面板（TreeView + 选中触发级联延迟加载 + 图层预览联动 + 刷新），L2+L4 测试就位
- [x] 欢迎页仪表盘（版本、连接状态、工作空间/图层计数速览）——已实现：欢迎页替换为 DashboardView（连接状态 + GeoServer 版本 + 工作空间/图层计数，连接后自动刷新、断开复位），L2+L4 测试就位

**M1 验收**：CI 双流水线全绿；ROADMAP/KNOWN-ISSUES 就位；三个小功能各带 L2/L4 测试。

---

## M2（第 5–10 周）：数据导入向导（核心功能补缺）

**目标**：打通「本地数据 → 发布为服务」的最短路径，这是桌面工具相对 web admin 的最大价值点。

- [x] **库层**：`ImporterService` 环境探测（404 → NotInstalled 明确状态）；不依赖扩展的内置发布向导数据面——目录浏览（file URL 校验）、数据源类型识别（shp 目录 / GeoTIFF / PostGIS 连接探测）、按 store type 的发布参数模板（替代 App 端手拼载荷）——已实现：`ImportWizardService`（三类数据源发布编排，幂等）+ `GeoFileInspector`（SHP/DBF/TIFF 文件头解析预览）+ 发布名/磁盘名分离（`NativeName`）
- [x] **App 层**：三步向导 UI（选数据 → 配参数/预检（bbox、SRID、字段、记录数预览）→ 发布结果反馈含 WMS/WFS 预览链接）——已实现：`ImportWizardView` / `ImportWizardViewModel`（三步状态机、连接守卫、PostGIS 探测、本地预检、发布重入、中英 L10n）
- [x] **扩展自适应**：扩展可用性探测就位（Available / NotInstalled / Unknown，库层）；App 以内置向导为统一路径（扩展 API 直连分支未接入，待扩展环境时补充）
- [x] **测试**：L1（参数构造/探测逻辑）、L2（发布闭环）、harness 增加「向导路径」检查项——已实现：L1 全量 375 通过、L2 `ImportWizardIT` 3 用例（发布闭环 + 幂等）、harness 向导检查（内置 shapefile + PostGIS + 外部真实数据）
- [x] **实测暴露的产品修复**：覆盖度 `nativeCRS`/`crs` 对象形态反序列化（`CrsStringConverter`）；`ExistsAsync` 404 语义修正（幂等重入不再吞错）

**验收**：外部真实数据（`GSD_REAL_DATA_DIR`）经向导路径一键发布并 WFS 计数比对通过。——已实测：16 个真实 shapefile（中国行政区划数据）经向导路径发布，WFS `numberMatched` 与 DBF 记录数全部一致（harness Pass=103 / Warn=2 / Fail=0；Warn 为两个已知 WCS 3.0.1 限制）。CI 三 job 全绿（run 36009604934）。

## M3（第 11–16 周）：SLD 编辑器 + 样式体系强化

**目标**：补 Roadmap 中 "online SLD editor" 项。

- [x] **SLD 编辑器**：结构化编辑（点/线/面符号、颜色、尺寸、规则过滤表达式）+ 源码双模式（XML 编辑 + 应用前校验）；样式预览复用 MapPreview 的 WMS 出图——已实现：库层 `SldBuilder`/`SldParser`/`SldValidator`（模型 ↔ SLD 1.0.0 双向转换、规则过滤表达式、结构+文档双层校验）；App 层 `SldEditorView`/`SldEditorViewModel`（结构化/源码双模式、规则增删与符号编辑、保存前校验、WMS 预览 URL）
- [x] **样式库**：样式-图层绑定关系总览、未引用样式清理提示——已实现：库层 `StyleUsageService`（全局样式 × 图层默认样式聚合、工作空间样式排除）；App 层 `StyleLibraryView`/`StyleLibraryViewModel`（总览列表、"仅显示未引用"筛选、未引用样式删除、引用中删除拦截）
- [x] **测试**：编辑器模型单元测试；L2：编辑 → 保存 → WMS GetMap 出图像素级真值验证——已实现：L1 新增 72 用例（生成/解析/校验/聚合）；L2 `SldEditorFlowIT` 3 用例（全链路像素验证 + 非法 SLD 双防线）；L4 新增 13 用例（编辑器 8 + 样式库 5）；harness 新增样式路径 11 检查项（含使用关系全量交叉核对）
- [x] **实测固化的服务端行为**：读回 SLD 时 NamedLayer/UserStyle 名被规范化重写为 "Default Styler"（名字信任边界：以 REST 资源名为准）；`/rest/layers.json` 图层名为 qualified 名；引用中样式删除 403 保护

**验收**：新建样式 → 编辑 → 绑定图层 → 出图像素验证全链路自动化通过。——已实测：L2 `SldEditorFlowIT` 3 用例全链路像素级通过（新建 → 编辑 → 绑定 → WMS 红/蓝像素验证 → 解绑 → 删除）；L4 全量 91 通过（含 M3 13 用例）；harness 样式路径 11 项全通过（总 Pass=50 / Warn=2 / Fail=0）。CI 三 job 全绿（run 36090794899）。

## M4（第 17–22 周）：批量操作 + 导入/导出

**目标**：Roadmap 收尾——把「多选批量」与「实例间迁移 / 跨连接设置同步」做成一等公民能力。

1. **批量操作**：图层/样式/工作空间多选列表、批量启停、批量删（级联选项）、批量改默认样式。
   —— 已实现：库层 `BatchOperationService`（部分成功语义：单项失败不中断整批，逐项结果 + FailureSummary
   汇总；引用中样式 403、coverageStore 404 回落路径按实测基线固化）；App 层 `BatchOperationsView` /
   `BatchOperationsViewModel`（多选列表 + 全选切换 + 级联选项 + 三类批量入口）。
   **实测基线**：GeoServer 3.0.1 图层无 `enabled` REST 通道，批量启停落到存储层——禁用 `dataStore` /
   `coverageStore` 后其图层即时从 WMS/WFS GetCapabilities 摘除（能力面复核：BatchOperationIT + harness）；
   绑定不存在的样式名服务端 200 静默忽略，批量改样式前需保证目标样式存在。
2. **导入/导出（工作空间级迁移工具）**：REST 读全量资源 → ZIP 归档（`manifest.json` + `styles/*.sld`）
   / 目标实例按依赖顺序重建（ws → namespace → store → style → featureType/coverage → 图层绑定 → layerGroup）。
   —— 已实现：库层 `WorkspaceMigrationService`（导出/导入 + 引用重写：workspace/namespace/store 名、
   连接参数 namespace URI、限定名 `ws:store`/`ws:layer`、layerGroup 自引用；剥离 `href/id/dateCreated/
   dateModified/_default` 等服务端字段；幂等：已存在默认跳过，Overwrite 时更新）+ `WorkspaceManifest` v1；
   App 层 `WorkspaceMigrationView` / `WorkspaceMigrationViewModel`（导出→内存归档→SaveFilePicker 落盘；
   OpenFilePicker→目标工作空间/前缀/URI 覆写→逐项结果表；文件对话框经委托注入保持无头可测）。
   **实测基线**：3.0.1 无 workspace→namespace 反查端点（按前缀=工作空间名 + `GET /rest/namespaces/{p}.json`
   探测）；新建工作空间自动生成占位命名空间 `uri=http://{ws}`；`recurse=true` 删除工作空间级联回收孤立命名空间。
3. **设置同步**：多连接间的 settings 差异比对视图（读-比-选择性应用）。
   —— 已实现：库层 `SettingsCompare`（叶子级 diff，volatile 键 `id/updateSequence/href/dateCreated/
   dateModified` 排除；数组整体叶子语义；`Apply` 以目标实例为基底合并勾选路径后整包 PUT——与「整包替换 +
   ExtensionData 防丢键」契约一致）+ `SettingsCompareService`（双连接 CompareAsync / ApplyAsync /
   任意路径读写）；App 层 `SettingsSyncView` / `SettingsSyncViewModel`（源连接表单 + 5 域 + 差异勾选表 +
   选择性应用后自动重比对收敛）。
   **实测基线**：服务级 WMS/WFS/WCS/WMTS 设置为平铺字段（无 service 包装）；工作空间级 WMS 设置仅在
   已注册时存在，否则 404（SettingsSyncIT 环境自适应发现，全无则 SkipLog 登记）。
4. **测试**：
   - L1 新增 32 例（`BatchOperationServiceTests` 14 / `WorkspaceMigrationServiceTests` 10 /
     `SettingsCompareTests` 8；新基建 `PathFakeClient` 支持按路径应答与 404 注入）。
   - L2 新增 8 例（`BatchOperationIT` 3 / `WorkspaceMigrationIT` 3 / `SettingsSyncIT` 2）：批量样式落库 +
     存储启停 WMS 能力面 + 级联删除回收命名空间 + 403/回落/缺参失败面；迁移清单结构、跨空间等价
     （图层/绑定/WFS 计数 vs DBF 头独立真值）、导出→清空→导入还原；设置扰动→差异→选择性应用→恢复。
   - L4 新增 9 例（批量 4 / 迁移 2 / 同步 3，含 VmRest 交叉复核与文件对话框注入）+ `MainWindowViewModelTests`
     M4 命令流/守卫 + `LocalizationServiceTests` 7 条 M4 双语探针。
   - harness 新增 3 段 13 检查项（批量 5 / 迁移 5 / 同步 3）：全量 Pass=70 Warn=2 Fail=0；
     注入 `GSD_REAL_DATA_DIR` 后 Pass=134 Warn=2 Fail=0。
   - `dotnet test` 合计 **675/675**（L1 487 + L2/L3 88 + L4 100），0 失败 0 跳过；
     `dotnet format --verify-no-changes` 干净。

**验收**：A 实例工作空间迁移到 B 实例后，WMS/WFS 能力与图层清单等价。
—— 已实测：`WorkspaceMigrationIT` 覆盖 A→B（同实例双空间）与 A→A'（清空→导入还原）两种形态，
图层清单/默认样式绑定/WFS `numberMatched` 与 DBF 头独立真值全部对齐；harness 迁移段同步通过。
同实例双空间已覆盖迁移路径全部代码分支（引用重写、工作空间/全局两级样式注册、幂等跳过、
绑定还原），跨真实双实例为同一代码路径、仅 HTTP 基址不同。CI 三 job 全绿（run 36095480103）。

## M5（第 23–26 周）：架构收敛 + 版本发布

> 原则 5「架构改动服务于功能」落地为**分五步、每步全量回归护航**的连续提交，不做大爆炸式重写。

1. **客户端库样板消除**（`d18503c`）
   - 新增 `ServiceBase`：HTTP 客户端持有与空值校验、`Esc` 转义、`JsonContent/JsonContentRaw/TextContent/BytesContent`
     请求体构造、`GetJsonAsync/GetWrappedAsync` 反序列化解包、`Post/PutJsonAsync`、`Post/PutContentAsync`（发送后释放）、`Bool`。
   - 49 个服务全量接入：消除 197 处 `_httpClient` 直调、170 处内联 `Uri.EscapeDataString`、53 处
     手写「序列化 + StringContent + using」块、33 处 GET→反序列化两行样板、15 处文本/二进制体；
     服务层不再直接引用 `Newtonsoft` / 内部 `GeoServerJson`。
   - 语义零变更红线：`GeoServerJson.Request` 的 null-省略形态集中到 `JsonContent`；历史上以默认设置
     （保留 null）发送的 StructuredCoverage 两端点显式标注 `rawNulls: true` 原样保留；
     宽容转义（null→空串）改名 `EscPath` 避免遮蔽基类。
2. **服务接口化**（`a6e976a`）
   - `Interfaces/` 新增 **50 个服务接口 / 215 个成员**，签名与 XML 文档由实现类镜像生成；
     实现类统一 `class Xxx : ServiceBase, IXxx`。
   - 公共 API 完全兼容：`GeoServerClientFactory` 的 `CreateXxxService()` 仍返回具体类型（接口为实现面）。
   - App 接缝 `IGeoServerConnectionService` 的 25 个服务 getter 全部改返回接口类型。
   - 新增 `ServiceInterfaceParityTests`（离线反射）双向奇偶校验：服务↔接口存在性、成员与签名一致、
     工厂返回类型实现接口、接口可见性与计数——防「加方法漏接口」。
3. **App DI 与组合根**（`b26450e`）
   - 引入 `Microsoft.Extensions.DependencyInjection`；`Composition.ServiceCollectionExtensions`
     注册设置服务、连接服务与 24 个子 VM（单例，保留跨导航状态）。
   - `MainWindowViewModel` 删除构造期 24 处 eager new，改「可空字段 + 惰性属性 + `Resolve<T>()`」；
     双构造（无参自建默认容器 / 注入容器）。`App.axaml.cs` 成为组合根。
   - 顺带清掉 M1 后遗留的死代码 `PlaceholderViewModel/PlaceholderView`。
   - 新增 `CompositionTests`：容器可解析、24 子 VM 惰性单例与容器实例一致、
     **每个 ViewModel 都必须注册**（新增 VM 漏注册即失败）、默认视图为仪表盘。
4. **VM 样板收敛**（`3e214df`）
   - `ViewModelBase` 承载 `IsLoading` / `StatusMessage`（替代 21+22 处重复声明）、`Connection` 与
     `HasConnection(自定义未连接文案)` 守卫、`RunGuardedAsync/RunConnectedGuardedAsync` 加载管道。
   - 34 处「未连接提示 + 返回」守卫块收敛为 `if (!HasConnection(...)) return;`（各自原文案保持不变：
     导航级 `StatusPleaseConnect` 与命令级 `StatusNotConnected`）；21 个子 VM 构造改 `: base(connectionService)`。
   - 旧 VM 命令体内 55 处 `IsLoading + try/catch/finally` 三段式**未做批量替换**（try 体内存在提前
     `return` 的点位需逐点核对语义），管道已就位并在 `BatchOperationsViewModel` 落地为参考实现，
     其余随后续功能改动就地收敛。
5. **本地化迁移 resx**（`d3c44fc`）
   - 634 条文案迁入 `Resources/Strings.resx`（英文中性）+ `Strings.zh.resx`（中文卫星），
     key 即属性名，属性统一 `T(nameof(X))`；`T(en, zh)` 作为兼容层保留。
   - 语言切换仍是进程内显式开关（不依赖 `CurrentUICulture`），查表时显式传文化；
     `NeutralResourcesLanguage=en`，构建产出 `zh/GeoServerDesktop.App.resources.dll`。
   - 迁移保真：一次性快照逐条比对 634 条 en/zh 完全一致（校验用例不入库）；
     常态守护 `LocalizationResourceTests`：条目数不低于基线、每个属性可在资源表解析、
     中文卫星确实加载（与英文差异 ≥92%）、`{n}` 占位符仍可 `string.Format`。
6. **扩展点验证（规划「插件原型」的收敛式收尾）**
   - `ServiceSubstitutionTests`：容器覆盖注册假 `ILayerService` 即可驱动真实 `LayersManagementViewModel`
     完整命令流（零网络），并断言接缝 getter 全部返回抽象、外部容器可整体替换组合根——
     这正是插件系统的前置条件。正式插件机制（外部程序集加载、清单、版本与沙箱）待有真实需求再立项。
7. **版本发布**
   - 库 `GeoServerDesktop.GeoServerClient` 与 App 版本号升至 **1.1.0**；tag `v1.1.0` 触发
     `nuget-publish.yml`（NuGet 发布）与新增的 `release.yml`（win-x64 / linux-x64 / osx-arm64
     三平台 self-contained 免安装包 → GitHub Release）。
   - 三平台 publish 本地预验证通过（各约 100MB，包内含 `zh/` 卫星）；release.yml 内置
     「可入口 + 卫星目录存在」断言，缺失即失败不产半成品 Release。

**验收**：重构后用例数与 harness 全绿不降级；三平台 Release 构建产物可运行。
—— 已实测：`dotnet test` **677 → 692**（净增 15：接口奇偶 4 + 组合根 4 + 资源守护 4 + 扩展点 3），
0 失败 0 跳过，既有用例一条未降级；harness Pass=70 / Warn=2 / Fail=0 与 M4 基线完全一致；
`dotnet format --verify-no-changes` 干净，解决方案构建 0 警告；三平台自包含 publish 本地全部成功。

---

## 规划原则

1. **查漏补缺优先**：先把已有功能面的洞补齐，再开新板块。
2. **实测真值驱动**：延续 3.0.1 实测基线方法论，每个新功能都带集成/真实数据测试。
3. **测试资产先行**：新功能 = 库方法 + L1/L2 测试 + VM + L4 测试，配套 harness 检查。
4. **不破坏数据无关原则**：所有测试数据经参数注入，不内置路径与期望值。
5. **架构改动服务于功能**：样板消除、DI 重构只在支撑新功能时顺带做，不做大爆炸式重写。

## 度量指标

- **测试**：`dotnet test` 用例数只增不减，Fail=0；harness 检查项每里程碑递增；CI 主干全绿率 ≥ 95%。
- **功能**：每里程碑交付 ≥ 1 个用户可见功能（M2–M4）或工程能力（M1/M5）。
- **债务**：M5 结束后样板重复点清零；LocalizationService 硬编码条目迁移完毕。
- **文档**：本文件与 `KNOWN-ISSUES.md` 随每次合入更新，不再新增一次性报告类文档。

## 里程碑依赖

- M1 独立且最优先（所有后续工作的回归保障）。
- M2 与 M3 无强依赖，可按资源并行或调序；M4 依赖 M2 的参数模板。
- M5 应在 M2–M4 至少完成一项后进行，确保重构有测试护航。
- 每个里程碑结束打 tag（随 NuGet 包版本节奏：v1.1.0 → v1.2.0 → …）。
