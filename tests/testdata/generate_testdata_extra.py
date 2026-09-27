#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
GeoServerDesktop 扩展真实数据测试集生成器（由 generate_testdata.py 统一调用，也可单跑）。

设计约束（与主生成器同源）：
  · 确定性——坐标与属性全部由公式给出，无随机源，重复运行字节级可比；
  · 期望值自描述——C# 侧一律从 .shp/.dbf/.prj/.cpg/.tif 文件本体独立解析推导，
    本脚本不做“给测试端抄答案”的硬编码期望值（manifest 仅作人类可读说明与交叉核对）；
  · 与既有 fixture 隔离——新数据写入 gdtest_data 的同级目录，避免改变既有 shapefile
    目录存储的扫描结果，从而不影响 L1/L2/L3 既有基线。

覆盖的易错形态（本轮测试缺口）：
  vec/  字段类型全覆盖（String/Integer/Real/Date/Boolean + 空值 + 254 宽文本 + 中文值）
        DBF 编码三变体（UTF-8+.cpg / GBK+.cpg / GBK 无 .cpg）——同逻辑内容，仅物理编码不同
        Point + NULL 几何 / PolygonZ（维数透传）/ 自相交（bowtie）/ 0 记录 / 超高顶点环
        CJK 与含空格基名
  img/  多波段 Byte + nodata、Int16 瓦片化 + 概览 + 统计、CJK 含空格文件名栅格
  vol/  2 万点大表（分页、maxFeatures、计时基线）
  bad/  缺 .dbf、SHP/DBF 记录数不符、非法 .prj、头长超出实际、只有 .dbf 无 .shp

用法: python generate_testdata_extra.py [gdtest_data 目录]   （环境变量 GSD_TEST_DATA_DIR 优先）
依赖: OSGeo4W（osgeo.ogr / osgeo.gdal / numpy）。
"""
import json
import math
import os
import shutil
import struct
import sys

try:
    from osgeo import gdal, ogr, osr
except ImportError as exc:                                # CI/最小环境缺 GDAL Python 绑定：跳过扩展数据集
    print("SKIP 扩展数据集生成（缺 GDAL Python 绑定：%s）；安装：apt install python3-gdal python3-numpy 或 OSGeo4W" % exc)
    sys.exit(0)

gdal.UseExceptions()
ogr.UseExceptions()

DATA_DIR = os.environ.get("GSD_TEST_DATA_DIR") or (sys.argv[1] if len(sys.argv) > 1 else None) \
    or r"D:\self\tool\docker\data\geoserver\gdtest_data"
ROOT = os.path.dirname(DATA_DIR.rstrip("\\/"))          # 挂载根（与 DataEnv.ContainerDataRoot 同语义）
VEC = os.path.join(ROOT, "gdtest_vec")
IMG = os.path.join(ROOT, "gdtest_img")
VOL = os.path.join(ROOT, "gdtest_vol")
BAD = os.path.join(ROOT, "gdtest_bad")

MANIFEST = {"datasets": {}}


def reset_dirs():
    for d in (VEC, IMG, VOL, BAD):
        shutil.rmtree(d, ignore_errors=True)
        os.makedirs(d, exist_ok=True)


def shp_path(directory, base):
    return os.path.join(directory, base + ".shp")


def create_layer(path, layer_base, geom_type, encoding=None, srs_wkt=None):
    """新建 shapefile 数据源与图层；encoding 非空时写 .cpg。"""
    ds = ogr.GetDriverByName("ESRI Shapefile").CreateDataSource(path)
    srs = None
    if srs_wkt:
        srs = osr.SpatialReference()
        srs.ImportFromWkt(srs_wkt)
    opts = ["ENCODING=" + encoding] if encoding else None
    ly = ds.CreateLayer(layer_base, geom_type=geom_type, options=opts)
    return ds, ly


def add_fields(ly, spec):
    """spec: [(名称, ogr 类型, 宽度, 精度, 子类型)]——子类型 OFSTBoolean 令 shapefile 写 'L' 逻辑型。"""
    for item in spec:
        name, typ, width, prec = item[:4]
        subtype = item[4] if len(item) > 4 else None
        fd = ogr.FieldDefn(name, typ)
        if width:
            fd.SetWidth(width)
        if prec is not None:
            fd.SetPrecision(prec)
        if subtype is not None:
            fd.SetSubType(subtype)
        ly.CreateField(fd)
    return ly


TYPE_SPEC = [
    ("NAME", ogr.OFTString, 40, None),
    ("ID", ogr.OFTInteger, 11, None),
    ("NUM", ogr.OFTReal, 24, 6),
    ("DT", ogr.OFTDate, 8, None),
    ("FLAG", ogr.OFTInteger, 1, None, ogr.OFSTBoolean),   # DBF 'L' 逻辑型
    ("TXT", ogr.OFTString, 254, None),
    ("NOTE", ogr.OFTString, 20, None),
]

WGS84_WKT = 'GEOGCS["WGS 84",DATUM["WGS_1984",SPHEROID["WGS 84",6378137,298.2572201439]],PRIMEM["Greenwich",0],UNIT["degree",0.0174532925199433]]'


def rect_wkt(x0, y0, x1, y1):
    return "POLYGON((%g %g,%g %g,%g %g,%g %g,%g %g))" % (x0, y0, x1, y0, x1, y1, x0, y1, x0, y0)


def feat_from(ly, wkt, values):
    f = ogr.Feature(ly.GetLayerDefn())
    if wkt is None:
        f.SetGeometry(None)
    else:
        f.SetGeometry(ogr.CreateGeometryFromWkt(wkt))
    for k, v in values.items():
        if v is not None:
            f.SetField(k, v)
    ly.CreateFeature(f)
    f = None


# ---------------------------------------------------------------- 1. 字段类型全覆盖
def gen_types():
    p = shp_path(VEC, "gdtest_types")
    ds, ly = create_layer(p, "gdtest_types", ogr.wkbPolygon, encoding="UTF-8", srs_wkt=WGS84_WKT)
    add_fields(ly, TYPE_SPEC)
    rows = []
    for i in range(6):
        values = {
            "NAME": "类型要素_%02d" % i,
            "ID": 700 + i,
            "NUM": round(3.5 * i + 0.25, 6),
            "DT": "202%d-%02d-%02d" % (i % 10, (i % 9) + 1, (i * 7) % 28 + 1),
            "FLAG": 1 if i % 2 == 0 else 0,
            "TXT": ("字母" + "abc" * 20)[:120] + "_%d" % i,
            "NOTE": None if i % 3 == 0 else "备注%d" % i,       # 0/3 为 NULL
        }
        feat_from(ly, rect_wkt(i * 1.0, 0.0, i * 1.0 + 0.8, 0.8), values)
        rows.append(values)
    ds.FlushCache(); ds = None
    MANIFEST["datasets"]["gdtest_types"] = {
        "dir": "gdtest_vec", "records": 6, "geometry": "Polygon", "crs": "EPSG:4326",
        "encoding": "UTF-8 (.cpg)", "null_note": "NOTE 在 i%3==0 为 NULL；geom 索引 3/7 无（见 points）",
        "features": rows}


# ---------------------------------------------------------------- 2. DBF 编码三变体
ENC_NAMES = ["北京市", "河北省", "山西省", "内蒙古自治区"]
ENC_ADM = ["中华人民共和国", "中华人民共和国", "中华人民共和国", "中华人民共和国"]
ENC_SPEC = [("NAME", ogr.OFTString, 60, None), ("ADM", ogr.OFTString, 60, None),
            ("ID", ogr.OFTInteger, 11, None)]


def gen_enc_variant(base, encoding, keep_cpg):
    p = shp_path(VEC, base)
    ds, ly = create_layer(p, base, ogr.wkbPolygon, encoding=encoding, srs_wkt=WGS84_WKT)
    add_fields(ly, ENC_SPEC)
    for i, nm in enumerate(ENC_NAMES):
        feat_from(ly, rect_wkt(i * 1.0, 0.0, i * 1.0 + 0.9, 0.9),
                  {"NAME": nm, "ADM": ENC_ADM[i], "ID": 900 + i})
    ds.FlushCache(); ds = None
    cpg = os.path.join(VEC, base + ".cpg")
    if not keep_cpg and os.path.exists(cpg):
        os.remove(cpg)
    MANIFEST["datasets"][base] = {
        "dir": "gdtest_vec", "records": len(ENC_NAMES), "geometry": "Polygon",
        "dbf_encoding": encoding, "cpg": "present" if os.path.exists(cpg) else "absent",
        "expected_names": ENC_NAMES,
        "note": "同一逻辑内容仅物理编码不同；期望值＝按声明编码解 DBF 字节"}


# ---------------------------------------------------------------- 3. Point + NULL 几何
def gen_points():
    p = shp_path(VEC, "gdtest_points")
    ds, ly = create_layer(p, "gdtest_points", ogr.wkbPoint, encoding="UTF-8", srs_wkt=WGS84_WKT)
    add_fields(ly, [("NAME", ogr.OFTString, 30, None), ("ID", ogr.OFTInteger, 11, None)])
    for i in range(9):
        geom = None if i in (3, 7) else "POINT(%g %g)" % (i * 0.5, (i % 4) * 0.5)   # 2 条 NULL 几何
        feat_from(ly, geom, {"NAME": "pts_%02d" % i, "ID": 800 + i})
    ds.FlushCache(); ds = None
    MANIFEST["datasets"]["gdtest_points"] = {
        "dir": "gdtest_vec", "records": 9, "null_geometry": [3, 7], "geometry": "Point",
        "crs": "EPSG:4326", "note": "NULL 几何记录应出现在属性面但不出现在几何面"}


# ---------------------------------------------------------------- 4. PolygonZ（维数透传）
def gen_polyz():
    p = shp_path(VEC, "gdtest_polyz")
    ds, ly = create_layer(p, "gdtest_polyz", ogr.wkbPolygon25D, encoding="UTF-8", srs_wkt=WGS84_WKT)
    add_fields(ly, [("NAME", ogr.OFTString, 30, None), ("ID", ogr.OFTInteger, 11, None),
                    ("ZMIN", ogr.OFTReal, 24, 3), ("ZMAX", ogr.OFTReal, 24, 3)])
    for i in range(4):
        base_z = 10.0 * i
        pts = []
        for k, (x, y) in enumerate([(0, 0), (1, 0), (1, 1), (0, 1), (0, 0)]):
            pts.append("%g %g %g" % (x + i * 2, y, base_z + k * 5))
        wkt = "POLYGON Z((%s))" % ",".join(pts)
        feat_from(ly, wkt, {"NAME": "polyz_%02d" % i, "ID": 600 + i,
                            "ZMIN": base_z, "ZMAX": base_z + 20})
    ds.FlushCache(); ds = None
    MANIFEST["datasets"]["gdtest_polyz"] = {
        "dir": "gdtest_vec", "records": 4, "geometry": "PolygonZ", "shape_type": 15,
        "z_formula": "第 i 条：z=10i+5k（k 为环顶点序号 0..4）", "crs": "EPSG:4326"}


# ---------------------------------------------------------------- 5. 自相交（bowtie）
def gen_selfint():
    p = shp_path(VEC, "gdtest_selfint")
    ds, ly = create_layer(p, "gdtest_selfint", ogr.wkbPolygon, encoding="UTF-8", srs_wkt=WGS84_WKT)
    add_fields(ly, [("NAME", ogr.OFTString, 30, None), ("ID", ogr.OFTInteger, 11, None)])
    bowtie = "POLYGON((0 0,2 2,2 0,0 2,0 0))"                       # 有向面积=0 的八字形
    feat_from(ly, bowtie, {"NAME": "si_bowtie", "ID": 500})
    feat_from(ly, rect_wkt(4, 0, 5, 1), {"NAME": "si_valid", "ID": 501})
    ds.FlushCache(); ds = None
    MANIFEST["datasets"]["gdtest_selfint"] = {
        "dir": "gdtest_vec", "records": 2, "geometry": "Polygon",
        "note": "si_bowtie 自相交（shoelace 有向面积≈0），si_valid 为对照"}


# ---------------------------------------------------------------- 6. 0 记录
def gen_empty():
    p = shp_path(VEC, "gdtest_empty")
    ds, ly = create_layer(p, "gdtest_empty", ogr.wkbPolygon, encoding="UTF-8", srs_wkt=WGS84_WKT)
    add_fields(ly, [("NAME", ogr.OFTString, 30, None), ("ID", ogr.OFTInteger, 11, None)])
    ds.FlushCache(); ds = None
    MANIFEST["datasets"]["gdtest_empty"] = {"dir": "gdtest_vec", "records": 0, "geometry": "Polygon",
                                            "note": "合法文件、零记录（bbox 全 0）"}


# ---------------------------------------------------------------- 7. 超高顶点环
HUGE_VERTS = 4000        # 每条记录唯一顶点数（闭合重复点另计）


def huge_ring(cx, cy, r0, amp, k):
    pts = []
    for n in range(HUGE_VERTS):
        th = 2 * math.pi * n / HUGE_VERTS
        r = r0 + amp * math.sin(th * k)
        pts.append("%.6f %.6f" % (cx + r * math.cos(th), cy + r * math.sin(th)))
    pts.append(pts[0])
    return "POLYGON((%s))" % ",".join(pts)


def gen_huge():
    p = shp_path(VEC, "gdtest_huge")
    ds, ly = create_layer(p, "gdtest_huge", ogr.wkbPolygon, encoding="UTF-8", srs_wkt=WGS84_WKT)
    add_fields(ly, [("NAME", ogr.OFTString, 30, None), ("ID", ogr.OFTInteger, 11, None),
                    ("VERTS", ogr.OFTInteger, 11, None)])
    for i in range(3):
        wkt = huge_ring(10.0 * i, 0.0, 2.0, 0.5, 7 + i)
        feat_from(ly, wkt, {"NAME": "huge_%02d" % i, "ID": 400 + i, "VERTS": HUGE_VERTS + 1})
    ds.FlushCache(); ds = None
    MANIFEST["datasets"]["gdtest_huge"] = {
        "dir": "gdtest_vec", "records": 3, "geometry": "Polygon",
        "vertices_per_record": HUGE_VERTS + 1,
        "ring_formula": "极坐标 r=r0+amp*sin(k*θ)，θ 均分 4000 份，r0=2，amp=0.5，k=7+i，中心 (10i,0)"}


# ---------------------------------------------------------------- 8. CJK 与含空格基名
def gen_cjk_name():
    p = shp_path(VEC, "湖泊 与 水库")
    ds, ly = create_layer(p, "湖泊 与 水库", ogr.wkbPolygon, encoding="UTF-8", srs_wkt=WGS84_WKT)
    add_fields(ly, [("NAME", ogr.OFTString, 40, None), ("ID", ogr.OFTInteger, 11, None)])
    for i in range(2):
        feat_from(ly, rect_wkt(i * 1.0, 8.0, i * 1.0 + 0.5, 8.5),
                  {"NAME": "湖泊_%d" % i, "ID": 300 + i})
    ds.FlushCache(); ds = None
    MANIFEST["datasets"]["湖泊 与 水库"] = {
        "dir": "gdtest_vec", "records": 2, "geometry": "Polygon",
        "note": "基名含中文与空格：nativeName/发布名/REST 与 WFS typeName 转义全链路"}


# ---------------------------------------------------------------- 9. 大表（分页/计时）
def gen_big_points():
    p = shp_path(VOL, "gdtest_pts20k")
    ds, ly = create_layer(p, "gdtest_pts20k", ogr.wkbPoint, encoding="UTF-8", srs_wkt=WGS84_WKT)
    add_fields(ly, [("ID", ogr.OFTInteger, 11, None), ("GRP", ogr.OFTInteger, 11, None)])
    n = 20000
    for i in range(n):
        x, y = i % 200, i // 200
        feat_from(ly, "POINT(%g %g)" % (x * 0.01, y * 0.01), {"ID": i, "GRP": i % 7})
    ds.FlushCache(); ds = None
    MANIFEST["datasets"]["gdtest_pts20k"] = {
        "dir": "gdtest_vol", "records": n, "geometry": "Point",
        "formula": "第 i 点 (x,y)=(i%200*0.01, i//200*0.01)，GRP=i%7"}


# ---------------------------------------------------------------- 10. 栅格
EPSG_UTM54N = 32754


def new_raster(path, width, height, bands, dtype, creation_opts):
    driver = gdal.GetDriverByName("GTiff")
    ds = driver.Create(path, width, height, bands, dtype, creation_opts)
    srs = osr.SpatialReference()
    srs.ImportFromEPSG(EPSG_UTM54N)
    ds.SetProjection(srs.ExportToWkt())
    return ds


def rgb_values(px, py, band):
    if band == 1:
        return (px * 3 + py) % 251 + 1
    if band == 2:
        return (px + py * 5) % 241 + 10
    # band3：左上一个 8×6 的 nodata 块（值 0 = nodata）
    if px < 8 and py < 6:
        return 0
    return (px * py) % 200 + 50


def int16_values(px, py):
    if px < 10 and py < 10:
        return -9999                                  # nodata
    return (px * py) % 3000 - 500


def gen_rgb():
    path = os.path.join(IMG, "gdtest_rgb.tif")
    w, h = 64, 48
    ds = new_raster(path, w, h, 3, gdal.GDT_Byte, ["BIGTIFF=NO"])
    import numpy as np
    xs = np.arange(w)[None, :].repeat(h, 0)
    ys = np.arange(h)[:, None].repeat(w, 1)
    for b in range(1, 4):
        arr = np.vectorize(lambda x, y, b=b: int(rgb_values(int(x), int(y), b)))(xs, ys).astype("uint8")
        band = ds.GetRasterBand(b)
        band.SetNoDataValue(0)
        band.WriteArray(arr)
        band.FlushCache()
    ds.SetGeoTransform([500000.0, 100.0, 0.0, 2200060.0, 0.0, -100.0])
    ds = None
    MANIFEST["datasets"]["gdtest_rgb.tif"] = {
        "dir": "gdtest_img", "width": w, "height": h, "bands": 3, "dtype": "Byte",
        "nodata": 0, "nodata_block": "band3 px<8 and py<6",
        "crs": "EPSG:%d" % EPSG_UTM54N, "origin": [500000.0, 2200060.0], "res": 100.0,
        "formulas": ["b1=(px*3+py)%251+1", "b2=(px+py*5)%241+10", "b3=(px*py)%200+50 (px<8&py<6 → nodata)"]}


def gen_int16():
    path = os.path.join(IMG, "gdtest_int16.tif")
    w, h = 200, 200
    ds = new_raster(path, w, h, 1, gdal.GDT_Int16, ["TILED=YES", "BLOCKXSIZE=64", "BLOCKYSIZE=64"])
    import numpy as np
    xs = np.arange(w)[None, :].repeat(h, 0)
    ys = np.arange(h)[:, None].repeat(w, 1)
    arr = np.vectorize(lambda x, y: int(int16_values(int(x), int(y))))(xs, ys).astype("int16")
    band = ds.GetRasterBand(1)
    band.SetNoDataValue(-9999)
    band.WriteArray(arr)
    ds.SetGeoTransform([500000.0, 50.0, 0.0, 2200060.0, 0.0, -50.0])
    ds.FlushCache()
    stats = band.GetStatistics(True, True)            # 触发统计写入
    ds = None
    gdal.SetConfigOption("GDAL_NUM_THREADS", "ALL_CPUS")
    d = gdal.Open(path)
    d.BuildOverviews("AVERAGE", [2, 4])               # 外部 .ovr，保持基文件可独立解析
    d = None
    MANIFEST["datasets"]["gdtest_int16.tif"] = {
        "dir": "gdtest_img", "width": w, "height": h, "bands": 1, "dtype": "Int16",
        "nodata": -9999, "tiled": "64x64", "overviews": [2, 4], "crs": "EPSG:%d" % EPSG_UTM54N,
        "origin": [500000.0, 2200060.0], "res": 50.0, "statistics": list(stats),
        "formulas": ["v=(px*py)%3000-500 (px<10&py<10 → nodata -9999)"]}


def gen_cjk_tif():
    path = os.path.join(IMG, "中文 栅格.tif")
    w, h = 32, 32
    ds = new_raster(path, w, h, 1, gdal.GDT_Byte, [])
    for py in range(h):
        row = bytes([(px * 2 + py * 3) % 256 for px in range(w)])
        ds.GetRasterBand(1).WriteRaster(0, py, w, 1, row)
    ds.SetGeoTransform([500000.0, 200.0, 0.0, 2200060.0, 0.0, -200.0])
    ds = None
    MANIFEST["datasets"]["中文 栅格.tif"] = {
        "dir": "gdtest_img", "width": w, "height": h, "bands": 1, "dtype": "Byte",
        "crs": "EPSG:%d" % EPSG_UTM54N, "origin": [500000.0, 2200060.0], "res": 200.0,
        "note": "文件名含中文与空格：coverageStore/发布/WCS 全链路转义",
        "formulas": ["v=(px*2+py*3)%256"]}


# ---------------------------------------------------------------- 11. 脏数据（负路径）
def clone_pair(src_dir, base, dst_dir, dst_base):
    for ext in (".shp", ".shx", ".dbf", ".prj", ".cpg"):
        s = os.path.join(src_dir, base + ext)
        if os.path.exists(s):
            shutil.copy(s, os.path.join(dst_dir, dst_base + ext))


def gen_bad_data():
    # 11.1 缺 .dbf
    clone_pair(VEC, "gdtest_types", BAD, "bd_nodbf")
    os.remove(os.path.join(BAD, "bd_nodbf.dbf"))

    # 11.2 SHP/DBF 记录数不符（DBF 头计数改小）
    clone_pair(VEC, "gdtest_types", BAD, "bd_mismatch")
    dbf = os.path.join(BAD, "bd_mismatch.dbf")
    b = bytearray(open(dbf, "rb").read())
    struct.pack_into("<I", b, 4, 3)                   # 头声明 3 条，SHP 实际 6 条
    open(dbf, "wb").write(bytes(b))

    # 11.3 非法 .prj
    clone_pair(VEC, "gdtest_points", BAD, "bd_badprj")
    open(os.path.join(BAD, "bd_badprj.prj"), "w", encoding="utf-8").write("PROJCS[broken,,,,,###not-wkt")

    # 11.4 无 .prj
    clone_pair(VEC, "gdtest_points", BAD, "bd_noprj")
    if os.path.exists(os.path.join(BAD, "bd_noprj.prj")):
        os.remove(os.path.join(BAD, "bd_noprj.prj"))

    # 11.5 SHP 被截断（头声明长度 > 实际）
    clone_pair(VEC, "gdtest_types", BAD, "bd_truncated")
    shp = os.path.join(BAD, "bd_truncated.shp")
    raw = open(shp, "rb").read()
    open(shp, "wb").write(raw[:int(len(raw) * 0.6)])

    # 11.6 只有 .dbf 无 .shp
    clone_pair(VEC, "gdtest_types", BAD, "bd_dbfonly")
    for ext in (".shp", ".shx", ".prj", ".cpg"):
        f = os.path.join(BAD, "bd_dbfonly" + ext)
        if os.path.exists(f):
            os.remove(f)

    # 11.7 正对照：脏数据目录内的合法对（发布应成功）
    clone_pair(VEC, "gdtest_types", BAD, "bd_valid")

    MANIFEST["datasets"]["bad/*"] = {
        "dir": "gdtest_bad",
        "cases": ["bd_nodbf 缺属性表", "bd_mismatch SHP6/DBF3 记录数不符", "bd_badprj 非法 WKT",
                  "bd_noprj 缺投影定义", "bd_truncated 头长超实际", "bd_dbfonly 只有属性表",
                  "bd_valid 合法正对照"],
        "note": "负路径要求：可诊断失败（Success=false + 可读原因），不得挂死/未分类 500"}


# ---------------------------------------------------------------- 主流程
def main():
    reset_dirs()
    gen_types()
    gen_enc_variant("enc_utf8", "UTF-8", keep_cpg=True)
    gen_enc_variant("enc_gbk_cpg", "GBK", keep_cpg=True)
    gen_enc_variant("enc_gbk_nocpg", "GBK", keep_cpg=False)
    gen_points()
    gen_polyz()
    gen_selfint()
    gen_empty()
    gen_huge()
    gen_cjk_name()
    gen_big_points()
    gen_rgb()
    gen_int16()
    gen_cjk_tif()
    gen_bad_data()

    manifest_file = os.path.join(DATA_DIR, "manifest_extra.json")
    with open(manifest_file, "w", encoding="utf-8") as f:
        json.dump(MANIFEST, f, ensure_ascii=False, indent=2)

    counts = {}
    for d in (VEC, IMG, VOL, BAD):
        counts[os.path.basename(d)] = sorted(os.listdir(d))
    print("扩展数据集写入：")
    for k, v in counts.items():
        print("  %s: %d 文件" % (k, len(v)))
    print("manifest:", manifest_file)


if __name__ == "__main__":
    main()
