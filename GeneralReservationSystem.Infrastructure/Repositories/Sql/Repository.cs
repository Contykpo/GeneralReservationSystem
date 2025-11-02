using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.Helpers;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using GeneralReservationSystem.Infrastructure.Helpers;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Text;

namespace GeneralReservationSystem.Infrastructure.Repositories.Sql
{
    public class Repository<T>(Func<DbConnection> connectionFactory) : IRepository<T> where T : class, new()
    {
        protected static readonly string _tableName = EntityHelper.GetTableName<T>();
        protected static readonly PropertyInfo[] _properties = typeof(T).GetProperties();
        protected static readonly PropertyInfo[] _keyProperties = EntityHelper.GetKeyProperties<T>();
        protected static readonly PropertyInfo[] _computedProperties = EntityHelper.GetComputedProperties<T>();
        protected static readonly PropertyInfo[] _nonComputedProperties = EntityHelper.GetNonComputedProperties<T>();

        #region Helper Methods

        protected void AddEntityParameters(DbCommand cmd, T entity, string parameterPrefix = "p")
        {
            for (int i = 0; i < _nonComputedProperties.Length; i++)
            {
                PropertyInfo prop = _nonComputedProperties[i];
                object? rawValue = prop.GetValue(entity);
                SqlCommandHelper.AddParameter(cmd, $"@{parameterPrefix}{i}", rawValue, prop.PropertyType);
            }
        }

        protected void AddKeyParameters(DbCommand cmd, T entity, string parameterPrefix = "key")
        {
            for (int i = 0; i < _keyProperties.Length; i++)
            {
                PropertyInfo keyProp = _keyProperties[i];
                object? rawValue = keyProp.GetValue(entity);
                SqlCommandHelper.AddParameter(cmd, $"@{parameterPrefix}{i}", rawValue, keyProp.PropertyType);
            }
        }

        protected async Task<int> ExecuteWithOutputAsync(DbCommand cmd, T entity, CancellationToken cancellationToken = default)
        {
            try
            {
                using DbDataReader reader = await cmd.ExecuteReaderAsync(cancellationToken);
                if (await reader.ReadAsync(cancellationToken))
                {
                    DataReaderMapper.UpdateComputedProperties(reader, entity, _computedProperties);
                }
                return 1;
            }
            catch (DbException ex)
            {
                throw SqlExceptionHelper.ToRepositoryException(ex);
            }
        }

        protected async Task<int> ExecuteBulkWithOutputAsync(DbCommand cmd, IList<T> entities, CancellationToken cancellationToken = default)
        {
            try
            {
                using DbDataReader reader = await cmd.ExecuteReaderAsync(cancellationToken);
                int row = 0;
                while (await reader.ReadAsync(cancellationToken))
                {
                    DataReaderMapper.UpdateComputedProperties(reader, entities[row], _computedProperties);
                    row++;
                }
                return row;
            }
            catch (DbException ex)
            {
                throw SqlExceptionHelper.ToRepositoryException(ex);
            }
        }

        protected static string BuildPagedSql(IList<Filter> filters, IList<SortOption> orders, int page, int pageSize)
        {
            string whereClause = filters != null && filters.Count > 0
                ? SqlCommandHelper.BuildFiltersClause<T>(filters)
                : string.Empty;
            string orderByClause = orders != null && orders.Count > 0
                ? SqlCommandHelper.BuildOrderByClause<T>(orders)
                : string.Empty;
            int offset = (page - 1) * pageSize;
            StringBuilder sql = new();
            _ = sql.Append($"SELECT * FROM grsdb.\"{_tableName}\"");
            if (!string.IsNullOrEmpty(whereClause))
            {
                _ = sql.Append($" WHERE {whereClause}");
            }

            if (!string.IsNullOrEmpty(orderByClause))
            {
                _ = sql.Append($" ORDER BY {orderByClause}");
            }

            _ = sql.Append($" LIMIT {pageSize} OFFSET {offset}");
            return sql.ToString();
        }

        protected static string BuildCountSql(IList<Filter> filters)
        {
            string whereClause = filters != null && filters.Count > 0
                ? SqlCommandHelper.BuildFiltersClause<T>(filters)
                : string.Empty;
            StringBuilder sql = new();
            _ = sql.Append($"SELECT COUNT(*) FROM grsdb.\"{_tableName}\"");
            if (!string.IsNullOrEmpty(whereClause))
            {
                _ = sql.Append($" WHERE {whereClause}");
            }

            return sql.ToString();
        }

        protected static object? ConvertToPropertyType(object? value, Type propType)
        {
            if (value == null)
            {
                return null;
            }

            Type targetType = Nullable.GetUnderlyingType(propType) ?? propType;

            if (targetType.IsEnum)
            {
                return value is string str1 ? Enum.Parse(targetType, str1) : Enum.ToObject(targetType, value);
            }
            if (targetType == typeof(Guid))
            {
                return value is string str2 ? Guid.Parse(str2) : value;
            }
            return targetType == typeof(DateTime)
                ? value is string str3 ? DateTime.Parse(str3) : Convert.ChangeType(value, targetType)
                : Convert.ChangeType(value, targetType);
        }

        protected static void AddFilterParameters<TResult>(DbCommand cmd, IList<Filter> filters, string paramPrefix = "p")
        {
            if (filters == null || filters.Count == 0)
            {
                return;
            }

            PropertyInfo[] resultProperties = typeof(TResult).GetProperties();

            int paramIndex = 0;
            foreach (Filter filter in filters)
            {
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
                    SqlCommandHelper.AddParameter(cmd, paramName + "_start", startValue, propType);
                    SqlCommandHelper.AddParameter(cmd, paramName + "_end", endValue, propType);
                }
                else if (filter.Operator is not FilterOperator.IsNullOrEmpty and not FilterOperator.IsNotNullOrEmpty)
                {
                    object? value = ConvertToPropertyType(filter.Value, propType);
                    SqlCommandHelper.AddParameter(cmd, paramName, value, propType);
                }
                paramIndex++;
            }
        }

        protected async Task<PagedResult<TResult>> MapPagedResultAsync<TResult>(
            DbCommand cmd,
            DbCommand countCmd,
            Func<DbDataReader, TResult> mapFunc,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            List<TResult> items = [];
            using (DbDataReader reader = await SqlCommandHelper.ExecuteReaderAsync(cmd, cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    items.Add(mapFunc(reader));
                }
            }

            long totalCount = (long)(await SqlCommandHelper.ExecuteScalarAsync(countCmd, cancellationToken))!;
            return new PagedResult<TResult>
            {
                Items = items,
                TotalCount = (int)totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        #endregion

        // TODO/FIX: "using" cierra la conexión y reader, pero puede ocurrir que tales deban ser administrados externamente al
        // repositorio. Decidir como arreglar esto (quizás no hace falta).

        public async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            using DbConnection conn = await SqlCommandHelper.CreateAndOpenConnectionAsync(connectionFactory, cancellationToken);
            using DbCommand cmd = SqlCommandHelper.CreateCommand(conn);

            cmd.CommandText = $"SELECT * FROM grsdb.\"{_tableName}\"";
            using DbDataReader reader = await SqlCommandHelper.ExecuteReaderAsync(cmd, cancellationToken);
            List<T> result = [];
            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(DataReaderMapper.MapReaderToEntity<T>(reader, _properties));
            }
            return result;
        }

        public async Task<int> CreateAsync(T entity, CancellationToken cancellationToken = default)
        {
            using DbConnection conn = await SqlCommandHelper.CreateAndOpenConnectionAsync(connectionFactory, cancellationToken);
            using DbCommand cmd = SqlCommandHelper.CreateCommand(conn);

            bool hasOutput = _computedProperties.Length != 0;
            cmd.CommandText = SqlCommandHelper.BuildInsertStatement(_tableName, _nonComputedProperties, _computedProperties, (p, i) => $"@p{i}");

            AddEntityParameters(cmd, entity);

            return hasOutput
                ? await ExecuteWithOutputAsync(cmd, entity, cancellationToken)
                : await SqlCommandHelper.ExecuteNonQueryAsync(cmd, cancellationToken);
        }

        public async Task<int> CreateBulkAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            IList<T> entityList = entities as IList<T> ?? [.. entities];
            if (entityList.Count == 0)
            {
                return 0;
            }

            if (entityList.Count == 1)
            {
                return await CreateAsync(entityList[0], cancellationToken);
            }

            using DbConnection conn = await SqlCommandHelper.CreateAndOpenConnectionAsync(connectionFactory, cancellationToken);
            using DbCommand cmd = SqlCommandHelper.CreateCommand(conn);

            string[] columns = [.. _nonComputedProperties.Select(EntityHelper.GetColumnName)];
            bool hasOutput = _computedProperties.Length != 0;
            StringBuilder valueRows = new();

            for (int e = 0; e < entityList.Count; e++)
            {
                List<string> valueNames = [];
                for (int i = 0; i < _nonComputedProperties.Length; i++)
                {
                    PropertyInfo prop = _nonComputedProperties[i];
                    string paramName = $"@p{e}_{i}";
                    object? rawValue = prop.GetValue(entityList[e]);
                    SqlCommandHelper.AddParameter(cmd, paramName, rawValue, prop.PropertyType);
                    valueNames.Add(paramName);
                }
                if (e > 0)
                {
                    _ = valueRows.Append(',');
                }
                _ = valueRows.Append("(" + string.Join(",", valueNames) + ")");
            }
            cmd.CommandText = hasOutput
                ? $"INSERT INTO grsdb.\"{_tableName}\" (" + string.Join(", ", columns.Select(c => $"\"{c}\"")) + ") VALUES " + valueRows.ToString() + " " + SqlCommandHelper.BuildReturningClause(_computedProperties)
                : $"INSERT INTO grsdb.\"{_tableName}\" (" + string.Join(", ", columns.Select(c => $"\"{c}\"")) + ") VALUES " + valueRows.ToString();

            return hasOutput
                ? await ExecuteBulkWithOutputAsync(cmd, entityList, cancellationToken)
                : await SqlCommandHelper.ExecuteNonQueryAsync(cmd, cancellationToken);
        }

        public async Task<int> UpdateAsync(T entity, Func<T, object?>? selector = null, CancellationToken cancellationToken = default)
        {
            using DbConnection conn = await SqlCommandHelper.CreateAndOpenConnectionAsync(connectionFactory, cancellationToken);
            using DbCommand cmd = SqlCommandHelper.CreateCommand(conn);

            PropertyInfo[] setColumns;
            if (selector == null)
            {
                setColumns = [.. _nonComputedProperties.Where(p => !_keyProperties.Contains(p))];
            }
            else
            {
                object? selected = selector(entity);
                setColumns = ReflectionHelpers.GetSelectedProperties<T>(selected, _nonComputedProperties, _keyProperties);
            }
            bool hasOutput = _computedProperties.Length != 0;

            cmd.CommandText = SqlCommandHelper.BuildUpdateStatement(_tableName, setColumns, _keyProperties, _computedProperties, (p, i) => $"@set{i}", (p, i) => $"@key{i}");

            int idx = 0;
            foreach (PropertyInfo p in setColumns)
            {
                object? rawValue = p.GetValue(entity);
                SqlCommandHelper.AddParameter(cmd, $"@set{idx}", rawValue, p.PropertyType);
                idx++;
            }

            AddKeyParameters(cmd, entity);

            return hasOutput
                ? await ExecuteWithOutputAsync(cmd, entity, cancellationToken)
                : await SqlCommandHelper.ExecuteNonQueryAsync(cmd, cancellationToken);
        }

        public async Task<int> UpdateBulkAsync(IEnumerable<T> entities, Func<T, object?>? selector = null, CancellationToken cancellationToken = default)
        {
            IList<T> entityList = entities as IList<T> ?? [.. entities];
            if (entityList.Count == 0)
            {
                return 0;
            }

            if (entityList.Count == 1)
            {
                return await UpdateAsync(entityList[0], selector, cancellationToken);
            }

            using DbConnection conn = await SqlCommandHelper.CreateAndOpenConnectionAsync(connectionFactory, cancellationToken);
            using DbCommand cmd = SqlCommandHelper.CreateCommand(conn);

            PropertyInfo[] setColumns;
            if (selector == null)
            {
                setColumns = [.. _nonComputedProperties.Where(p => !_keyProperties.Contains(p))];
            }
            else
            {
                object? selected = selector(entityList[0]);
                setColumns = ReflectionHelpers.GetSelectedProperties<T>(selected, _nonComputedProperties, _keyProperties);
            }
            bool hasOutput = _computedProperties.Length != 0;
            List<string> setClauses = [];

            for (int s = 0; s < setColumns.Length; s++)
            {
                PropertyInfo setCol = setColumns[s];
                string colName = EntityHelper.GetColumnName(setCol);
                StringBuilder caseExpr = new($"\"{colName}\" = CASE");
                for (int i = 0; i < entityList.Count; i++)
                {
                    List<string> whenParts = [];
                    for (int k = 0; k < _keyProperties.Length; k++)
                    {
                        PropertyInfo keyProp = _keyProperties[k];
                        string keyCol = EntityHelper.GetColumnName(keyProp);
                        string paramName = $"@key{i}_{k}";
                        object? rawValue = keyProp.GetValue(entityList[i]);
                        SqlCommandHelper.AddParameter(cmd, paramName, rawValue, keyProp.PropertyType);
                        whenParts.Add($"\"{keyCol}\" = {paramName}");
                    }
                    string valueParam = $"@set{i}_{colName}";
                    object? rawSetValue = setCol.GetValue(entityList[i]);
                    SqlCommandHelper.AddParameter(cmd, valueParam, rawSetValue, setCol.PropertyType);
                    _ = caseExpr.Append($" WHEN {string.Join(" AND ", whenParts)} THEN {valueParam}");
                }
                _ = caseExpr.Append($" ELSE \"{colName}\" END");
                setClauses.Add(caseExpr.ToString());
            }
            string whereClause = SqlCommandHelper.BuildBulkWhereClause(entityList, _keyProperties, cmd, "wkey");
            cmd.CommandText = hasOutput
                ? $"UPDATE grsdb.\"{_tableName}\" SET " + string.Join(",", setClauses) + " WHERE " + whereClause + " " + SqlCommandHelper.BuildReturningClause(_computedProperties)
                : $"UPDATE grsdb.\"{_tableName}\" SET " + string.Join(",", setClauses) + " WHERE " + whereClause;

            return hasOutput
                ? await ExecuteBulkWithOutputAsync(cmd, entityList, cancellationToken)
                : await SqlCommandHelper.ExecuteNonQueryAsync(cmd, cancellationToken);
        }

        public async Task<int> DeleteAsync(T entity, CancellationToken cancellationToken = default)
        {
            using DbConnection conn = await SqlCommandHelper.CreateAndOpenConnectionAsync(connectionFactory, cancellationToken);
            using DbCommand cmd = SqlCommandHelper.CreateCommand(conn);

            cmd.CommandText = SqlCommandHelper.BuildDeleteStatement(_tableName, _keyProperties, (p, i) => $"@key{i}");

            AddKeyParameters(cmd, entity);

            return await SqlCommandHelper.ExecuteNonQueryAsync(cmd, cancellationToken);
        }

        public async Task<int> DeleteBulkAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            IList<T> entityList = entities as IList<T> ?? [.. entities];
            if (entityList.Count == 0)
            {
                return 0;
            }

            if (entityList.Count == 1)
            {
                return await DeleteAsync(entityList[0], cancellationToken);
            }

            using DbConnection conn = await SqlCommandHelper.CreateAndOpenConnectionAsync(connectionFactory, cancellationToken);
            using DbCommand cmd = SqlCommandHelper.CreateCommand(conn);

            string whereClause = SqlCommandHelper.BuildBulkWhereClause(entityList, _keyProperties, cmd, "key");
            cmd.CommandText = $"DELETE FROM grsdb.\"{_tableName}\" WHERE " + whereClause;

            return await SqlCommandHelper.ExecuteNonQueryAsync(cmd, cancellationToken);
        }
    }
}