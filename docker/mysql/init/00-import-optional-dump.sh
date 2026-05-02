#!/bin/sh
set -eu

echo "[initdb] Optional dump import step"

dump_file="${WM_DB_DUMP_FILE:-}"

if [ -z "$dump_file" ]; then
  echo "[initdb] WM_DB_DUMP_FILE is empty. Skipping dump import."
  exit 0
fi

if [ ! -f "$dump_file" ]; then
  echo "[initdb] Dump file not found at: $dump_file. Skipping dump import."
  exit 0
fi

if [ ! -s "$dump_file" ]; then
  echo "[initdb] Dump file is empty at: $dump_file. Skipping dump import."
  exit 0
fi

echo "[initdb] Dump found: $dump_file"

echo "[initdb] Importing dump into database: ${MYSQL_DATABASE}"
mysql -uroot -p"${MYSQL_ROOT_PASSWORD}" "${MYSQL_DATABASE}" < "$dump_file"

echo "[initdb] Dump import completed successfully"
