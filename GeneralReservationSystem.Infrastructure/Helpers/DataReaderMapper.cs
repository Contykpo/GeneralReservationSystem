using GeneralReservationSystem.Application.Helpers;
using System.Data.Common;
using System.Reflection;

namespace GeneralReservationSystem.Infrastructure.Helpers
{
    public static class DataReaderMapper
    {
        public static T MapReaderToEntity<T>(DbDataReader reader, PropertyInfo[] properties) where T : class, new()
        {
            var entity = new T();
            foreach (var prop in properties)
            {
                var colName = EntityHelper.GetColumnName(prop);
                var ordinal = reader.GetOrdinal(colName);
                if (!reader.IsDBNull(ordinal))
                {
                    var dbValue = reader.GetValue(ordinal);
                    var convertedValue = EntityTypeConverter.ConvertFromDbValue(dbValue, prop.PropertyType);
                    prop.SetValue(entity, convertedValue);
                }
            }
            return entity;
        }

        public static void UpdateComputedProperties<T>(DbDataReader reader, T entity, PropertyInfo[] computedProperties)
        {
            for (int i = 0; i < computedProperties.Length; i++)
            {
                if (!reader.IsDBNull(i))
                {
                    var dbValue = reader.GetValue(i);
                    var convertedValue = EntityTypeConverter.ConvertFromDbValue(dbValue, computedProperties[i].PropertyType);
                    computedProperties[i].SetValue(entity, convertedValue);
                }
            }
        }

        public static T MapReaderToEntityWithAliases<T>(
            DbDataReader reader,
            IReadOnlyList<(string Column, string Alias)> selectedColumns,
            bool selectAll)
        {
            var targetType = typeof(T);
            var props = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            if (EntityTypeConverter.IsScalar(targetType))
            {
                var val = reader.IsDBNull(0) ? default : reader.GetValue(0);
                if (val is null || val == DBNull.Value)
                {
                    if (default(T) is null)
                    {
                        return default!;
                    }
                    throw new InvalidOperationException("Cannot map null value to non-nullable scalar type.");
                }
                return (T)Convert.ChangeType(val, targetType)!;
            }

            var instance = Activator.CreateInstance<T>() ?? throw new InvalidOperationException($"Cannot create instance of type {targetType.Name}");
            if (selectAll)
            {
                for (int i = 0; i < selectedColumns.Count; i++)
                {
                    var alias = selectedColumns[i].Alias;
                    var prop = props.FirstOrDefault(p => p.Name == alias);
                    if (prop == null) continue;
                    if (!reader.IsDBNull(i))
                    {
                        var raw = reader.GetValue(i);
                        var converted = EntityTypeConverter.ConvertFromDbValue(raw, prop.PropertyType);
                        prop.SetValue(instance, converted);
                    }
                }
            }
            else
            {
                for (int i = 0; i < selectedColumns.Count; i++)
                {
                    var alias = selectedColumns[i].Alias;
                    var prop = props.FirstOrDefault(p => p.Name == alias);
                    if (prop != null && !reader.IsDBNull(i))
                    {
                        var raw = reader.GetValue(i);
                        var converted = EntityTypeConverter.ConvertFromDbValue(raw, prop.PropertyType);
                        prop.SetValue(instance, converted);
                    }
                }
            }

            return instance;
        }

        public static async Task<T> MapReaderToEntityWithAliasesAsync<T>(
            DbDataReader reader,
            IReadOnlyList<(string Column, string Alias)> selectedColumns,
            bool selectAll,
            CancellationToken cancellationToken = default)
        {
            var targetType = typeof(T);
            var props = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            if (EntityTypeConverter.IsScalar(targetType))
            {
                var val = await reader.IsDBNullAsync(0, cancellationToken) ? default : reader.GetValue(0);
                if (val is null || val == DBNull.Value)
                {
                    if (default(T) is null)
                    {
                        return default!;
                    }
                    throw new InvalidOperationException("Cannot map null value to non-nullable scalar type.");
                }
                return (T)Convert.ChangeType(val, targetType)!;
            }

            var instance = Activator.CreateInstance<T>() ?? throw new InvalidOperationException($"Cannot create instance of type {targetType.Name}");
            if (selectAll)
            {
                for (int i = 0; i < selectedColumns.Count; i++)
                {
                    var alias = selectedColumns[i].Alias;
                    var prop = props.FirstOrDefault(p => p.Name == alias);
                    if (prop == null) continue;
                    if (!await reader.IsDBNullAsync(i, cancellationToken))
                    {
                        var raw = reader.GetValue(i);
                        var converted = EntityTypeConverter.ConvertFromDbValue(raw, prop.PropertyType);
                        prop.SetValue(instance, converted);
                    }
                }
            }
            else
            {
                for (int i = 0; i < selectedColumns.Count; i++)
                {
                    var alias = selectedColumns[i].Alias;
                    var prop = props.FirstOrDefault(p => p.Name == alias);
                    if (prop != null && !await reader.IsDBNullAsync(i, cancellationToken))
                    {
                        var raw = reader.GetValue(i);
                        var converted = EntityTypeConverter.ConvertFromDbValue(raw, prop.PropertyType);
                        prop.SetValue(instance, converted);
                    }
                }
            }

            return instance;
        }
    }
}
