#!/usr/bin/env bash
# 将生成的数据集加载进 PostGIS（期望值仍由文件头/DBF 独立推导，本脚本不产出任何期望值）。
# 连接参数与 xunit/harness 同源：GSD_TEST_PG_*（默认 127.0.0.1:5432 postgres/postgres/postgres）。
set -e
DATA="${GSD_TEST_DATA_DIR:-D:\\self\\tool\\docker\\data\\geoserver\\gdtest_data}"
ROOT="$(cd "$(dirname "$DATA")" && pwd)"
PGH="${GSD_TEST_PG_HOST:-127.0.0.1}"; PGP="${GSD_TEST_PG_PORT:-5432}"
PGD="${GSD_TEST_PG_DB:-postgres}"; PGU="${GSD_TEST_PG_USER:-postgres}"; PGW="${GSD_TEST_PG_PASS:-postgres}"
TABLE="${GSD_TEST_PG_TABLE:-gdtest_poly}"
PG="PG:host=$PGH port=$PGP dbname=$PGD user=$PGU password=$PGW"

ogr2ogr -overwrite -nlt PROMOTE_TO_MULTI -lco GEOMETRY_NAME=geom -nln "$TABLE" \
  "$PG" "$DATA/gdtest_poly.shp"
echo "loaded $TABLE"

# 扩展表：字段类型全覆盖（与 shapefile/DBF 做三方属性保真）
if [ -f "$ROOT/gdtest_vec/gdtest_types.shp" ]; then
  ogr2ogr -overwrite -lco GEOMETRY_NAME=geom -lco FID=gid -nln gdtest_types \
    "$PG" "$ROOT/gdtest_vec/gdtest_types.shp"
  echo "loaded gdtest_types"
fi

# 扩展表：2 万点大表（WFS 分页 / maxFeatures / 计时基线）
if [ -f "$ROOT/gdtest_vol/gdtest_pts20k.shp" ]; then
  ogr2ogr -overwrite -lco GEOMETRY_NAME=geom -lco FID=gid -nln gdtest_pts \
    "$PG" "$ROOT/gdtest_vol/gdtest_pts20k.shp"
  echo "loaded gdtest_pts"
fi
