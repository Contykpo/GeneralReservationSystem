using GeneralReservationSystem.Application.Helpers;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using GeneralReservationSystem.Application.Repositories.Util.Interfaces;
using GeneralReservationSystem.Infrastructure.Helpers;
using GeneralReservationSystem.Infrastructure.Repositories.Util.Sql;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Text;

namespace GeneralReservationSystem.Infrastructure.Repositories.Sql
{
    public class Repository<T>(Func<DbConnection> connectionFactory, DbTransaction? transaction = null) : IRepository<T> where T : class, new()
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

        #endregion

        public IQuery<T> Query()
        {
            return new Query<T>(connectionFactory, transaction);
        }

        public async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            using DbConnection conn = await SqlCommandHelper.CreateAndOpenConnectionAsync(connectionFactory, cancellationToken);
            using DbCommand cmd = SqlCommandHelper.CreateCommand(conn, transaction);

            cmd.CommandText = $"SELECT * FROM [{_tableName}]";
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
            using DbCommand cmd = SqlCommandHelper.CreateCommand(conn, transaction);

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
            using DbCommand cmd = SqlCommandHelper.CreateCommand(conn, transaction);

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
                ? $"INSERT INTO [{_tableName}] (" + string.Join(",", columns) + ") " + SqlCommandHelper.BuildOutputClause(_computedProperties) + " VALUES " + valueRows.ToString()
                : $"INSERT INTO [{_tableName}] (" + string.Join(",", columns) + ") VALUES " + valueRows.ToString();

            return hasOutput
                ? await ExecuteBulkWithOutputAsync(cmd, entityList, cancellationToken)
                : await SqlCommandHelper.ExecuteNonQueryAsync(cmd, cancellationToken);
        }

        public async Task<int> UpdateAsync(T entity, Func<T, object?>? selector = null, CancellationToken cancellationToken = default)
        {
            using DbConnection conn = await SqlCommandHelper.CreateAndOpenConnectionAsync(connectionFactory, cancellationToken);
            using DbCommand cmd = SqlCommandHelper.CreateCommand(conn, transaction);

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
            using DbCommand cmd = SqlCommandHelper.CreateCommand(conn, transaction);

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
                StringBuilder caseExpr = new($"{colName} = CASE");
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
                        whenParts.Add($"[{keyCol}] = {paramName}");
                    }
                    string valueParam = $"@set{i}_{colName}";
                    object? rawSetValue = setCol.GetValue(entityList[i]);
                    SqlCommandHelper.AddParameter(cmd, valueParam, rawSetValue, setCol.PropertyType);
                    _ = caseExpr.Append($" WHEN {string.Join(" AND ", whenParts)} THEN {valueParam}");
                }
                _ = caseExpr.Append($" ELSE [{colName}] END");
                setClauses.Add(caseExpr.ToString());
            }

            string whereClause = SqlCommandHelper.BuildBulkWhereClause(entityList, _keyProperties, cmd, "wkey");
            cmd.CommandText = hasOutput
                ? $"UPDATE [{_tableName}] SET " + string.Join(",", setClauses) + " " + SqlCommandHelper.BuildOutputClause(_computedProperties) + " WHERE " + whereClause
                : $"UPDATE [{_tableName}] SET " + string.Join(",", setClauses) + " WHERE " + whereClause;

            return hasOutput
                ? await ExecuteBulkWithOutputAsync(cmd, entityList, cancellationToken)
                : await SqlCommandHelper.ExecuteNonQueryAsync(cmd, cancellationToken);
        }

        public async Task<int> DeleteAsync(T entity, CancellationToken cancellationToken = default)
        {
            using DbConnection conn = await SqlCommandHelper.CreateAndOpenConnectionAsync(connectionFactory, cancellationToken);
            using DbCommand cmd = SqlCommandHelper.CreateCommand(conn, transaction);

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
            using DbCommand cmd = SqlCommandHelper.CreateCommand(conn, transaction);

            string whereClause = SqlCommandHelper.BuildBulkWhereClause(entityList, _keyProperties, cmd, "key");
            cmd.CommandText = $"DELETE FROM [{_tableName}] WHERE " + whereClause;

            return await SqlCommandHelper.ExecuteNonQueryAsync(cmd, cancellationToken);
        }
    }
}