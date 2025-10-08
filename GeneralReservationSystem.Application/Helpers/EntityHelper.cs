using GeneralReservationSystem.Application.Common;
using System.Linq.Expressions;
using System.Reflection;

namespace GeneralReservationSystem.Application.Helpers
{
    public static class EntityHelper
    {
        public static string GetTableName<TEntity>()
        {
            var attr = typeof(TEntity).GetCustomAttribute<TableNameAttribute>();

            if (attr == null)
                return typeof(TEntity).Name;

            return attr.Name;
        }

        public static string GetTableName(Type entityType)
        {
            var attr = entityType.GetCustomAttribute<TableNameAttribute>();

            if (attr == null)
                return entityType.Name;

            return attr.Name;
        }

        public static PropertyInfo[] GetKeyProperties<TEntity>()
        {
            var keys = typeof(TEntity).GetProperties()
                .Where(p => p.GetCustomAttribute<KeyAttribute>() != null)
                .ToArray();

            if (!keys.Any())
                throw new InvalidOperationException($"Entity {typeof(TEntity).Name} must have at least one [Key] property.");

            return keys;
        }

        public static PropertyInfo[] GetComputedProperties<TEntity>()
        {
            return typeof(TEntity).GetProperties()
                .Where(p => p.GetCustomAttribute<ComputedAttribute>() != null)
                .ToArray();
        }

        public static PropertyInfo[] GetNonComputedProperties<TEntity>()
        {
            return typeof(TEntity).GetProperties()
                .Where(p => p.GetCustomAttribute<ComputedAttribute>() == null)
                .ToArray();
        }

        public static string GetColumnName<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression)
        {
            if (propertyExpression.Body is not MemberExpression member)
                throw new ArgumentException("Expression must be a member expression", nameof(propertyExpression));

            return GetColumnName((PropertyInfo)member.Member);
        }

        public static string GetColumnName(PropertyInfo prop) =>
            prop.GetCustomAttribute<ColumnNameAttribute>()?.Name ?? prop.Name;
    }
}
