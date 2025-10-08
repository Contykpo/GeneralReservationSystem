using GeneralReservationSystem.Application.Helpers;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using System.Reflection;

namespace GeneralReservationSystem.Infrastructure.Helpers
{
    public static class SqlCommandHelper
    {
        public static string FormatQualifiedTableName(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName)) return tableName;
            var parts = tableName.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join('.', parts.Select(p => "[" + p + "]"));
        }

        public static void AddParameter(DbCommand cmd, string parameterName, object? value, Type propertyType)
        {
            var param = cmd.CreateParameter();
            param.ParameterName = parameterName;
            param.Value = EntityTypeConverter.ConvertToDbValue(value, propertyType);
            cmd.Parameters.Add(param);
        }

        public static void AddParameters(DbCommand cmd, IEnumerable<KeyValuePair<string, object?>> parameters)
        {
            foreach (var p in parameters)
            {
                var param = cmd.CreateParameter();
                param.ParameterName = p.Key;
                param.Value = p.Value ?? DBNull.Value;
                cmd.Parameters.Add(param);
            }
        }

        public static async Task<DbConnection> CreateAndOpenConnectionAsync(
            Func<DbConnection> connectionFactory,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var conn = connectionFactory();
                if (conn.State != System.Data.ConnectionState.Open)
                    await conn.OpenAsync(cancellationToken);
                return conn;
            }
            catch (DbException ex)
            {
                throw SqlExceptionHelper.ToRepositoryException(ex);
            }
        }

        public static async Task<DbTransaction> CreateTransactionAsync(
            DbConnection dbConnection,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await dbConnection.BeginTransactionAsync(cancellationToken);
            }
            catch (DbException ex)
            {
                throw SqlExceptionHelper.ToRepositoryException(ex);
            }
        }

        public static DbCommand CreateCommand(DbConnection connection, DbTransaction? transaction = null)
        {
            var cmd = connection.CreateCommand();
            if (transaction != null)
                cmd.Transaction = transaction;
            return cmd;
        }

        public static string BuildColumnList(IEnumerable<PropertyInfo> properties, string? tableAlias = null)
        {
            var columns = properties.Select(p =>
            {
                var colName = EntityHelper.GetColumnName(p);
                var prefix = string.IsNullOrEmpty(tableAlias) ? "" : $"{FormatQualifiedTableName(tableAlias)}.";
                return $"{prefix}[{colName}]";
            });
            return string.Join(", ", columns);
        }

        public static string BuildColumnListWithAliases(
            IEnumerable<(string Column, string Alias)> columns,
            string tableName)
        {
            var qualifiedTable = FormatQualifiedTableName(tableName);
            return string.Join(", ", columns.Select(s => $"{qualifiedTable}.[{s.Column}] AS [{s.Alias}]"));
        }

        public static async Task<object?> ExecuteScalarAsync(
            Func<DbConnection> connectionFactory,
            DbTransaction? transaction,
            string sql,
            IReadOnlyList<KeyValuePair<string, object?>> parameters,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await using var conn = await CreateAndOpenConnectionAsync(connectionFactory, cancellationToken);
                await using var cmd = CreateCommand(conn, transaction);
                cmd.CommandText = sql;
                AddParameters(cmd, parameters);
                return await cmd.ExecuteScalarAsync(cancellationToken);
            }
            catch (DbException ex)
            {
                throw SqlExceptionHelper.ToRepositoryException(ex);
            }
        }

        public static object? ExecuteScalar(
            Func<DbConnection> connectionFactory,
            DbTransaction? transaction,
            string sql,
            IReadOnlyList<KeyValuePair<string, object?>> parameters)
        {
            try
            {
                using var conn = connectionFactory();
                if (conn.State != System.Data.ConnectionState.Open)
                    conn.Open();
                using var cmd = CreateCommand(conn, transaction);
                cmd.CommandText = sql;
                AddParameters(cmd, parameters);
                return cmd.ExecuteScalar();
            }
            catch (DbException ex)
            {
                throw SqlExceptionHelper.ToRepositoryException(ex);
            }
        }

        public static async Task<int> ExecuteNonQueryAsync(
            DbCommand cmd,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (DbException ex)
            {
                throw SqlExceptionHelper.ToRepositoryException(ex);
            }
        }

        public static async Task<DbDataReader> ExecuteReaderAsync(
            DbCommand cmd,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await cmd.ExecuteReaderAsync(cancellationToken);
            }
            catch (DbException ex)
            {
                throw SqlExceptionHelper.ToRepositoryException(ex);
            }
        }

        public static DbDataReader ExecuteReader(DbCommand cmd)
        {
            try
            {
                return cmd.ExecuteReader();
            }
            catch (DbException ex)
            {
                throw SqlExceptionHelper.ToRepositoryException(ex);
            }
        }

        public static string BuildOutputClause(IEnumerable<PropertyInfo> computedProperties, string tableName)
        {
            var outputColumns = computedProperties.Select(p => $"INSERTED.[{EntityHelper.GetColumnName(p)}]");
            return "OUTPUT " + string.Join(",", outputColumns);
        }

        public static string BuildInsertStatement(string tableName, IEnumerable<PropertyInfo> nonComputedProperties, IEnumerable<PropertyInfo> computedProperties, Func<PropertyInfo, int, string> parameterNameFactory)
        {
            var columns = nonComputedProperties.Select(p => EntityHelper.GetColumnName(p)).ToArray();
            var values = nonComputedProperties.Select((p, i) => parameterNameFactory(p, i)).ToArray();
            var hasOutput = computedProperties.Any();

            return hasOutput
                ? $"INSERT INTO [{tableName}] (" + string.Join(",", columns) + ") " + BuildOutputClause(computedProperties, tableName) + " VALUES (" + string.Join(",", values) + ")"
                : $"INSERT INTO [{tableName}] (" + string.Join(",", columns) + ") VALUES (" + string.Join(",", values) + ")";
        }

        public static string BuildUpdateStatement(string tableName, IEnumerable<PropertyInfo> setColumns, IEnumerable<PropertyInfo> keyProperties, IEnumerable<PropertyInfo> computedProperties, Func<PropertyInfo, int, string> setParamFactory, Func<PropertyInfo, int, string> keyParamFactory)
        {
            var setClauses = setColumns.Select((p, i) => $"{EntityHelper.GetColumnName(p)} = {setParamFactory(p, i)}").ToArray();
            var whereClauses = keyProperties.Select((p, i) => $"{EntityHelper.GetColumnName(p)} = {keyParamFactory(p, i)}").ToArray();
            var hasOutput = computedProperties.Any();

            return hasOutput
                ? $"UPDATE [{tableName}] SET " + string.Join(",", setClauses) + " " + BuildOutputClause(computedProperties, tableName) + " WHERE " + string.Join(" AND ", whereClauses)
                : $"UPDATE [{tableName}] SET " + string.Join(",", setClauses) + " WHERE " + string.Join(" AND ", whereClauses);
        }

        public static string BuildDeleteStatement(string tableName, IEnumerable<PropertyInfo> keyProperties, Func<PropertyInfo, int, string> keyParamFactory)
        {
            var whereClauses = keyProperties.Select((p, i) => $"{EntityHelper.GetColumnName(p)} = {keyParamFactory(p, i)}").ToArray();
            return $"DELETE FROM [{tableName}] WHERE " + string.Join(" AND ", whereClauses);
        }

        public static string BuildBulkWhereClause<T>(IList<T> entities, PropertyInfo[] keyProperties, DbCommand cmd, string paramPrefix) where T : class
        {
            var whereClauses = new List<string>();
            for (int idx = 0; idx < entities.Count; idx++)
            {
                var clauseParts = new List<string>();
                for (int k = 0; k < keyProperties.Length; k++)
                {
                    var keyProp = keyProperties[k];
                    var colName = EntityHelper.GetColumnName(keyProp);
                    var paramName = $"@{paramPrefix}{idx}_{k}";
                    var rawValue = keyProp.GetValue(entities[idx]);
                    AddParameter(cmd, paramName, rawValue, keyProp.PropertyType);
                    clauseParts.Add($"{colName} = {paramName}");
                }
                whereClauses.Add("(" + string.Join(" AND ", clauseParts) + ")");
            }
            return string.Join(" OR ", whereClauses);
        }
    }
}
