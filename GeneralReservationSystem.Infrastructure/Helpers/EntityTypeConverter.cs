namespace GeneralReservationSystem.Infrastructure.Helpers
{
    public static class EntityTypeConverter
    {
        public static object? ConvertToDbValue(object? clrValue, Type targetType)
        {
            if (clrValue == null) return DBNull.Value;

            var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (underlying == typeof(TimeZoneInfo))
            {
                if (clrValue is TimeZoneInfo tzi)
                    return tzi.Id;

                var strValue = clrValue.ToString();
                return string.IsNullOrEmpty(strValue) ? (object)DBNull.Value : strValue;
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
                return GetDefaultValue(targetType);

            var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (underlying == typeof(TimeZoneInfo))
            {
                if (dbValue is TimeZoneInfo tzi)
                    return tzi;

                var timeZoneId = dbValue?.ToString();
                if (string.IsNullOrEmpty(timeZoneId))
                    return TimeZoneInfo.Utc;

                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                }
                catch
                {
                    try
                    {
                        var tz = TimeZoneInfo.GetSystemTimeZones()
                            .FirstOrDefault(z => z.DisplayName == timeZoneId ||
                                               z.StandardName == timeZoneId ||
                                               z.DaylightName == timeZoneId);
                        if (tz != null) return tz;
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
                        return Enum.Parse(underlying, str);

                    var value = Convert.ChangeType(dbValue, Enum.GetUnderlyingType(underlying));
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
            var underlying = Nullable.GetUnderlyingType(type) ?? type;

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
            if (type.IsValueType)
                return Activator.CreateInstance(type);

            return null;
        }

        private static bool IsCompatibleType(Type sourceType, Type targetType)
        {
            return targetType.IsAssignableFrom(sourceType);
        }
    }
}
