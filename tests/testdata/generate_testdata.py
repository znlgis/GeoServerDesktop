#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
GeoServerDesktop 测试数据生成器。
期望值由生成参数确定性推导；文件本体经 OSGeo4W GDAL 转换保证规范合法。

用法: python generate_testdata.py [输出目录]   （环境变量 GSD_TEST_DATA_DIR 优先）
依赖: PATH 中的 ogr2ogr / gdal_translate（OSGeo4W）。
"""
import json, os, subprocess, sys

OUT = os.environ.get("GSD_TEST_DATA_DIR") or (sys.argv[1] if len(sys.argv) > 1 else None) \
    or r"D:\self\tool\docker\data\geoserver\gdtest_data"
os.makedirs(OUT, exist_ok=True)
SRC = os.path.join(OUT, "_src")
os.makedirs(SRC, exist_ok=True)

def run(cmd):
    r = subprocess.run(cmd, shell=True, capture_output=True, text=True)
    if r.returncode != 0:
        print("CMD FAIL:", cmd, "\n", r.stdout, r.stderr); sys.exit(1)
    return r

def sh(cmd): return run(cmd)

# ---------------- 数据集定义（确定性公式，测试端据此独立校验） ----------------
def rect(x0, y0, x1, y1):
    return [[x0, y0], [x1, y0], [x1, y1], [x0, y1], [x0, y0]]

POLY_META = [(f"poly_{i:02d}", i * 10 + 1, i * 2 + 0.5) for i in range(10)] \
    + [("poly_10", 101, 20.5), ("poly_11", 111, 22.5)]

def poly_geoms():
    geoms = []
    for i in range(10):
        x0, y0 = i, (i % 3) * 2
        geoms.append([rect(x0 + 0.1, y0 + 0.1, x0 + 0.9, y0 + 1.9)])
    geoms.append([rect(10.0, 4.0, 10.8, 5.8), rect(10.8, 4.0, 11.0, 4.8)])  # 双部件 L 形
    geoms.append([rect(0.0, 5.0, 1.0, 6.0), rect(0.25, 5.25, 0.75, 5.75)[::-1]])  # 带洞
    return geoms

LINE_GEOMS = [
    [[(0.0, 0.0), (11.0, 6.0)]],
    [[(0.0, 6.0), (11.0, 0.0)]],
    [[(1.0, 1.0), (2.0, 1.0), (2.0, 2.0)]],
    [[(3.0, 0.0), (3.5, 0.5), (4.0, 0.0), (4.5, 0.5)]],
    [[(5.0, 5.0), (6.0, 5.0)]],
    [[(7.0, 1.0), (7.0, 3.0), (8.0, 3.0), (8.0, 5.0)]],
    [[(9.0, 0.0), (9.0, 6.0), (10.0, 6.0)]],
    [[(0.2, 0.2), (0.8, 0.2)], [(0.2, 0.8), (0.8, 0.8)]],  # 双部件
]

def geojson_polygons():
    feats = []
    for geom, (name, idv, val) in zip(poly_geoms(), POLY_META):
        rings = [r + [r[0]] if r[0] != r[-1] else r for r in geom]
        if len(rings) == 1:
            g = {"type": "Polygon", "coordinates": rings}      # 单部件
        else:
            g = {"type": "MultiPolygon", "coordinates": [[rg] for rg in rings]}  # 分离部件
        feats.append({"type": "Feature",
                      "properties": {"NAME": name, "ID": int(idv), "VALUE": float(val)},
                      "geometry": g})
    return json.dumps({"type": "FeatureCollection", "features": feats})

def geojson_lines():
    feats = []
    for i, parts in enumerate(LINE_GEOMS):
        feats.append({"type": "Feature", "properties": {"NAME": f"line_{i:02d}", "ID": (i + 1) * 3},
                      "geometry": {"type": "MultiLineString", "coordinates": parts}})
    return json.dumps({"type": "FeatureCollection", "features": feats})

W, H, RES = 81, 41, 100.0
OX, OY = 500000.0, 2200060.0     # 左上角 X / 顶部 Y（north-up）
EPSG_DEM = 32754                   # WGS84 / UTM zone 54N

def dem_value(px, py):
    x = OX + (px + 0.5) * RES
    y = OY - (py + 0.5) * RES
    return (x - OX) / RES + 2.0 * (y - OY) / RES + ((px * py) % 7)

def asc_grid():
    rows = []
    for py in range(H):
        rows.append(" ".join("%.6f" % dem_value(px, py) for px in range(W)))
    return ("ncols %d\nnrows %d\nxllcorner %.1f\nyllcorner %.1f\ncellsize %.1f\nNODATA_value -9999\n%s\n"
            % (W, H, OX, OY - H * RES, RES, "\n".join(rows)))

# ---------------- 经 GDAL 转换生成最终文件 ----------------
with open(os.path.join(SRC, "gdtest_poly.geojson"), "w", encoding="utf-8") as f: f.write(geojson_polygons())
with open(os.path.join(SRC, "gdtest_lines.geojson"), "w", encoding="utf-8") as f: f.write(geojson_lines())
with open(os.path.join(SRC, "gdtest_dem.asc"), "w", encoding="utf-8") as f: f.write(asc_grid())

sh('ogr2ogr -overwrite -lco ENCODING=UTF-8 "%s" "%s"' % (
    os.path.join(OUT, "gdtest_poly.shp"), os.path.join(SRC, "gdtest_poly.geojson")))
sh('ogr2ogr -overwrite -lco ENCODING=UTF-8 "%s" "%s"' % (
    os.path.join(OUT, "gdtest_lines.shp"), os.path.join(SRC, "gdtest_lines.geojson")))
sh('gdal_translate -a_srs EPSG:%d -of GTiff "%s" "%s"' % (
    EPSG_DEM, os.path.join(SRC, "gdtest_dem.asc"), os.path.join(OUT, "gdtest_dem.tif")))

manifest = {
    "gdtest_poly": {"records": len(POLY_META), "geometry": "Polygon", "crs": "EPSG:4326",
                    "bbox": [0.0, 0.0, 11.0, 6.0],
                    "features": [{"NAME": n, "ID": i, "VALUE": v} for n, i, v in POLY_META],
                    "notes": "poly_10 双部件 L 形；poly_11 带内环（洞）"},
    "gdtest_lines": {"records": len(LINE_GEOMS), "geometry": "LineString", "crs": "EPSG:4326",
                     "bbox": [0.0, 0.0, 11.0, 6.0],
                     "features": [{"NAME": f"line_{i:02d}", "ID": (i + 1) * 3} for i in range(len(LINE_GEOMS))],
                     "notes": "line_07 双部件"},
    "gdtest_dem.tif": {"width": W, "height": H, "res": RES, "origin_x": OX,
                       "origin_y_top": OY, "crs": f"EPSG:{EPSG_DEM}",
                       "value_formula": "(x-OX)/RES + 2*(y-OYtop)/RES + (px*py)%7，x=OX+(px+0.5)*RES，y=OYtop-(py+0.5)*RES"},
    "postgis": {"table_env": "GSD_TEST_PG_*", "layer": "gdtest_poly", "records": len(POLY_META)},
}
with open(os.path.join(OUT, "manifest.json"), "w", encoding="utf-8") as f:
    json.dump(manifest, f, ensure_ascii=False, indent=2)
print("generated in", OUT)
