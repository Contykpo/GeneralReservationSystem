using GeneralReservationSystem.Application.Helpers;
using System.Data.Common;
using System.Reflection;

namespace GeneralReservationSystem.Infrastructure.Helpers
{
    public static class DataReaderMapper
    {
        public static T MapReaderToEntity<T>(DbDataReader reader, PropertyInfo[] properties) where T : class, new()
        {
            T entity = new();
            foreach (PropertyInfo prop in properties)
            {
                string colName = EntityHelper.GetColumnName(prop);
                int ordinal = reader.GetOrdinal(colName);
                if (!reader.IsDBNull(ordinal))
                {
                    object dbValue = reader.GetValue(ordinal);
                    object? convertedValue = EntityTypeConverter.ConvertFromDbValue(dbValue, prop.PropertyType);
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
                    object dbValue = reader.GetValue(i);
                    object? convertedValue = EntityTypeConverter.ConvertFromDbValue(dbValue, computedProperties[i].PropertyType);
                    computedProperties[i].SetValue(entity, convertedValue);
                }
            }
        }

        public static T MapReaderToEntityWithAliases<T>(
            DbDataReader reader,
            IReadOnlyList<(string Column, string Alias)> selectedColumns,
            bool selectAll)
        {
            Type targetType = typeof(T);
            PropertyInfo[] props = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            if (EntityTypeConverter.IsScalar(targetType))
            {
                object? val = reader.IsDBNull(0) ? default : reader.GetValue(0);
                return val is null || val == DBNull.Value
                    ? default(T) is null ? default! : throw new InvalidOperationException("Cannot map null value to non-nullable scalar type.")
                    : (T)Convert.ChangeType(val, targetType)!;
            }

            T instance = Activator.CreateInstance<T>() ?? throw new InvalidOperationException($"Cannot create instance of type {targetType.Name}");
            if (selectAll)
            {
                for (int i = 0; i < selectedColumns.Count; i++)
                {
                    string alias = selectedColumns[i].Alias;
                    PropertyInfo? prop = props.FirstOrDefault(p => p.Name == alias);
                    if (prop == null)
                    {
                        continue;
                    }

                    if (!reader.IsDBNull(i))
                    {
                        object raw = reader.GetValue(i);
                        object? converted = EntityTypeConverter.ConvertFromDbValue(raw, prop.PropertyType);
                        prop.SetValue(instance, converted);
                    }
                }
            }
            else
            {
                for (int i = 0; i < selectedColumns.Count; i++)
                {
                    string alias = selectedColumns[i].Alias;
                    PropertyInfo? prop = props.FirstOrDefault(p => p.Name == alias);
                    if (prop != null && !reader.IsDBNull(i))
                    {
                        object raw = reader.GetValue(i);
                        object? converted = EntityTypeConverter.ConvertFromDbValue(raw, prop.PropertyType);
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
            Type targetType = typeof(T);
            PropertyInfo[] props = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            if (EntityTypeConverter.IsScalar(targetType))
            {
                object? val = await reader.IsDBNullAsync(0, cancellationToken) ? default : reader.GetValue(0);
                return val is null || val == DBNull.Value
                    ? default(T) is null ? default! : throw new InvalidOperationException("Cannot map null value to non-nullable scalar type.")
                    : (T)Convert.ChangeType(val, targetType)!;
            }

            T instance = Activator.CreateInstance<T>() ?? throw new InvalidOperationException($"Cannot create instance of type {targetType.Name}");
            if (selectAll)
            {
                for (int i = 0; i < selectedColumns.Count; i++)
                {
                    string alias = selectedColumns[i].Alias;
                    PropertyInfo? prop = props.FirstOrDefault(p => p.Name == alias);
                    if (prop == null)
                    {
                        continue;
                    }

                    if (!await reader.IsDBNullAsync(i, cancellationToken))
                    {
                        object raw = reader.GetValue(i);
                        object? converted = EntityTypeConverter.ConvertFromDbValue(raw, prop.PropertyType);
                        prop.SetValue(instance, converted);
                    }
                }
            }
            else
            {
                for (int i = 0; i < selectedColumns.Count; i++)
                {
                    string alias = selectedColumns[i].Alias;
                    PropertyInfo? prop = props.FirstOrDefault(p => p.Name == alias);
                    if (prop != null && !await reader.IsDBNullAsync(i, cancellationToken))
                    {
                        object raw = reader.GetValue(i);
                        object? converted = EntityTypeConverter.ConvertFromDbValue(raw, prop.PropertyType);
                        prop.SetValue(instance, converted);
                    }
                }
            }

            return instance;
        }
    }
}
