using System.Collections.Concurrent;
using System.Reflection;

namespace GeneralReservationSystem.Application.Helpers
{
    public static class ReflectionHelpers
    {
        public static ConcurrentDictionary<Type, PropertyInfo[]> TypePropertiesCache { get; private set; } = new();
        public static ConcurrentDictionary<MemberInfo, Attribute[]> AttributeCache { get; private set; } = new();

        public static PropertyInfo[] GetSelectedProperties<T>(object? selectorResult, PropertyInfo[] allProps, PropertyInfo[] keyProps)
        {
            if (selectorResult == null)
            {
                return [.. allProps.Where(p => !keyProps.Contains(p))];
            }

            Type entityType = typeof(T);
            Type selectedType = selectorResult.GetType();

            if (selectedType == entityType)
            {
                return [.. allProps.Where(p => !keyProps.Contains(p))];
            }

            if (selectedType.IsPrimitive || selectedType == typeof(string))
            {
                PropertyInfo? match = allProps.FirstOrDefault(p => !keyProps.Contains(p) && p.PropertyType == selectedType);
                return match != null ? [match] : [];
            }

            HashSet<string> selectedNames = [.. selectedType.GetProperties().Select(p => p.Name)];
            return [.. allProps.Where(p => !keyProps.Contains(p) && selectedNames.Contains(p.Name))];
        }

        public static PropertyInfo[] GetProperties(this Type type)
        {
            return TypePropertiesCache.GetOrAdd(type, t => t.GetProperties());
        }

        public static PropertyInfo[] GetProperties<TEntity>()
        {
            return GetProperties(typeof(TEntity));
        }

        public static PropertyInfo GetProperty(this Type type, string propertyName)
        {
            ThrowHelpers.ThrowIfNullOrWhiteSpace(propertyName, nameof(propertyName));

            PropertyInfo? propertyInfo = GetProperties(type).FirstOrDefault(p => p.Name == propertyName);

            return propertyInfo ?? throw new ArgumentException($"Property '{propertyName}' not found on type '{type.FullName}'", nameof(propertyName));
        }

        public static PropertyInfo[] GetFilteredProperties<TEntity>(Func<PropertyInfo, bool> predicate)
        {
            ThrowHelpers.ThrowIfNull(predicate, nameof(predicate));
            return [.. GetProperties<TEntity>().Where(predicate)];
        }

        public static PropertyInfo[] GetPropertiesWithAttribute<TEntity, TAttribute>()
            where TAttribute : Attribute
        {
            return GetFilteredProperties<TEntity>(p => p.HasAttribute<TAttribute>());
        }

        public static PropertyInfo[] GetPropertiesWithoutAttribute<TEntity, TAttribute>()
            where TAttribute : Attribute
        {
            return GetFilteredProperties<TEntity>(p => !p.HasAttribute<TAttribute>());
        }

        public static Attribute[] GetPropertyAttributes(this PropertyInfo propertyInfo)
        {
            ThrowHelpers.ThrowIfNull(propertyInfo, nameof(propertyInfo));
            return AttributeCache.GetOrAdd(propertyInfo, p => [.. propertyInfo.GetCustomAttributes()]);
        }

        public static Attribute[] GetTypeAttributes(this Type type)
        {
            ThrowHelpers.ThrowIfNull(type, nameof(type));
            return AttributeCache.GetOrAdd(type, t => [.. type.GetCustomAttributes()]);
        }

        public static Attribute[] GetTypeAttributes<TEntity>()
        {
            return GetTypeAttributes(typeof(TEntity));
        }

        public static Attribute[] GetAttributes(this MemberInfo attrProvider)
        {
            ThrowHelpers.ThrowIfNull(attrProvider, nameof(attrProvider));
            return attrProvider switch
            {
                PropertyInfo p => p.GetPropertyAttributes(),
                Type t => t.GetTypeAttributes(),
                _ => throw new ArgumentException($"{nameof(attrProvider)} must be of type {nameof(PropertyInfo)} or {nameof(Type)}", nameof(attrProvider))
            };
        }

        public static bool HasAttribute<TAttribute>(this MemberInfo attrProvider)
            where TAttribute : Attribute
        {
            return attrProvider.GetAttributes().Any(a => a is TAttribute);
        }

        public static TAttribute? TryGetAttribute<TAttribute>(this MemberInfo attrProvider)
            where TAttribute : Attribute
        {
            return attrProvider.GetAttributes().OfType<TAttribute>().FirstOrDefault();
        }
    }
}