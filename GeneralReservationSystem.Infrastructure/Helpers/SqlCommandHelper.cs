using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.Helpers;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace GeneralReservationSystem.Infrastructure.Helpers
{
    public static class SqlCommandHelper
    {
        /*public static string FormatQualifiedTableName(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                return tableName;
            }

            string[] parts = tableName.Split(['.'], StringSplitOptions.RemoveEmptyEntries);
            return string.Join('.', parts.Select(p => $"\"{p}\""));
        }*/

        public static void AddParameter(DbCommand cmd, string parameterName, object? value, Type propertyType)
        {
            DbParameter param = cmd.CreateParameter();
            param.ParameterName = parameterName;
            param.Value = EntityTypeConverter.ConvertToDbValue(value, propertyType);
            _ = cmd.Parameters.Add(param);
        }

        /*public static void AddParameters(DbCommand cmd, IEnumerable<KeyValuePair<string, object?>> parameters)
        {
            foreach (KeyValuePair<string, object?> p in parameters)
            {
                DbParameter param = cmd.CreateParameter();
                param.ParameterName = p.Key;
                param.Value = p.Value ?? DBNull.Value;
                _ = cmd.Parameters.Add(param);
            }
        }*/

        public static async Task<DbConnection> CreateAndOpenConnectionAsync(
            Func<DbConnection> connectionFactory,
            CancellationToken cancellationToken = default)
        {
            try
            {
                DbConnection conn = connectionFactory();
                if (conn.State != ConnectionState.Open)
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

        /*public static DbConnection CreateAndOpenConnection(
            Func<DbConnection> connectionFactory)
        {
            try
            {
                DbConnection conn = connectionFactory();
                if (conn.State != ConnectionState.Open)
                {
                    conn.Open();
                }

                return conn;
            }
            catch (DbException ex)
            {
                throw SqlExceptionHelper.ToRepositoryException(ex);
            }
        }*/

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

        /*public static DbTransaction CreateTransaction(
            DbConnection dbConnection)
        {
            try
            {
                return dbConnection.BeginTransaction();
            }
            catch (DbException ex)
            {
                throw SqlExceptionHelper.ToRepositoryException(ex);
            }
        }*/

        public static DbCommand CreateCommand(DbConnection connection, DbTransaction? transaction = null)
        {
            DbCommand cmd = connection.CreateCommand();
            if (transaction != null)
            {
                cmd.Transaction = transaction;
            }

            return cmd;
        }

        /*public static string BuildColumnList(IEnumerable<PropertyInfo> properties, string? tableAlias = null)
        {
            IEnumerable<string> columns = properties.Select(p =>
            {
                string colName = EntityHelper.GetColumnName(p);
                string prefix = string.IsNullOrEmpty(tableAlias) ? "" : $"{FormatQualifiedTableName(tableAlias)}.";
                return $"{prefix}\"{colName}\"";
            });
            return string.Join(", ", columns);
        }

        public static string BuildColumnListWithAliases(
            IEnumerable<(string Column, string Alias)> columns,
            string tableName)
        {
            string qualifiedTable = FormatQualifiedTableName(tableName);
            return string.Join(", ", columns.Select(s => $"{qualifiedTable}.\"{s.Column}\" AS \"{s.Alias}\""));
        }*/

        /*public static async Task<object?> ExecuteScalarAsync(
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
        }*/

        public static async Task<object?> ExecuteScalarAsync(
            DbCommand cmd,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await cmd.ExecuteScalarAsync(cancellationToken);
            }
            catch (DbException ex)
            {
                throw SqlExceptionHelper.ToRepositoryException(ex);
            }
        }

        /*public static object? ExecuteScalar(
            Func<DbConnection> connectionFactory,
            DbTransaction? transaction,
            string sql,
            IReadOnlyList<KeyValuePair<string, object?>> parameters)
        {
            try
            {
                using DbConnection conn = connectionFactory();
                if (conn.State != ConnectionState.Open)
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

        public static object? ExecuteScalar(DbCommand cmd)
        {
            try
            {
                return cmd.ExecuteScalar();
            }
            catch (DbException ex)
            {
                throw SqlExceptionHelper.ToRepositoryException(ex);
            }
        }*/

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

        /*public static DbDataReader ExecuteReader(DbCommand cmd)
        {
            try
            {
                return cmd.ExecuteReader();
            }
            catch (DbException ex)
            {
                throw SqlExceptionHelper.ToRepositoryException(ex);
            }
        }*/

        public static string BuildReturningClause(IEnumerable<PropertyInfo> computedProperties)
        {
            IEnumerable<string> returningColumns = computedProperties.Select(p => $"\"{EntityHelper.GetColumnName(p)}\"");
            return "RETURNING " + string.Join(", ", returningColumns);
        }

        public static string BuildInsertStatement(string tableName, IEnumerable<PropertyInfo> nonComputedProperties, IEnumerable<PropertyInfo> computedProperties, Func<PropertyInfo, int, string> parameterNameFactory)
        {
            string[] columns = [.. nonComputedProperties.Select(EntityHelper.GetColumnName)];
            string[] values = [.. nonComputedProperties.Select((p, i) => parameterNameFactory(p, i))];
            bool hasOutput = computedProperties.Any();

            return hasOutput
                ? $"INSERT INTO grsdb.\"{tableName}\" (" + string.Join(", ", columns.Select(c => $"\"{c}\"")) + $") VALUES (" + string.Join(", ", values) + $") {BuildReturningClause(computedProperties)}"
                : $"INSERT INTO grsdb.\"{tableName}\" (" + string.Join(", ", columns.Select(c => $"\"{c}\"")) + $") VALUES (" + string.Join(", ", values) + ")";
        }

        public static string BuildUpdateStatement(string tableName, IEnumerable<PropertyInfo> setColumns, IEnumerable<PropertyInfo> keyProperties, IEnumerable<PropertyInfo> computedProperties, Func<PropertyInfo, int, string> setParamFactory, Func<PropertyInfo, int, string> keyParamFactory)
        {
            string[] setClauses = [.. setColumns.Select((p, i) => $"\"{EntityHelper.GetColumnName(p)}\" = {setParamFactory(p, i)}")];
            string[] whereClauses = [.. keyProperties.Select((p, i) => $"\"{EntityHelper.GetColumnName(p)}\" = {keyParamFactory(p, i)}")];
            bool hasOutput = computedProperties.Any();

            return hasOutput
                ? $"UPDATE grsdb.\"{tableName}\" SET " + string.Join(", ", setClauses) + " WHERE " + string.Join(" AND ", whereClauses) + $" {BuildReturningClause(computedProperties)}"
                : $"UPDATE grsdb.\"{tableName}\" SET " + string.Join(", ", setClauses) + " WHERE " + string.Join(" AND ", whereClauses);
        }

        public static string BuildDeleteStatement(string tableName, IEnumerable<PropertyInfo> keyProperties, Func<PropertyInfo, int, string> keyParamFactory)
        {
            string[] whereClauses = [.. keyProperties.Select((p, i) => $"\"{EntityHelper.GetColumnName(p)}\" = {keyParamFactory(p, i)}")];
            return $"DELETE FROM grsdb.\"{tableName}\" WHERE " + string.Join(" AND ", whereClauses);
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

        public static string BuildFiltersClauses<T>(IEnumerable<FilterClause> filterClauses, string paramPrefix = "p")
        {
            Type type = typeof(T);
            PropertyInfo[] properties = ReflectionHelpers.GetProperties(type);
            HashSet<string> validNames = [.. properties.Select(p => p.Name)];
            List<string> clauses = [];
            int paramIndex = 0;
            foreach (FilterClause filterClause in filterClauses)
            {
                List<string> propositions = [];
                foreach (Filter filter in filterClause.Filters)
                {
                    PropertyInfo? prop = properties.FirstOrDefault(p => p.Name == filter.PropertyOrField);
                    if (prop == null)
                    {
                        continue; // skip invalid
                    }

                    string colName = EntityHelper.GetColumnName(prop);
                    string paramName = $"@{paramPrefix}{paramIndex}";
                    string? proposition = filter.Operator switch
                    {
                        FilterOperator.Equals => $"\"{colName}\" = {paramName}",
                        FilterOperator.NotEquals => $"\"{colName}\" <> {paramName}",
                        FilterOperator.GreaterThan => $"\"{colName}\" > {paramName}",
                        FilterOperator.GreaterThanOrEqual => $"\"{colName}\" >= {paramName}",
                        FilterOperator.LessThan => $"\"{colName}\" < {paramName}",
                        FilterOperator.LessThanOrEqual => $"\"{colName}\" <= {paramName}",
                        FilterOperator.Contains => $"\"{colName}\" ILIKE '%' || {paramName} || '%'",
                        FilterOperator.NotContains => $"\"{colName}\" NOT ILIKE '%' || {paramName} || '%'",
                        FilterOperator.StartsWith => $"\"{colName}\" ILIKE {paramName} || '%'",
                        FilterOperator.EndsWith => $"\"{colName}\" ILIKE '%' || {paramName}",
                        FilterOperator.IsNullOrEmpty => $"(\"{colName}\" IS NULL OR \"{colName}\" = '')",
                        FilterOperator.IsNotNullOrEmpty => $"(\"{colName}\" IS NOT NULL AND \"{colName}\" <> '')",
                        FilterOperator.Between => $"\"{colName}\" BETWEEN {paramName}_start AND {paramName}_end",
                        _ => null
                    };

                    if (proposition != null)
                    {
                        propositions.Add(proposition);
                        paramIndex++;
                    }
                }

                if (propositions.Count > 0)
                {
                    string combined = string.Join(" OR ", propositions);
                    clauses.Add($"({combined})");
                }
            }
            return clauses.Count > 0 ? string.Join(" AND ", clauses) : "";
        }

        public static string BuildOrderByClause<T>(IEnumerable<SortOption> sortOptions)
        {
            Type type = typeof(T);
            PropertyInfo[] properties = ReflectionHelpers.GetProperties(type);
            List<string> clauses = [];
            foreach (SortOption sort in sortOptions)
            {
                PropertyInfo? prop = properties.FirstOrDefault(p => p.Name == sort.PropertyOrField);
                if (prop == null)
                {
                    continue; // skip invalid
                }

                string colName = EntityHelper.GetColumnName(prop);
                string direction = sort.Direction == SortDirection.Desc ? "DESC" : "ASC";
                clauses.Add($"\"{colName}\" {direction}");
            }
            return clauses.Count > 0 ? string.Join(", ", clauses) : "";
        }

        public static string BuildOrderByClauseWithDefault<T>(IEnumerable<SortOption> sortOptions)
        {
            string orderByClause = BuildOrderByClause<T>(sortOptions);
            
            if (!string.IsNullOrEmpty(orderByClause))
            {
                return orderByClause;
            }

            // No explicit order provided, use first property as default
            Type type = typeof(T);
            PropertyInfo[] properties = ReflectionHelpers.GetProperties(type);

            string firstColumnName = EntityHelper.GetColumnName(properties[0]);
            return $"\"{firstColumnName}\" ASC";
        }

        public static object? ConvertToPropertyType(object? value, Type propType)
        {
            if (value == null)
            {
                return null;
            }

            Type targetType = Nullable.GetUnderlyingType(propType) ?? propType;

            return targetType.IsEnum
                ? value is string str1 ? Enum.Parse(targetType, str1) : Enum.ToObject(targetType, value)
                : targetType == typeof(Guid)
                ? value is string str2 ? Guid.Parse(str2) : value
                : targetType == typeof(DateTime)
                ? value is string str3 ? DateTime.Parse(str3) : Convert.ChangeType(value, targetType)
                : Convert.ChangeType(value, targetType);
        }

        public static void AddFilterParameters<TResult>(DbCommand cmd, IEnumerable<FilterClause> filterClauses, string paramPrefix = "p")
        {
            if (filterClauses == null || !filterClauses.Any())
            {
                return;
            }

            PropertyInfo[] resultProperties = typeof(TResult).GetProperties();

            int paramIndex = 0;
            foreach (FilterClause filterClause in filterClauses)
            {
                foreach (Filter filter in filterClause.Filters)
                {
                    if (filter.Operator is FilterOperator.IsNullOrEmpty or FilterOperator.IsNotNullOrEmpty)
                    {
                        continue;
                    }
                    PropertyInfo? prop = resultProperties.FirstOrDefault(p => p.Name == filter.PropertyOrField);
                    if (prop == null)
                    {
                        continue;
                    }
                    string paramName = $"@{paramPrefix}{paramIndex}";
                    Type propType = prop.PropertyType;
                    if (filter.Operator == FilterOperator.Between && filter.Value is object[] arr && arr.Length == 2)
                    {
                        object? startValue = ConvertToPropertyType(arr[0], propType);
                        object? endValue = ConvertToPropertyType(arr[1], propType);
                        AddParameter(cmd, paramName + "_start", startValue, propType);
                        AddParameter(cmd, paramName + "_end", endValue, propType);
                    }
                    else
                    {
                        object? value = ConvertToPropertyType(filter.Value, propType);
                        AddParameter(cmd, paramName, value, propType);
                    }
                    paramIndex++;
                }
            }
        }

        public static async Task<PagedResult<TResult>> MapPagedResultAsync<TResult>(
            DbCommand cmd,
            DbCommand countCmd,
            Func<DbDataReader, TResult> mapFunc,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            List<TResult> items = [];
            using (DbDataReader reader = await ExecuteReaderAsync(cmd, cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    items.Add(mapFunc(reader));
                }
            }

            long totalCount = (long)(await ExecuteScalarAsync(countCmd, cancellationToken))!;
            return new PagedResult<TResult>
            {
                Items = items,
                TotalCount = (int)totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
    }
}
