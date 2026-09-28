#!/usr/bin/env bash
# CI 诊断：扩展数据集在“宿主 / 容器”两侧是否真的存在且可读。
# 主生成器会吞掉扩展子脚本的 stdout，仅看退出码不足以判定 GeoServer 能否读到数据；
# 而 “no attributes were specified” 这类服务端报错正是“存储看不到文件”的表现，需要一眼可辨的证据。
set -u

WS="${GITHUB_WORKSPACE:-$(pwd)}"
GS_HOST="$WS/gsdata"
GS_DATA="${GS_CONTAINER_DATA_DIR:-/data/geoserver/data_dir}"

echo "=== host: 扩展数据集清单 ==="
for d in gdtest_vec gdtest_img gdtest_vol gdtest_bad; do
  count=$(ls "$GS_HOST/$d" 2>/dev/null | wc -l | tr -d ' ')
  sample=$(ls "$GS_HOST/$d" 2>/dev/null | head -3 | paste -sd ' ' -)
  echo "$d: ${count} 个条目; 样例: ${sample:-（空）}"
done

echo "=== container: GeoServer 视角 ==="
if command -v docker >/dev/null 2>&1 && [ -n "$(docker ps -q -f name=geoserver 2>/dev/null)" ]; then
  docker exec geoserver bash -lc "for d in gdtest_vec gdtest_img gdtest_vol gdtest_bad; do
      p=$GS_DATA/\$d
      printf '%s: %s 个条目; 属主/权限: %s\n' \"\$d\" \"\$(ls \"\$p\" 2>/dev/null | wc -l | tr -d ' ')\" \"\$(stat -c '%U:%G %A' \"\$p\" 2>/dev/null || echo 不可见)\"
    done
    printf '容器内以 GeoServer 用户读 .shp 头 4 字节: %s\n' \"\$(head -c 4 $GS_DATA/gdtest_vec/gdtest_types.shp 2>&1 | od -An -tx1 | tr -s ' ' | tr -d '\n')\"" || true
else
  echo "（无 docker 或未运行 geoserver 容器，跳过容器侧检查）"
fi
