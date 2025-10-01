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
    local use_db="${2:-false}"
    local db_arg=""
    if [ "$use_db" = "true" ]; then
        db_arg="-d "$DB_NAME""
    fi
    /opt/mssql-tools18/bin/sqlcmd -S "$DB_HOST,$DB_PORT" -U "$DB_USER" -P "$DB_PASSWORD" $db_arg -Q "$sql" -N -C
}

run_sql_file() {
    local file="$1"
    local use_db="${2:-false}"
    local db_arg=""
    if [ "$use_db" = "true" ]; then
        db_arg="-d "$DB_NAME""
    fi
    /opt/mssql-tools18/bin/sqlcmd -S "$DB_HOST,$DB_PORT" -U "$DB_USER" -P "$DB_PASSWORD" $db_arg -i "$file" -N -C
}

wait_for_sqlserver() {
    sleep 30
    echo "================================================================="
    echo "Waiting for SQL Server at $DB_HOST:$DB_PORT..."
    until run_sql "SELECT 1;" false >/dev/null 2>&1; do
        sleep 2
        echo "Still waiting for SQL Server..."
    done
    echo "SQL Server is ready."
}

run_init_db() {
    echo "Running initial SQL script..."
    run_sql_file "./db-init.sql" false
    echo "Database initialized."
}

run_migrations() {
    echo "Running migrations from $MIGRATIONS_DIR..."
    for file in $(ls "$MIGRATIONS_DIR"/*.sql | sort); do
        echo "Applying migration: $file"

        run_sql_file "$file" true # NOTE: Each migration needs to be idempotent (so it can be run multiple times without causing errors)
    done
    echo "All migrations applied."
    echo "================================================================="
}

wait_for_sqlserver
run_init_db
run_migrations