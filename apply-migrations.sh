#!/bin/bash
trap 'echo "Error occurred at line $LINENO. Exiting with status $?"' ERR
set -euo pipefail

DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-1433}"
DB_NAME="${DB_NAME:-AppDb}"
DB_USER="${DB_USER:-sa}"
DB_PASSWORD="${DB_PASSWORD:-SuperSa1234}"
ADMIN_USER_PASSWORD="${ADMIN_USER_PASSWORD:-Admin1234}"
APP_USER_PASSWORD="${APP_USER_PASSWORD:-Application1234}"

MIGRATIONS_DIR="./Migrations"

run_sql() {
    local sql="$1"
    /opt/mssql-tools18/bin/sqlcmd -S "$DB_HOST,$DB_PORT" -U "$DB_USER" -P "$DB_PASSWORD" -Q "$sql" -N -C
}

run_sql_file() {
    local file="$1"
    /opt/mssql-tools18/bin/sqlcmd -S "$DB_HOST,$DB_PORT" -U "$DB_USER" -P "$DB_PASSWORD" \
      -i "$file" -N -C \
      -v AdminUserPassword="$ADMIN_USER_PASSWORD" AppUserPassword="$APP_USER_PASSWORD"
}

run_migrations() {
    sleep 10
    echo "================================================================="
    echo "Waiting for SQL Server at $DB_HOST:$DB_PORT..."
    until run_sql "SELECT 1;" false >/dev/null 2>&1; do
        sleep 2
        echo "Still waiting for SQL Server..."
    done
    echo "SQL Server is ready."
    echo "Running migrations from $MIGRATIONS_DIR..."
    for file in $(ls "$MIGRATIONS_DIR"/*.sql | sort); do
        echo "Applying migration: $file"
        run_sql_file "$file"
    done
    echo "All migrations applied."
    echo "================================================================="
}

run_migrations