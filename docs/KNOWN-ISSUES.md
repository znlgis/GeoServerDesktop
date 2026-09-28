# 已知问题与兼容性清单（Known Issues）

> 本文件承接《GeoServerDesktop 功能总结与中期开发规划（2026-09）》§2.1 / §2.2 基线清单，随每次合入更新。
> 最后更新：2026-09-26 ｜ 实测基线：GeoServer 3.0.1 ｜ 已回归版本：2.28.0

## 一、兼容性总览

| 版本 | 状态 |
|---|---|
| GeoServer 3.0.1 | **实测基线**：四层测试套件（L1–L4）与真实数据 harness 全量通过 |
| GeoServer 2.28.0 | **已实测（2026-09-26）**：数据面与 3.0.1 结论一致；差异见第四节（栅格 WMTS、monitor 路径、GWC diskquota 等） |
| GeoServer 2.20+ | **预期兼容**，未逐版本实测；已知差异模式见第四节 |

## 二、服务端不支持项（3.0.1 无对应 REST 端点）

以下功能在 GeoServer 3.0.1 中没有对应 REST 端点，客户端库以显式 `NotSupportedException`（含注释）呈现，而非静默失败：

| 项 | 位置 | 现状 |
|---|---|---|
| keystore 全部方法 | `src/GeoServerDesktop.GeoServerClient/Services/KeystoreService.cs` | 显式异常 + 注释 |
| users/{name} 详情、组详情 | `src/GeoServerDesktop.GeoServerClient/Services/UserGroupService.cs` | 显式异常 + 注释 |
| GWC 默认设置 `/gwc/rest/settings` | `CachingDefaultsViewModel` | UI 提示「暂不可配置」 |

> 这些项不是缺陷；如需对应能力，请使用 GeoServer 官方 web admin 操作。

## 三、服务器行为基线（Warn，非产品缺陷）

- **GeoServer 3.0 移除工作空间级 `/{ws}/ows` 端点**——影响预览 URL 生成策略（客户端已适配）。
- **WCS 2.0.1 GeoTIFF 输出丢弃 ModelPixelScale/Tiepoint**；**WCS 1.0.0 不受理 GetCoverage**。
- **WMTS 矢量层默认 `format=mvt`**——预览需显式指定 `png`。
- **imports / transforms / structuredcoverage 等扩展未安装时返回 404**——服务保留 + 注释，基线不翻转。
- **样式 SLD 读回时 NamedLayer/UserStyle 名被规范化**（3.0.1 实测重写为 "Default Styler"；颜色等样式体保留）——编辑器加载已有样式时以 REST 资源名为准（客户端已适配）。
- **图层（LayerInfo）REST 无 `enabled` 通道**——`PUT {"layer":{"enabled":...}}` 被服务端静默忽略（200 OK 但读回原样）；
  M4 批量启停因此落到存储层：`PUT {"dataStore":{"enabled":false}}` 或 `PUT {"coverageStore":{"enabled":false}}`
  生效，且实测该操作会即时从 WMS/WFS GetCapabilities 中摘除其下图层（能力面复核：BatchOperationIT / harness 3.8）。
- **PUT 局部更新语义**——`PUT /rest/layers/{name}` 只带变更字段（如 `{"layer":{"name":..., "defaultStyle":{"name":...}}}`）
  即生效且其它字段保持；`dateModified` 不一定推进，不能用来判定变更是否被受理（M4 harness 复核依赖独立读回）。
- **绑定不存在的样式名服务端返回 200 且静默忽略**（M4 harness 探针发现）——批量改默认样式必须先确保样式存在，
  否则 GET 回读会保持原样式（不是产品缺陷，属服务端契约）。
- **无 `GET /rest/workspaces/{ws}/namespace` 反查端点**——M4 迁移按命名约定 `prefix=workspaceName` +
  `GET /rest/namespaces/{prefix}.json` 探测；新建工作空间自动生成占位命名空间 `uri=http://{ws}`。
- **`DELETE /rest/workspaces/{ws}?recurse=true` 级联回收孤立命名空间**（实测 3.0.1 与其 namespace 一并 404）。
- **自定义投影（`.prj` 无 EPSG 权威码）图层的 GeoJSON 输出不可靠**（3.0.1 与 2.28 均实测）：服务端写完要素后在流尾部抛
  `Cannot invoke "String.indexOf(String)" because "identifier" is null`，响应成为「JSON 前缀 + XML 异常」或被截断，
  **要素静默丢失**；同一请求显式带 `srsName=EPSG:4326` 即完整。CQL 空间过滤在该类图层上同样触发。
  真实数据几何面因此走 **GML 通道**（原生坐标、响应完整），判据用拓扑保持（唯一顶点集/包络/部件数）。
- **EPSG:4326 图层的裸 CQL 几何字面量按 (lat,lon) 解释**：`INTERSECTS(the_geom,POINT(lon lat))` 静默漏检，
  必须写成 `SRID=4326;POINT(lon lat)`；请求级 `BBOX` 需带 `,EPSG:4326` 后缀才按 (lon,lat)。
  生成过滤器/预览链接时务必显式声明（harness `Axis/*` 钉住该契约）。
- **缺 `.cpg` 声明的非 ASCII 属性表**：GeoTools 只认 `.cpg`，无声明则按平台默认 ISO-8859-1 解码，
  中文属性在 WFS 侧变乱码（服务端解码契约，客户端不能代为改正）。客户端职责是发布前告知（见 E42）。
- **服务端错误未必是 GeoServer 的 JSON/XML**：容器层（Tomcat）会直接返回 HTML 错误页；客户端已统一按形态摘要成
  一行可读原因（`ServiceBase.Describe`，E46），调用方不应再自行拼接 `ResponseContent`。
- **`resultType=hits` 时即使指定 `outputFormat=application/json` 仍返回 GML**（计数解析按文本取 `numberMatched`）。
- **WCS 2.0.1 `DescribeCoverage` 的 `gmlcov:rangeType` 内 `swe:DataRecord` 为空**：波段数/波段名不经 WCS 元数据宣告，
  须经 `GetCoverage` 输出核对（本轮以 3 波段 Byte 与 Int16 瓦片栅格逐点验证）。
- **服务级 WMS/WFS/WCS/WMTS 设置为平铺字段**（如 `/rest/services/wms/settings.json` 根 `wms` 下直接
  `enabled/title/...`，无 `service` 包装）——M4 设置比对按实测形态扁平化。
- **工作空间级 WMS 设置 `/rest/services/wms/workspaces/{ws}/settings` 仅在服务端已注册时存在**——
  未注册的工作空间返回 404（非产品缺陷；M4 SettingsSyncIT 采用环境自适应发现，全无则 SkipLog 登记）。

## 四、2.x 兼容性差异（已于 2026-09-26 实测 GeoServer 2.28.0）

矩阵做法：同一份数据目录、同一套真实数据检查（harness 扩展段 + L1–L4）跑 `docker.osgeo.org/geoserver:2.28.0`，
与 3.0.1 同口径逐项比对。

| 领域 | 3.0.1 实测契约 | 2.28.0 实测结果 |
|---|---|---|
| 数据面（属性/几何/编码/栅格/分页/负路径） | 见第三节各契约 | **一致**（Warn 集合与 3.0.1 逐项相同；本轮客户端修复在 2.x/3.x 语义一致） |
| 栅格 WMTS | coverage 瓦片可取 | **`GetTile` 返回 400**（GWC 1.28 未把该 coverage 注册为可瓦片图层）→ 唯一新增失败 |
| styles 创建 | `Accept` 需含 `text/plain` | 2.x 无此要求；附加头无害（实测不报错） |
| featureType 创建请求体 | 容忍恒定携带的 `"attributes"` 包装体 | **直接 500** → 写出侧已禁止（`FeatureTypeAttributeConverter.CanWrite => false`，E45） |
| shapefile 目录存储连接参数 | 多余 `namespace` 参数会让非 ASCII 基名解析为 schema 前缀而失败（向导模板本就只给 `url`） | 同 3.0.1（一致行为） |
| monitor 请求列表 | 路由可用 | **404**（路径不同，客户端该服务在 2.x 上不可用） |
| GWC diskquota | XML 形态已对齐 3.0.1 | 形态不同（既有 IT 按 3.0.1 钉值，在 2.28 失败） |
| `/rest/about/version` | 按 3.0.1 字面钉值 | 返回 2.28.0（既有 IT 的基线断言按版本失败，属预期钉值） |
| 工作空间级 `/{ws}/ows` | 3.0 已移除 | 2.x 仍在（客户端已按 3.0 适配，不依赖旧端点） |

**跨版本测试的两条硬约束**（踩过的坑，写在这里避免复现）：

1. 不要把 3.0.1 的 data_dir 直接复制给 2.x——配置版本漂移会产生大量 500 假信号，必须用该版本自己初始化的空 data_dir。
2. 跨版本夹具只走产品自身的发布路径（`ImportWizardService`），不要手拼 store/featureType 参数；
   手拼路径曾把「测试路径差异」伪装成「产品缺陷」。

## 五、使用与维护

- 本清单是「实测真值」方法论的载体：每项差异都有对应测试基线（L2 集成测试 / L3 真实数据）。
- 修复或环境变化时同步更新本文件与 `docs/ROADMAP.md`；不再新增一次性报告类文档。
