using GeneralReservationSystem.Application.Helpers;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace GeneralReservationSystem.Infrastructure.Helpers
{
    public static class SqlCommandHelper
    {
        public static string FormatQualifiedTableName(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                return tableName;
            }

            string[] parts = tableName.Split(['.'], StringSplitOptions.RemoveEmptyEntries);
            return string.Join('.', parts.Select(p => "[" + p + "]"));
        }

        public static void AddParameter(DbCommand cmd, string parameterName, object? value, Type propertyType)
        {
            DbParameter param = cmd.CreateParameter();
            param.ParameterName = parameterName;
            param.Value = EntityTypeConverter.ConvertToDbValue(value, propertyType);
            _ = cmd.Parameters.Add(param);
        }

        public static void AddParameters(DbCommand cmd, IEnumerable<KeyValuePair<string, object?>> parameters)
        {
            foreach (KeyValuePair<string, object?> p in parameters)
            {
                DbParameter param = cmd.CreateParameter();
                param.ParameterName = p.Key;
                param.Value = p.Value ?? DBNull.Value;
                _ = cmd.Parameters.Add(param);
            }
        }

        public static async Task<DbConnection> CreateAndOpenConnectionAsync(
            Func<DbConnection> connectionFactory,
            CancellationToken cancellationToken = default)
        {
            try
            {
                DbConnection conn = connectionFactory();
                if (conn.State != System.Data.ConnectionState.Open)
                {
                    await conn.OpenAsync(cancellationToken);
                }

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
            DbCommand cmd = connection.CreateCommand();
            if (transaction != null)
            {
                cmd.Transaction = transaction;
            }

            return cmd;
        }

        public static string BuildColumnList(IEnumerable<PropertyInfo> properties, string? tableAlias = null)
        {
            IEnumerable<string> columns = properties.Select(p =>
            {
                string colName = EntityHelper.GetColumnName(p);
                string prefix = string.IsNullOrEmpty(tableAlias) ? "" : $"{FormatQualifiedTableName(tableAlias)}.";
                return $"{prefix}[{colName}]";
            });
            return string.Join(", ", columns);
        }

        public static string BuildColumnListWithAliases(
            IEnumerable<(string Column, string Alias)> columns,
            string tableName)
        {
            string qualifiedTable = FormatQualifiedTableName(tableName);
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
                await using DbConnection conn = await CreateAndOpenConnectionAsync(connectionFactory, cancellationToken);
                await using DbCommand cmd = CreateCommand(conn, transaction);
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
                using DbConnection conn = connectionFactory();
                if (conn.State != System.Data.ConnectionState.Open)
                {
                    conn.Open();
                }

                using DbCommand cmd = CreateCommand(conn, transaction);
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

        public static string BuildOutputClause(IEnumerable<PropertyInfo> computedProperties)
        {
            IEnumerable<string> outputColumns = computedProperties.Select(p => $"INSERTED.[{EntityHelper.GetColumnName(p)}]");
            return "OUTPUT " + string.Join(",", outputColumns);
        }

        public static string BuildInsertStatement(string tableName, IEnumerable<PropertyInfo> nonComputedProperties, IEnumerable<PropertyInfo> computedProperties, Func<PropertyInfo, int, string> parameterNameFactory)
        {
            string[] columns = nonComputedProperties.Select(EntityHelper.GetColumnName).ToArray();
            string[] values = nonComputedProperties.Select((p, i) => parameterNameFactory(p, i)).ToArray();
            bool hasOutput = computedProperties.Any();

            return hasOutput
                ? $"INSERT INTO [{tableName}] (" + string.Join(",", columns) + ") " + BuildOutputClause(computedProperties) + " VALUES (" + string.Join(",", values) + ")"
                : $"INSERT INTO [{tableName}] (" + string.Join(",", columns) + ") VALUES (" + string.Join(",", values) + ")";
        }

        public static string BuildUpdateStatement(string tableName, IEnumerable<PropertyInfo> setColumns, IEnumerable<PropertyInfo> keyProperties, IEnumerable<PropertyInfo> computedProperties, Func<PropertyInfo, int, string> setParamFactory, Func<PropertyInfo, int, string> keyParamFactory)
        {
            string[] setClauses = setColumns.Select((p, i) => $"{EntityHelper.GetColumnName(p)} = {setParamFactory(p, i)}").ToArray();
            string[] whereClauses = keyProperties.Select((p, i) => $"{EntityHelper.GetColumnName(p)} = {keyParamFactory(p, i)}").ToArray();
            bool hasOutput = computedProperties.Any();

            return hasOutput
                ? $"UPDATE [{tableName}] SET " + string.Join(",", setClauses) + " " + BuildOutputClause(computedProperties) + " WHERE " + string.Join(" AND ", whereClauses)
                : $"UPDATE [{tableName}] SET " + string.Join(",", setClauses) + " WHERE " + string.Join(" AND ", whereClauses);
        }

        public static string BuildDeleteStatement(string tableName, IEnumerable<PropertyInfo> keyProperties, Func<PropertyInfo, int, string> keyParamFactory)
        {
            string[] whereClauses = keyProperties.Select((p, i) => $"{EntityHelper.GetColumnName(p)} = {keyParamFactory(p, i)}").ToArray();
            return $"DELETE FROM [{tableName}] WHERE " + string.Join(" AND ", whereClauses);
        }

        public static string BuildBulkWhereClause<T>(IList<T> entities, PropertyInfo[] keyProperties, DbCommand cmd, string paramPrefix) where T : class
        {
            List<string> whereClauses = [];
            for (int idx = 0; idx < entities.Count; idx++)
            {
                List<string> clauseParts = [];
                for (int k = 0; k < keyProperties.Length; k++)
                {
                    PropertyInfo keyProp = keyProperties[k];
                    string colName = EntityHelper.GetColumnName(keyProp);
                    string paramName = $"@{paramPrefix}{idx}_{k}";
                    object? rawValue = keyProp.GetValue(entities[idx]);
                    AddParameter(cmd, paramName, rawValue, keyProp.PropertyType);
                    clauseParts.Add($"{colName} = {paramName}");
                }
                whereClauses.Add("(" + string.Join(" AND ", clauseParts) + ")");
            }
            return string.Join(" OR ", whereClauses);
        }
    }
}
