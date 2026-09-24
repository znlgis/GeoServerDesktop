# GeoServerDesktop 功能总结与中期开发规划（2026-09）

> 日期：2026-09-24 ｜ 规划跨度：3–6 个月（至 2027-03 前后）｜ 侧重点：**查漏补缺优先**
> 基准：main 分支 `c96e8c0`（四层测试套件与真实数据 harness 已落地），GeoServer 3.0.1 实测真值基线。

---

## 一、项目现状总结

### 1.1 项目定位

GeoServerDesktop 是一个跨平台 GeoServer 管理桌面应用，由两部分组成：

| 组成 | 技术 | 规模 | 状态 |
|---|---|---|---|
| **GeoServerDesktop.GeoServerClient** | .NET Standard 2.0 | 45 个服务类 / 195 个 REST 操作 / 18 个模型文件 | 已按 GeoServer 3.0.1 实测对齐，发布至 NuGet（v1.0.0–v1.0.3，tag 触发 CI） |
| **GeoServerDesktop.App** | Avalonia 11.3.9 / .NET 8 / CommunityToolkit.Mvvm / Mapsui 5 | 19 个 ViewModel + 19 个 View | 功能面覆盖主要管理场景，中英双语 |

### 1.2 已实现功能面（App UI）

- **数据管理**：工作空间、数据存储（PostGIS/Shapefile 等）、覆盖存储（GeoTIFF 等）、WMS/WMTS 级联存储、图层与要素类型、SLD 样式管理、图层组、Mapsui 地图预览。
- **服务配置**：WMS / WFS / WCS 设置读写回滚。
- **系统管理**：About/版本、全局设置、日志级别配置、GeoWebCache（缓存默认值、格网集、磁盘配额）。
- **安全管理**：安全设置、用户/组/角色管理。
- **基础设施**：多连接配置管理（含真实连通性探测）、中英文一键切换、跨平台（Windows/macOS/Linux）。

### 1.3 测试与质量现状（2026-09-15 修复轮完成）

| 层 | 内容 | 数量 | 结果 |
|---|---|---|---|
| L1 单元测试 | FakeHttpClient 离线测 45 服务（URL/请求体/响应解析三元断言） | 321 | 全绿 |
| L2 集成测试 | 真实 GeoServer 3.0.1 CRUD 闭环 + 写后读一致性 + 错误路径 | 50 | 全绿 |
| L3 真实数据端到端 | GDAL 确定性生成数据，WFS/WMS/WCS/WMTS/GWC 数据面核验（期望值从文件头独立推导） | 22 | 全绿 |
| L4 ViewModel 无头测试 | 19 个 VM 命令流 | 60 | 全绿 |
| 控制台 harness | 34 检查，Fail>0 退出码 1，可接 CI | 34 | 34 Pass / 2 Warn / 0 Fail |

`dotnet test` 合计 **453/453**。数据生成与外部数据注入均数据无关（`GSD_TEST_DATA_DIR` / `GSD_REAL_DATA_DIR`），服务器不可达自动 Skip 登记。

### 1.4 本轮测试修复的关键缺陷（已闭环）

- 客户端：Accept 头兼容 3.x styles（E41）、请求体 NullValueHandling.Ignore 消除零维 coverage、认证 UTF-8、资源名统一转义、GlobalSettings 根键 `{"global"}` 修正 + 12 个 settings 模型 ExtensionData 防丢键、安全域/GWC 域全量按 3.0.1 实测契约修正。
- App：shapefile 存储发布载荷（namespace+url）、连接真实探测、CachingDefaults 假实现显式化、硬编码文案全部国际化。

---

## 二、当前问题与短板清单（规划输入）

### 2.1 明确的服务端不支持项（3.0.1 无 REST 端点，库层已显式 `NotSupportedException`）

| 项 | 位置 | 现状 |
|---|---|---|
| keystore 全部方法 | `KeystoreService.cs:33-45` | 显式异常 + 注释 |
| users/{name} 详情、组详情 | `UserGroupService.cs:138-142` | 显式异常 + 注释 |
| GWC 默认设置 `/gwc/rest/settings` | `CachingDefaultsViewModel` | UI 提示"暂不可配置" |

这些项**不是缺陷**，但 UI 体验上是缺口：要么提供替代通道，要么把降级状态做成一等公民。

### 2.2 服务器行为基线 Warn（非产品缺陷，但影响用户预期）

- GeoServer 3.0 移除工作空间级 `/{ws}/ows` 端点（影响预览 URL 生成策略）。
- WCS 2.0.1 GeoTIFF 输出丢弃 ModelPixelScale/Tiepoint；WCS 1.0.0 不受理 GetCoverage。
- WMTS 矢量层默认 format=mvt（预览需显式 png）。
- imports/transforms/structuredcoverage 等扩展未安装时 404（服务保留 + 注释，基线不翻转）。

### 2.3 客户端库架构债

- **45 个服务类无接口抽象、无泛型基类**：GET/包装/序列化样板重复 200+ 处，URL 拼接手写散布。增加新端点或修契约时改动面大（本轮 3.0.1 对齐修复就动了全 45 个服务）。
- `GeoServerClientFactory`（约 450 行）与 `GeoServerConnectionService` 各手写 45/18 个透传方法，加一个服务要改三处。

### 2.4 App 架构债

- 无 DI 容器、无导航服务：19 个子 VM 在 `MainWindowViewModel` ctor 直接 new。
- 各管理 VM 重复「IsLoading + StatusMessage + IsConnected 守卫 + try/catch/finally」样板 10+ 处。
- `LocalizationService` 为单例 + 硬编码 `T(en, zh)` 属性数百条，无 resx 资源文件，新增文案成本高且易漏。
- 欢迎页为 `PlaceholderView` 占位。

### 2.5 UI 功能面缺口（对照 GeoServer 官方 web admin）

| 缺口 | 说明 | 优先级判断 |
|---|---|---|
| **数据导入向导** | ImporterService 已有库层（依赖扩展，未装则 404），App 无 UI；用户目前只能手动准备数据目录 | 高（核心价值） |
| **SLD 编辑器** | 样式管理仅上传/绑定，无编辑器；README Roadmap 明示未做 | 高（Roadmap 项） |
| **批量操作** | 多选删除/批量发布/批量改样式，Roadmap 未勾项 | 中高 |
| **日志实时查看** | 仅日志级别配置，无 tail/文件查看 | 中 |
| **资源树浏览** | `ResourceTreeNode` 模型已有，无对应浏览器视图 | 中 |
| **导入/导出** | 配置/资源导入导出（含 GeoServer 迁移场景），Roadmap 未勾项 | 中 |
| **WPS/CSW 设置页** | 库层有服务，无 View | 低（依赖扩展安装） |
| **模板/字体/monitoring/urlchecks UI** | 仅库层保留 | 低 |

### 2.6 工程化短板

- CI 仅有 tag 触发的 NuGet 发布流水线；**无 PR/push 触发的构建+测试流水线**（453 用例与 harness 未纳入日常回归）。
- 兼容性声明仍写 "built against GeoServer 2.28.x"，而实测基线已是 3.0.1，README 兼容性说明需要更新并明确 2.x 回归矩阵。
- 无发布物（安装包/免压缩包）分发渠道：桌面应用本身只有源码构建一种获取方式。
- `docs/` 下 10+ 份阶段性报告（FINAL_TASK_REPORT、VERIFICATION_SUMMARY 等多数已过时），缺一份活的 ROADMAP/KNOWN-ISSUES 文档。

---

## 三、规划原则

1. **查漏补缺优先**：先把已有功能面的洞补齐（导入向导、SLD 编辑、批量操作），再开新板块。
2. **实测真值驱动**：延续 3.0.1 实测基线方法论，每个新功能都带集成/真实数据测试。
3. **测试资产先行**：新功能 = 库方法 + L1/L2 测试 + VM + L4 测试，配套 harness 检查。
4. **不破坏数据无关原则**：所有测试数据经参数注入，不内置路径与期望值。
5. **架构改动服务于功能**：样板消除、DI 重构只在支撑新功能时顺带做，不做大爆炸式重写。

---

## 四、里程碑规划（3–6 个月）

### M1（第 1–4 周）：工程化地基 + 快速补缺

**目标**：让日常开发有回归保障，补掉最小的用户可见洞。

1. **CI 补全**
   - 新增 `build-test.yml`：push/PR 触发 → `dotnet build` + L1 单元测试（全环境可跑）+ L4 无头测试；可选 job 以 services 容器起 GeoServer+PostGIS 跑 L2/L3 与 harness（缺环境自动跳过协议已具备）。
   - Lint：`.editorconfig` 已有，加 `dotnet format --verify-no-changes` 检查。
2. **文档治理**
   - 过时报告归档至 `docs/archive/`；新建单一活文档 `docs/ROADMAP.md`（承接本规划）与 `docs/KNOWN-ISSUES.md`（承接 2.1/2.2 基线清单）。
   - README 兼容性章节更新为「实测 GeoServer 3.0.1；2.20+ 预期兼容，见 KNOWN-ISSUES 差异清单」，补 NuGet 安装说明与版本号。
3. **App 小补缺**
   - 日志实时查看（读取 GeoServer 日志文件内容展示，`logging` REST + `GEOSERVER_LOG_LOCATION` 状态项）。
   - 资源树浏览视图（基于已有 `ResourceTreeNode` + ResourceService）。
   - 欢迎页占位符替换为仪表盘（版本、连接状态、工作空间/图层计数速览）。

**验收**：CI 双流水线全绿；ROADMAP/KNOWN-ISSUES 就位；三个小功能各带 L2/L4 测试。

### M2（第 5–10 周）：数据导入向导（核心功能补缺）

**目标**：打通「本地数据 → 发布为服务」的最短路径，这是桌面工具相对 web admin 的最大价值点。

1. **库层**：`ImporterService` 增加环境探测方法（探测扩展是否安装，404 → 明确状态）；不依赖扩展的**内置发布向导数据面**：目录浏览（file URL 校验）、数据源类型识别（shp 目录/GeoTIFF/PostGIS 连接探测）、`DataStoreService`/`CoverageStoreService` 发布参数模板（按 store type 给出参数键值对 schema，而非现在 App 端手拼 shapefile 载荷的约定式写法）。
2. **App 层**：三步向导 UI（选数据 → 配参数/预检（bbox、SRID、字段、记录数预览，复用 RealData 的文件头独立解析技术）→ 发布结果反馈含 WMS/WFS 预览链接）。
3. **扩展自适应**：Importer 扩展可用时走扩展 API，不可用时走内置向导，UI 统一。
4. **测试**：L1（参数构造/探测逻辑）、L2（发布闭环）、harness 增加「向导路径」检查项。

**验收**：外部真实数据（`GSD_REAL_DATA_DIR`）经向导路径一键发布并 WFS 计数比对通过。

### M3（第 11–16 周）：SLD 编辑器 + 样式体系强化

**目标**：补 Roadmap 中"online SLD editor"项。

1. **SLD 编辑器**：结构化编辑（点/线/面符号、颜色、尺寸、规则过滤表达式）+ 源码双模式（XML 编辑 + 应用前校验）；样式预览复用 MapPreview 的 WMS 出图（绑定待编辑样式 + DEM/演示数据）。
2. **样式库**：样式-图层绑定关系总览、未引用样式清理提示。
3. **测试**：编辑器模型单元测试；L2：编辑 → 保存 → WMS GetMap 出图像素级真值验证（复用 L3 红色真值像素技术）。

**验收**：新建样式 → 编辑 → 绑定图层 → 出图像素验证全链路自动化通过。

### M4（第 17–22 周）：批量操作 + 导入/导出（Roadmap 收尾）

1. **批量操作**：图层/样式/工作空间多选列表、批量启停、批量删（级联选项）、批量改默认样式。
2. **导入/导出**：工作空间级配置导出（REST 读全量资源 → JSON 归档）/导入（目标实例重建，含依赖顺序：ws → store → style → layer → group），作为 GeoServer 实例迁移工具。
3. **设置同步**：多连接间的 settings 差异比对视图（读-比-选择性应用）。
4. **测试**：导出→清空→导入→资源等价性断言（L2 级）。

**验收**：A 实例工作空间迁移到 B 实例后，WMS/WFS 能力与图层清单等价。

### M5（第 23–26 周）：架构收敛 + 版本发布（技术债集中偿还）

> 按「架构改动服务于功能」原则，前四个里程碑新功能已把样板问题放大到必须处理时，在此集中偿还。

1. **客户端库重构**：提取 `ServiceBase`（URL 构建/转义/包装反序列化/错误处理）或轻量泛型 helper，45 个服务消除 200+ 处样板；服务接口化（`IDataStoreService` 等）供 VM 测试替身与未来插件系统使用。保持公共 API 兼容（`GeoServerClientFactory` 方法签名不变），发 minor 版本。
2. **App 重构**：引入 `Microsoft.Extensions.DependencyInjection`，`MainWindowViewModel` 改工厂/惰性创建子 VM；提取 `ViewModelBase` 的 Loading/Status/连接守卫管道；LocalizationService 迁移 resx 资源文件（保留 T() 兼容层）。
3. **发布**：GitHub Release 附桌面端免安装包（win-x64/osx/linux 三平台 publish）+ NuGet 客户端库 v2.0（若含破坏性变更）或 v1.1。
4. **收尾**：插件系统（Roadmap 最后一项）做技术验证原型（接口化后的服务注入点 + 示例插件），是否正式纳入视余量决定。

**验收**：重构后 453 用例 + harness 全绿不降级；三平台 Release 构建产物可运行。

### 里程碑依赖与并行性

- M1 独立且最优先（所有后续工作的回归保障）。
- M2 与 M3 无强依赖，可按资源并行或调序；M4 依赖 M2 的参数模板。
- M5 应在 M2–M4 至少完成一项后进行，确保重构有测试护航。
- 每个里程碑结束打 tag（App 无版本号概念则随 NuGet 包版本节奏：v1.1.0 → v1.2.0 → …）。

---

## 五、风险与对策

| 风险 | 影响 | 对策 |
|---|---|---|
| GeoServer 3.x 后续版本再变契约 | 库层返工 | 维持「实测真值 + KNOWN-ISSUES 差异清单」方法论；L2 集成测试是契约回归网 |
| Importer 等扩展环境差异大 | 向导行为不一致 | 扩展探测显式化；内置向导不依赖扩展为主路径 |
| Avalonia/Mapsui 跨平台渲染差异 | 预览/编辑器表现不一 | 预览验证用 HTTP 层像素真值而非 UI 截图，规避 UI 自动化脆弱性 |
| 重构引发回归 | 发布延期 | 重构仅在测试全绿基线上做，分 PR 小步走，CI 全量回归 |
| 服务器不可达环境跑不了 L2/L3 | CI 覆盖打折 | 已有 SkipLog 显式跳过协议；CI 用 GitHub services 容器补齐 |

---

## 六、度量指标

- 测试：`dotnet test` 用例数只增不减，Fail=0；harness 检查项每里程碑递增；CI 主干全绿率 ≥ 95%。
- 功能：每里程碑交付 ≥ 1 个用户可见功能（M2–M4）或工程能力（M1/M5）。
- 债务：M5 结束后样板重复点（连接守卫/序列化样板）清零；LocalizationService 硬编码条目迁移完毕。
- 文档：ROADMAP/KNOWN-ISSUES 随每次合入更新，不再新增一次性报告类文档。
