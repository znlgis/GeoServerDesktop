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
