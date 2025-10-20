using System.Linq.Expressions;

namespace GeneralReservationSystem.Application.Common
{
    public enum SortDirection
    {
        Asc,
        Desc
    }

    public sealed record SortOption(string PropertyOrField, SortDirection Direction = SortDirection.Asc)
    {
        public Expression<Func<T, object>> ToExpression<T>()
        {
            ParameterExpression parameter = Expression.Parameter(typeof(T), "x");
            MemberExpression property = Expression.PropertyOrField(parameter, PropertyOrField);
            return Expression.Lambda<Func<T, object>>(Expression.Convert(property, typeof(object)), parameter);
        }
    }
}
