#!/bin/bash
trap 'echo "Error occurred at line $LINENO. Exiting with status $?"' ERR
set -euo pipefail

DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-1433}"
DB_NAME="${DB_NAME:-AppDb}"
DB_USER="${DB_USER:-sa}"
DB_PASSWORD="${DB_PASSWORD:-SuperSa1234}"

MIGRATIONS_DIR="./Migrations"

run_sql() {
    local sql="$1"
    /opt/mssql-tools18/bin/sqlcmd -S "$DB_HOST,$DB_PORT" -U "$DB_USER" -P "$DB_PASSWORD" -d "$DB_NAME" -Q "$sql" -N -C
}

run_sql_file() {
    local file="$1"
    /opt/mssql-tools18/bin/sqlcmd -S "$DB_HOST,$DB_PORT" -U "$DB_USER" -P "$DB_PASSWORD" -d "$DB_NAME" -i "$file" -N -C
}

wait_for_sqlserver() {
    echo "================================================================="
    echo "Waiting for SQL Server at $DB_HOST:$DB_PORT..."
    until run_sql "SELECT 1;" >/dev/null 2>&1; do
        sleep 2
        echo "Still waiting for SQL Server..."
    done
    echo "SQL Server is ready."
}

run_init_db() {
    echo "Running initial SQL script..."
    /opt/mssql-tools18/bin/sqlcmd -S "$DB_HOST,$DB_PORT" -U "$DB_USER" -P "$DB_PASSWORD" -i "./db-init.sql" -N -C
    echo "Database initialized."
}

run_migrations() {
    echo "Running migrations from $MIGRATIONS_DIR..."
    for file in $(ls "$MIGRATIONS_DIR"/*.sql | sort); do
        echo "Applying migration: $file"

        run_sql_file "$file" # NOTE: Each migration needs to be idempotent (so it can be run multiple times without causing errors)
    done
    echo "All migrations applied."
}

wait_for_sqlserver;
run_init_db;
run_migrations;