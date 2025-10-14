using GeneralReservationSystem.Application.Common;
using System.Linq.Expressions;
using System.Reflection;

namespace GeneralReservationSystem.Application.Helpers
{
    public static class EntityHelper
    {
        public static string GetTableName(Type entityType)
        {
            TableNameAttribute? attr = entityType.TryGetAttribute<TableNameAttribute>();

            return attr?.Name ?? entityType.Name;
        }

        public static string GetTableName<TEntity>()
        {
            return GetTableName(typeof(TEntity));
        }

        public static PropertyInfo[] GetKeyProperties<TEntity>()
        {
            PropertyInfo[] keys = ReflectionHelpers.GetPropertiesWithAttribute<TEntity, KeyAttribute>();

            return keys.Length == 0
                ? throw new InvalidOperationException($"Entity {typeof(TEntity).Name} must have at least one [Key] property.")
                : keys;
        }

        public static PropertyInfo[] GetComputedProperties<TEntity>()
        {
            return ReflectionHelpers.GetPropertiesWithAttribute<TEntity, ComputedAttribute>();
        }

        public static PropertyInfo[] GetNonComputedProperties<TEntity>()
        {
            return ReflectionHelpers.GetPropertiesWithoutAttribute<TEntity, ComputedAttribute>();
        }

        public static string GetColumnName<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression)
        {
            ThrowHelpers.ThrowIfNull(propertyExpression, nameof(propertyExpression));

            MemberExpression? me = propertyExpression.Body switch
            {
                MemberExpression m => m,
                UnaryExpression u when u.Operand is MemberExpression m => m,
                MethodCallExpression mc when mc.Object is MemberExpression m => m,
                _ => null
            } ?? throw new ArgumentException("Expression must be a member expression e => e.m", nameof(propertyExpression));

            return me.Member is not PropertyInfo pi
                ? throw new ArgumentException($"{nameof(MemberExpression)} must refer to a property, not a method or a field", nameof(propertyExpression))
                : GetColumnName(pi);
        }

        public static string GetColumnName(PropertyInfo prop)
        {
            //TODO: Tal vez sea mas conveniente tirar una excepcion si no tiene el atributo
            return prop.GetCustomAttribute<ColumnNameAttribute>()?.Name ?? prop.Name;
        }
    }
}
