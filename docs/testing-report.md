# GeoServerDesktop 全面测试报告（真实数据集成测试）

日期：2026-09-15（修复轮完成后更新）｜ 范围：GeoServerDesktop.GeoServerClient（45 服务类/195 REST 操作）、GeoServerDesktop.App（19 ViewModel + 服务层）
被测环境：GeoServer 3.0.1 @ http://localhost:8765/geoserver（Docker）；PostGIS 17.5/3.5.2 @ 127.0.0.1:5432（Docker，GeoServer 侧以主机名 `postgis` 访问）；.NET SDK（net8 测试目标）。
方案文档：桌面《GeoServerDesktop全面测试方案.md》（四层范围经确认）。

## 一、结果总览

| 层 | 工程/目录 | 数量 | 结果 |
|---|---|---|---|
| L1 客户端离线单元测试 | `src/GeoServerDesktop.Tests/Unit` | 321 | 全绿（不依赖服务器，任何环境可跑） |
| L2 真实 GeoServer REST 集成 | `src/GeoServerDesktop.Tests/Integration` | 50 | 全绿（CRUD 闭环+写后读校验+错误路径，修复后行为断言） |
| L3 真实数据端到端 | `src/GeoServerDesktop.Tests/RealDataTests`（+`RealData` 检查库） | 22 | 全绿（WFS/WMS/WCS/WMTS/GWC 数据面） |
| L4 ViewModel 无头测试 | `src/GeoServerDesktop.Tests/Headless` | 60 | 全绿（含 18 处环境守卫） |
| 控制台 harness | `src/GeoServerDesktop.RealDataHarness` | 34 检查 | 34 Pass / 2 Warn / 0 Fail，退出码 0 |

`dotnet test` 总计 **453/453**（修复轮后连跑两遍稳定）；harness `dotnet run` Fail=0、退出码 0，可直接接 CI。

## 二、真实测试数据（数据无关原则）

`tests/testdata/generate_testdata.py`（+`load_postgis.sh`）确定性生成，全部经 GDAL 独立验证；期望值由生成参数与文件头独立推导（`RealData/DataFileHeaders.cs`、`DbfRecords`、`ShapefileRecords`、`TiffPixels` 纯手写解析，不经任何 GIS 库）：

- `gdtest_poly.shp` 12 面（EPSG:4326，NAME/ID/VALUE；含双部件 L 形与带内环洞记录）；`gdtest_lines.shp` 8 线（含双部件）；
- `gdtest_dem.tif` 81×41 Float32（EPSG:32754，像元值=确定性公式 (x-OX)/res + 2(y-OY)/res + (px·py)%7）；
- PostGIS 表 `gdtest_poly`（12 行，与 shapefile 交叉比对两服务面一致）；
- 目录经 `GSD_TEST_DATA_DIR` 注入（须位于容器 data_dir 挂载内）；外部真实数据经 `GSD_REAL_DATA_DIR` 走泛化发现式检查（成对 .shp/.dbf 记录数交叉校验+发布+WFS 计数比对）。
- 环境变量族：`GSD_GEOSERVER_URL/GSD_GEOUSER/GSD_GEEPASS/GSD_TEST_PG_*/GSD_CONTAINER_DATA_DIR/GSD_SETTINGS_PATH`；GeoServer 不可达时集成用例经 SkipLog 显式登记跳过（xunit v2 无运行期 skip 的协议补偿）。
- 全部服务器资源 `gdtest` 前缀 + finally 显式删除 + 集合夹具结束孤儿扫描（保留共享 e2e 夹具 `gdtest_ws_e2e` 供跨进程复用）。

## 三、服务面端到端验证要点（L3/harness）

WFS 计数=文件头记录数、逐属性与 DBF 独立解析一致（PG 列名折叠大小写不敏感比对）、CQL/BBOX 过滤、重投影量级变换；WMS GetMap 尺寸/红色真值像素/GetFeatureInfo 命中，演示数据 topp:states 只读交叉验证；WCS 2.0.1 GetCoverage 回读 GeoTIFF 头 81×41+ProjCS32754+6 点像元套公式（容差 1e-3）；WMTS/GWC：capabilities→GetTile(PNG)→seed 任务→宿主缓存目录文件增量→truncate。

## 四、发现缺陷与修复终态（按推荐已全部处理）

**客户端库（GeoServerClient）**
- E41（阻断）：默认仅 `Accept: application/json` 致 3.x `POST /rest/styles` 500——已修（Accept 增 `text/plain;q=0.9`，实测 201）。
- 请求体 null 字段序列化致 GeoServer 建出**零维 coverage** 等——已修：新增 `Http/GeoServerJson.Request`（NullValueHandling.Ignore），全部服务请求序列化统一接入；零维 coverage 根因消除，RealData/Integration 走库路径复验。
- 认证编码 ASCII→UTF8（E31）；全部资源名路径段 `Uri.EscapeDataString`。
- `FeatureType.Enabled`→`bool?` 且默认 true（E27 建 FT 默认禁用消除）。
- 安全域按 3.0.1 真实路由/包装逐项修复（证据为控制器类与实测）：`authfilters` 全小写双层包装、`authproviders` 动态 Java 类名键 map（JToken 解析）、users/groups 列表为**直接对象数组**、POST users 显式 enabled（否则 500 NPE）、filterchain 单数路由、ACL 去 `.json`、roles 字符串数组（E7 对 roles 系误报）、`self/password` 扁平 `{"newPassword"}` 即真契约（E16 误报）；users/{name} 详情、组详情、keystore 在 3.0.1 无对应端点——方法明确化为 `NotSupportedException`+注释（不再静默 404 错路径）。
- GWC 域：layers/gridsets/blobstores 列表=**纯 JSON 数组**（RawStringArrayConverter）、gridset 单体 `{"gridSet"}`（大写 S）+extent.coords、diskquota 恒 XML（XDocument 读写往返）、seed/truncate 统一 `POST /gwc/rest/seed/{layer}` XML（zoomStart/Stop 必填）、masstruncate 按控制器形态；`/gwc/rest/settings` 3.0.1 无端点（注明）。
- 其它：templates/urlchecks/monitor 空态/动态类名键容错；GetGranules filter URL 转义（E24）；WMTS settings 根 `wmts` GET/PUT 均有效（E15 误报，翻转基线）；contact 单层平铺（E17 误报）+模型补齐字段；**GlobalSettings 根实为 `{"global":...}`**（原库读 `settings` 恒 null——修复并对 12 个 settings 模型类加 ExtensionData 防丢键，SystemIT 逐字段快照往返）；reload/reset 显式空 JSON 体；PreviewService workspace 转义/srs 缺省回退（E38）。
- imports/transforms/structuredcoverage：扩展未装（404），服务保留+注释，基线不翻转（属环境而非库缺陷）。

**App（Avalonia）**
- E40：StoresManagementViewModel 创建 shapefile 存储载荷修复为 `namespace + url`（约定 `file:<存储名>`，相对容器 data_dir，注释写明）、`Type="Shapefile"`、Description 落写；对话框新增"数据目录"可选输入（NewDataStoreDirectory）。Headless 翻转为创建后独立 GET 断言 enabled/type/url/description+删除闭环。
- E32：ConnectCommand 增加真实连通性探测（GetVersionAsync 失败→断开+本地化错误提示），错误 URL 现置 IsConnected=false。
- E37：CachingDefaults 假实现显式化——3.0.1 无 GWC 默认设置 REST 端点（实测 404 + 无库方法），Load/Save 如实提示"暂不可配置"，不再假装成功。
- E36：Security/Style/MapPreview 硬编码文案全部入 LocalizationService（新增约 25 键），中英切换生效。
- GlobalSettingsViewModel Save 改 GET-先读-改-PUT，配合 ExtensionData 保留模型外服务器键。
- E33（可测试性）：SettingsService 目录注入点（此前已修）。

**服务器行为（非产品缺陷，基线注释固化）**：GeoServer 3.0 移除工作空间级 `/{ws}/ows`；WCS 2.0.1 GeoTIFF 输出丢弃 ModelPixelScale/Tiepoint（Warn）；不受理 WCS 1.0.0 GetCoverage（Warn）；WMTS 矢量层默认 format=mvt（测试显式选 png）。

**测试基线策略**：修复项对应 KNOWN-ISSUE E## 全部翻转为 FIXED-E##（断言新正确行为）；不可用端点明确化为显式异常/注释；服务器侧行为保留现状基线并注明归因。

## 五、复现命令

```bash
# 数据生成（需 OSGeo4W 命令行）
python tests/testdata/generate_testdata.py && bash tests/testdata/load_postgis.sh
# 全量（离线层任何环境可跑；服务器不可达时集成层自动跳过并登记）
dotnet test src/GeoServerDesktop.Tests
# 真实数据 harness（Fail>0 退出码 1）
dotnet run --project src/GeoServerDesktop.RealDataHarness
```

## 六、变更清单

新增：`src/GeoServerDesktop.Tests`（Unit/Integration/RealData+RealDataTests/Headless/Infrastructure，共约 90 文件）、`src/GeoServerDesktop.RealDataHarness`、`tests/testdata/*`、本报告。
修改（修复轮）：GeoServerClient 全 45 服务序列化/路径/包装（Models/{Security,GeoWebCache,ServiceSettings,Settings,DataStore,FeatureType,…}、Services 全域、Http/GeoServerJson.cs 新增）；App：StoresManagement/MainWindow/CachingDefaults/SecuritySettings/Style/MapPreview/GlobalSettings 各 VM + LocalizationService + StoresManagementView.axaml + SettingsService 注入点；`GeoServerDesktop.sln`（+2 工程）。

---

# 第二轮：真实数据强化测试（2026-09-26）

被测环境不变（GeoServer 3.0.1 + PostGIS 17.5/3.5.2 + .NET 8），本轮把测试重心从"REST 契约面"推进到
**真实数据的属性/几何/编码/规模/损坏形态**，并补做 GeoServer 2.28.0 跨版本矩阵。

## 一、结果总览

| 项 | 上一轮基线 | 本轮 | 变化 |
|---|---|---|---|
| `dotnet test` 全量（L1+L2+L3+L4） | 692 通过 | **752 通过 / 0 失败** | +60 例 |
| 控制台 harness（无外部真实数据） | Pass=70 Warn=2 Fail=0 | **Pass=263 Warn=54 Fail=0**（挂真实数据全段，退出码 0） | +193 检查 |
| harness（同口径 4 段，3.0.1） | — | Pass=129 Warn=52 Fail=0 | 基准 |
| harness（同口径 4 段，2.28.0） | — | Pass=126 Warn=52 **Fail=3**（仅栅格 WMTS） | Warn 集合逐项相同 |

54 项 Warn（分段复跑时为 47/5 两级）全部是**有证据的服务端契约**（见第四节），不是待修的客户端缺陷。

## 二、新增真实测试数据（确定性、数据无关）

`tests/testdata/generate_testdata.py` 现会一并调用 `generate_testdata_extra.py`，把扩展数据写入
挂载根下的同级目录（**不改动既有 gdtest_data**，因此既有 L1–L4 基线不受影响）：

| 目录 | 覆盖形态 |
|---|---|
| `gdtest_vec` | 字段类型全覆盖（String/Integer/Real/**Date**/**Logical**/254 宽文本/**NULL**）；**DBF 编码三变体**（UTF-8+.cpg / GBK+.cpg / GBK 无 .cpg，同逻辑内容）；Point+**NULL 几何**；PolygonZ；**自相交 bowtie**；**0 记录**层；**4001 顶点/环**×3；**CJK 与含空格基名** |
| `gdtest_img` | 3 波段 Byte + nodata 块；Int16 **瓦片化 + 外部概览 + 统计**；**CJK 含空格文件名**栅格 |
| `gdtest_vol` | 2 万点大表（分页 / 计时基线） |
| `gdtest_bad` | 缺 .dbf、SHP/DBF 记录数不符、非法 .prj、缺 .prj、SHP 截断、只有 .dbf、以及同目录合法正对照 |

PostGIS 侧 `load_postgis.sh` 追加加载 `gdtest_types`、`gdtest_pts`（2 万点）。
所有期望值仍由文件本体独立推导（新增 `DbfTable`、`GeometryDerive`、`TiffSample` 三个纯解析器，不经 GDAL/JTS）。

## 三、本轮发现并已修复的客户端缺陷（按推荐全部处理）

- **E42 shapefile 属性编码/投影声明无预检**：GeoServer 读属性表只认 `.cpg`（无声明则按平台默认
  ISO-8859-1），缺声明的中文数据发布后属性面**静默乱码**。库层 `GeoFileInspector` 新增
  `CpgEncoding / DbfHasNonAscii / DbfLooksUtf8 / EncodingRisk / CrsRisk / Warnings` 与纯函数
  `IsValidUtf8 / ScanDbfTextBytes / EvaluateEncodingRisk / EvaluateCrsRisk`（无新增依赖）；
  向导第 1/2 步预检与发布结果都会把风险呈现到用户可见文案（新增中英 resx 键）。
- **E43 损坏 shapefile 被报"发布成功"**：实测 GeoServer 对缺 .dbf、SHP 头长与实际不符、DBF 记录数不符、
  .prj 非法 WKT 一律返回 201，缺陷要等服务查询才暴露。新增 `ShapefileIntegrityRisk`（全部 O(1) 文件头判据）
  与 `ImportSourceRequest.LocalSourcePath` 发布前闸门：**阻断级问题直接失败并给出原因**，
  非阻断项进 `PublishResult.Warnings/PreflightErrors`。
- **E44 发布 2xx ≠ 图层可用**：真实数据实测——服务端匹配不到磁盘文件时会接受请求却建出**只有几何字段的
  空要素类型**（REST 里资源存在、WFS 无数据）。`PublishShapefileAsync` 增加发布后回读校验
  （存在 + 启用 + 属性面含业务字段），失败时返回可诊断原因。
- **E45 FeatureType 模型缺属性面**：新增 `Attributes/FeatureAttributeInfo` 与
  `FeatureTypeAttributeConverter`（读 `{"attribute":[…]}`/裸数组/单对象/缺省）。
  写侧显式 `CanWrite => false`：自定义 WriteJson 会抢在 `NullValueHandling.Ignore` 之前执行，
  使创建请求恒定携带 `attributes` 包装体——**实测 GeoServer 2.28 对该形态直接 500**（3.0.1 容忍）。

- **E46 服务端 HTML 错误页被原样抛给用户**：GeoServer 并非总是返回自身 JSON/XML——容器层（Tomcat）会直接给出
  HTML 错误页，早前实现把整页 HTML 截断 200 字塞进 `Message`，用户读不懂且真正原因（藏在 `<p><b>Message</b> …`）被丢掉。
  新增基类统一实现 `ServiceBase.Describe`（JSON 取 detail/message/error/title 并还原转义、OGC XML 取 ExceptionText、
  HTML 抽取 Message 段并去标签折叠空白、无 body 只留状态码、超长加省略号），同时消掉
  `ImportWizardService` / `BatchOperationService` / `WorkspaceMigrationService` 三处重复的私有 `Describe`。

测试基线：以上各项均有 L1（离线矩阵）、L4（预检到达用户文案，断言取与语言无关的证据串）、
L3/harness（真实文件 + 真实实例）三层覆盖；`ResidueCheckTests` 与 harness `Audit` 段验证零残留。

## 四、真实数据暴露的服务端契约（固化为 Warn 基线，非客户端缺陷）

1. **无 `.cpg` 的非 ASCII DBF** → 服务端按 ISO-8859-1 解码，WFS 属性面为乱码（`ÄÏ³äÊÐ` ⇄ 南充市）。
   客户端无法代为改正，只能预检告知（E42）。
2. **`.prj` 无 EPSG 权威码（自定义投影）图层的 GeoJSON 输出不可靠**：写完要素后流尾部 NPE
   （`Cannot invoke "String.indexOf(String)" because "identifier" is null`），响应变成
   *JSON 前缀 + XML 异常* 或被直接掐断（解析失败），**要素静默丢失**；同一请求带
   `srsName=EPSG:4326` 即完整。CQL 空间过滤在该类图层上同样触发此缺陷。
   harness 以 `JsonStream/*` 固化，并把几何面改走 **GML 通道**（原生坐标、响应完整）。
3. **EPSG:4326 图层的裸 CQL 几何字面量按 (lat,lon) 解释**：`INTERSECTS(the_geom,POINT(lon lat))`
   静默漏检，`SRID=4326;POINT(lon lat)` 才按 (lon,lat)；请求级 `BBOX` 需带 `,EPSG:4326` 后缀。
   由 `Axis/*` 契约项钉住（客户端生成过滤器/预览 URL 时必须显式声明）。
4. **`resultType=hits` 时即使指定 `outputFormat=application/json` 也返回 GML**（既有计数逻辑按文本解析，符合预期）。
5. **WCS 2.0.1 DescribeCoverage 的 `gmlcov:rangeType` 内 `swe:DataRecord` 为空**：波段数/波段名不经
   WCS 元数据宣告，必须由 GetCoverage 输出核对（本轮即以此法验证 3 波段 Byte 与 Int16 瓦片栅格逐点一致）。
6. **`grid`/`nativeCRS` 形态**：REST coverage 的 `nativeCRS` 是对象 `{"@class":"projected","$":"WKT…"}`
   （历史缺陷：按字符串解析恒空），`grid.range.high` 给网格尺寸、`grid.crs` 给 EPSG。

## 五、几何保真判据的方法学结论

逐点顶点计数**不是**稳定契约：服务端会把 Polygon 归一为 MultiPolygon、补齐环闭合点、合并重复点。
真实数据的几何面因此采用**拓扑保持**判据（唯一顶点集合 + 包络框 + 部件数，对环起点旋转与闭合不敏感），
逐点严格比对只保留给自己构造、已声明 EPSG 的数据集（用于证明"服务未静默抽稀"）。

## 六、GeoServer 2.28.0 兼容矩阵（KNOWN-ISSUES 第四节"待回归"→ 已实测）

用同一份数据目录、同一套检查在 `docker.osgeo.org/geoserver:2.28.0` 上重跑：

- **数据面结论一致**：Warn 集合与 3.0.1 逐项相同（编码降级、GeoJSON 流契约、CQL 轴序、WCS rangeType 等），
  说明本轮客户端修复在 2.x/3.x 上语义一致。
- **唯一新增失败**：栅格图层的 WMTS `GetTile` 返回 400（GWC 1.28 未把该 coverage 注册为可瓦片图层）→
  矢量/栅格 WMTS 能力在 2.x 需按实测重新对齐。
- **REST 契约面已定位差异**（3.0.1 基线的显式钉值，非缺陷）：`/rest/about/version` 版本字面量、
  monitor 请求列表路径（2.28 为 404）、GWC diskquota XML 形态。
- **跨版本测试夹具注意**：把 3.0.1 的 data_dir 直接复制给 2.28 会因配置版本漂移产生大量 500 假信号，
  必须用**该版本自己初始化的空 data_dir**；此外 namespace 连接参数与 `attributes` 包装体在 2.28 上的
  严格性差异已在 E45 中记录。

## 七、复现命令（更新）

```bash
# 数据生成（含扩展数据集；需 OSGeo4W 或 gdal-bin + numpy）
python tests/testdata/generate_testdata.py && bash tests/testdata/load_postgis.sh
# 全量（L1–L4；服务器/数据缺失自动跳过并登记 SkipLog）
dotnet test src/GeoServerDesktop.Tests
# 真实数据 harness（Fail>0 → 退出码 1）；支持段过滤与现场保留
dotnet run --project src/GeoServerDesktop.RealDataHarness
GSD_REAL_DATA_DIR="<挂载内真实数据目录>" dotnet run --project src/GeoServerDesktop.RealDataHarness -- --only ext,raster,extfid,audit
GSD_KEEP=1 ... --only diag        # 把服务响应原文落盘（定位用）
```
CI 的 integration job 已挂载 `gdtest_vec/gdtest_img/gdtest_vol/gdtest_bad`，扩展检查在 CI 内同样真实执行。

