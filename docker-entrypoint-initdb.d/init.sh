#!/usr/bin/env bash
set -Eeuo pipefail

echo "Waiting for SQL Server to be ready..."

if [ -x "/opt/mssql-tools/bin/sqlcmd" ]; then
  SQLCMD="/opt/mssql-tools/bin/sqlcmd"
elif [ -x "/opt/mssql-tools18/bin/sqlcmd" ]; then
  SQLCMD="/opt/mssql-tools18/bin/sqlcmd"
else
  echo "sqlcmd not found in expected locations"
  exit 1
fi

echo "Using sqlcmd at: $SQLCMD"

# Require SA password to be present
: "${SA_PASSWORD:?Environment variable SA_PASSWORD is required}"

until "$SQLCMD" -S sqlserver -U sa -P "$SA_PASSWORD" -C -Q "SELECT 1" >/dev/null 2>&1; do
  echo "Waiting for SQL Server to be available..."
  sleep 2
done

echo "SQL Server is up. Running initialization script..."
"$SQLCMD" -S sqlserver -U sa -P "$SA_PASSWORD" -C -i "/docker-entrypoint-initdb.d/init.sql"
echo "Database initialization finished."