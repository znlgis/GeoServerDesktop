# 已知问题与兼容性清单（Known Issues）

> 本文件承接《GeoServerDesktop 功能总结与中期开发规划（2026-09）》§2.1 / §2.2 基线清单，随每次合入更新。
> 最后更新：2026-09-25 ｜ 实测基线：GeoServer 3.0.1

## 一、兼容性总览

| 版本 | 状态 |
|---|---|
| GeoServer 3.0.1 | **实测基线**：四层测试套件（L1–L4）与真实数据 harness 全量通过 |
| GeoServer 2.20+ | **预期兼容**，尚未完整实测；差异与注意事项见第四节 |

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
- **服务级 WMS/WFS/WCS/WMTS 设置为平铺字段**（如 `/rest/services/wms/settings.json` 根 `wms` 下直接
  `enabled/title/...`，无 `service` 包装）——M4 设置比对按实测形态扁平化。
- **工作空间级 WMS 设置 `/rest/services/wms/workspaces/{ws}/settings` 仅在服务端已注册时存在**——
  未注册的工作空间返回 404（非产品缺陷；M4 SettingsSyncIT 采用环境自适应发现，全无则 SkipLog 登记）。

## 四、2.x 兼容性差异（预期兼容，待回归验证）

客户端最初基于 2.28.x 规范构建，后按 3.0.1 实测契约完成对齐（见 `docs/testing-report.md`）。
以下为按 3.0.1 固化、在 2.x 上可能不同的行为点；完整回归前请留意：

| 领域 | 3.0.1 实测契约 | 2.x 注意事项 |
|---|---|---|
| styles 创建 | `Accept` 需含 `text/plain`（否则 500） | 2.x 无此要求，附加头预期无害 |
| 安全域路由 | 按 3.0.1 控制器路由与包装（authfilters / authproviders 形态、users / groups 数组等） | 2.x 路由与包装可能不同 |
| GlobalSettings 根键 | `{"global": ...}` | 2.x 可能为其他根键，读取可能为空 |
| GWC 域形态 | layers / gridsets / blobstores 为纯 JSON 数组；seed / truncate 为 XML | 2.x 包装可能不同 |
| 工作空间预览 | 3.0 移除 `/{ws}/ows` | 2.x 仍可用旧策略 |

> 2.x 回归矩阵（2.20 / 2.24 / 2.28 实测）为后续工作项；发现问题时按「实测真值」方法论同步更新本清单与测试基线。

## 五、使用与维护

- 本清单是「实测真值」方法论的载体：每项差异都有对应测试基线（L2 集成测试 / L3 真实数据）。
- 修复或环境变化时同步更新本文件与 `docs/ROADMAP.md`；不再新增一次性报告类文档。
