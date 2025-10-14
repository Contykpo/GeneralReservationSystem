using System.Data.SqlTypes;

namespace GeneralReservationSystem.Infrastructure.Helpers
{
    public static class EntityTypeConverter
    {
        public static object? ConvertToDbValue(object? clrValue, Type targetType)
        {
            if (clrValue == null)
            {
                return DBNull.Value;
            }

            Type underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

            // Handle DateTime / DateTimeOffset bounds to avoid SqlDateTime overflow when sending to SQL Server
            if (underlying == typeof(DateTime) && clrValue is DateTime dt)
            {
                DateTime sqlMin = SqlDateTime.MinValue.Value;
                DateTime sqlMax = SqlDateTime.MaxValue.Value;
                return (dt < sqlMin || dt > sqlMax) ? DBNull.Value : (object)dt;
            }

            if (underlying == typeof(DateTimeOffset) && clrValue is DateTimeOffset dto)
            {
                DateTime sqlMin = SqlDateTime.MinValue.Value;
                DateTime sqlMax = SqlDateTime.MaxValue.Value;
                DateTime utc = dto.UtcDateTime;
                return (utc < sqlMin || utc > sqlMax) ? DBNull.Value : (object)dto;
            }

            if (underlying == typeof(byte[]))
            {
                return clrValue == null ? DBNull.Value : clrValue is byte[] bytes && bytes.Length == 0 ? DBNull.Value : clrValue;
            }

            if (underlying == typeof(TimeZoneInfo))
            {
                if (clrValue is TimeZoneInfo tzi)
                {
                    return tzi.Id;
                }

                string? strValue = clrValue.ToString();
                return string.IsNullOrEmpty(strValue) ? DBNull.Value : strValue;
            }

            if (underlying.IsEnum)
            {
                try
                {
                    return Convert.ChangeType(clrValue, Enum.GetUnderlyingType(underlying));
                }
                catch
                {
                    return DBNull.Value;
                }
            }

            return clrValue;
        }

        public static object? ConvertFromDbValue(object? dbValue, Type targetType)
        {
            if (dbValue == null || dbValue == DBNull.Value)
            {
                return GetDefaultValue(targetType);
            }

            Type underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (underlying == typeof(TimeZoneInfo))
            {
                if (dbValue is TimeZoneInfo tzi)
                {
                    return tzi;
                }

                string? timeZoneId = dbValue?.ToString();
                if (string.IsNullOrEmpty(timeZoneId))
                {
                    return TimeZoneInfo.Utc;
                }

                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                }
                catch
                {
                    try
                    {
                        TimeZoneInfo? tz = TimeZoneInfo.GetSystemTimeZones()
                            .FirstOrDefault(z => z.DisplayName == timeZoneId ||
                                               z.StandardName == timeZoneId ||
                                               z.DaylightName == timeZoneId);
                        if (tz != null)
                        {
                            return tz;
                        }
                    }
                    catch { }

                    return TimeZoneInfo.Utc;
                }
            }

            if (underlying.IsEnum)
            {
                try
                {
                    if (dbValue is string str)
                    {
                        return Enum.Parse(underlying, str);
                    }

                    object value = Convert.ChangeType(dbValue, Enum.GetUnderlyingType(underlying));
                    return Enum.ToObject(underlying, value!);
                }
                catch
                {
                    return GetDefaultValue(targetType);
                }
            }

            try
            {
                return Convert.ChangeType(dbValue, underlying);
            }
            catch
            {
                return IsCompatibleType(dbValue.GetType(), underlying)
                    ? dbValue
                    : GetDefaultValue(targetType);
            }
        }

        public static bool IsScalar(Type type)
        {
            Type underlying = Nullable.GetUnderlyingType(type) ?? type;

            return underlying.IsPrimitive ||
                   underlying == typeof(string) ||
                   underlying == typeof(decimal) ||
                   underlying == typeof(DateTime) ||
                   underlying == typeof(DateTimeOffset) ||
                   underlying == typeof(TimeSpan) ||
                   underlying == typeof(Guid) ||
                   underlying.IsValueType;
        }

        private static object? GetDefaultValue(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        private static bool IsCompatibleType(Type sourceType, Type targetType)
        {
            return targetType.IsAssignableFrom(sourceType);
        }
    }
}
