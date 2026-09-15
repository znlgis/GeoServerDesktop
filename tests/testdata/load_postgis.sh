#!/usr/bin/env bash
# 将生成的 gdtest_poly.shp 加载进 PostGIS（期望值仍由 manifest/文件头独立推导）。
# 连接参数与 xunit/harness 同源：GSD_TEST_PG_*（默认 127.0.0.1:5432 postgres/postgres/postgres）。
set -e
DATA="${GSD_TEST_DATA_DIR:-D:\\self\\tool\\docker\\data\\geoserver\\gdtest_data}"
PGH="${GSD_TEST_PG_HOST:-127.0.0.1}"; PGP="${GSD_TEST_PG_PORT:-5432}"
PGD="${GSD_TEST_PG_DB:-postgres}"; PGU="${GSD_TEST_PG_USER:-postgres}"; PGW="${GSD_TEST_PG_PASS:-postgres}"
TABLE="${GSD_TEST_PG_TABLE:-gdtest_poly}"
ogr2ogr -overwrite -nlt PROMOTE_TO_MULTI -lco GEOMETRY_NAME=geom -nln "$TABLE" \
  "PG:host=$PGH port=$PGP dbname=$PGD user=$PGU password=$PGW" \
  "$DATA/gdtest_poly.shp"
echo "loaded $TABLE into $PGD"
