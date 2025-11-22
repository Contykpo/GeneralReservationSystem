using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.Entities.Authentication;
using GeneralReservationSystem.Application.DTOs.Authentication;
using System.Linq.Expressions;
using System.Reflection;

namespace GeneralReservationSystem.Application.Helpers
{
    public static class EntityHelper
    {
        public static UserInfo GetUserInfo(this User usr) => new UserInfo(usr);

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

        public static string GetColumnName(MemberInfo member)
        {
            // Default to member name if no ColumnName attribute is found.
            return member.GetCustomAttribute<ColumnNameAttribute>()?.Name ?? member.Name;
        }
    }
}
