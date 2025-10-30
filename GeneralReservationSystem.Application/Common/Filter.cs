using System.Linq.Expressions;

namespace GeneralReservationSystem.Application.Common
{
    public enum FilterOperator
    {
        Equals,
        NotEquals,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        Contains,
        NotContains,
        StartsWith,
        EndsWith,
        IsNullOrEmpty,
        IsNotNullOrEmpty,
        Between
    }

    public static class FilterOperatorExtensions
    {
        public static FilterOperator ToFilterOperator(string name)
        {
            return name switch
            {
                "equals" => FilterOperator.Equals,
                "not equals" => FilterOperator.NotEquals,
                "greater than" => FilterOperator.GreaterThan,
                "greater than or equal" => FilterOperator.GreaterThanOrEqual,
                "less than" => FilterOperator.LessThan,
                "less than or equal" => FilterOperator.LessThanOrEqual,
                "contains" => FilterOperator.Contains,
                "not contains" => FilterOperator.NotContains,
                "starts with" => FilterOperator.StartsWith,
                "ends with" => FilterOperator.EndsWith,
                "is empty" => FilterOperator.IsNullOrEmpty,
                "is not empty" => FilterOperator.IsNotNullOrEmpty,
                "between" => FilterOperator.Between,
                _ => FilterOperator.Equals
            };
        }

        public static string ToString(FilterOperator op)
        {
            return op switch
            {
                FilterOperator.Equals => "equals",
                FilterOperator.NotEquals => "not equals",
                FilterOperator.GreaterThan => "greater than",
                FilterOperator.GreaterThanOrEqual => "greater than or equal",
                FilterOperator.LessThan => "less than",
                FilterOperator.LessThanOrEqual => "less than or equal",
                FilterOperator.Contains => "contains",
                FilterOperator.NotContains => "not contains",
                FilterOperator.StartsWith => "starts with",
                FilterOperator.EndsWith => "ends with",
                FilterOperator.IsNullOrEmpty => "is empty",
                FilterOperator.IsNotNullOrEmpty => "is not empty",
                FilterOperator.Between => "between",
                _ => "equals"
            };
        }
    }

    public sealed record Filter(string PropertyOrField, FilterOperator Operator, object? Value)
    {
        private static object? ExtractValueFromJsonElement(System.Text.Json.JsonElement jsonElement, Type memberType)
        {
            return memberType switch
            {
                Type t when t == typeof(string) => jsonElement.GetString(),
                Type t when t == typeof(int) => jsonElement.GetInt32(),
                Type t when t == typeof(long) => jsonElement.GetInt64(),
                Type t when t == typeof(bool) => jsonElement.GetBoolean(),
                Type t when t == typeof(DateTime) => jsonElement.GetDateTime(),
                Type t when t.IsEnum => Enum.Parse(memberType, jsonElement.GetString() ?? string.Empty),
                Type t when t == typeof(double) => jsonElement.GetDouble(),
                Type t when t == typeof(decimal) => jsonElement.GetDecimal(),
                Type t when t == typeof(float) => jsonElement.GetSingle(),
                Type t when t == typeof(Guid) => jsonElement.GetGuid(),
                _ when jsonElement.ValueKind == System.Text.Json.JsonValueKind.Null => null,
                _ => throw new NotSupportedException($"Unsupported filter value type: {memberType}")
            };
        }

        public Expression<Func<T, bool>> ToExpression<T>()
        {
            ParameterExpression parameter = Expression.Parameter(typeof(T), "x");
            MemberExpression member = Expression.PropertyOrField(parameter, PropertyOrField);
            Type memberType = member.Type;

            if (Operator is FilterOperator.GreaterThan or FilterOperator.GreaterThanOrEqual or FilterOperator.LessThan or FilterOperator.LessThanOrEqual or FilterOperator.Between)
            {
                if (!typeof(IComparable).IsAssignableFrom(memberType) && !(memberType.IsGenericType && memberType.GetGenericTypeDefinition() == typeof(Nullable<>) && typeof(IComparable).IsAssignableFrom(Nullable.GetUnderlyingType(memberType)!)))
                {
                    throw new NotSupportedException($"Operator '{Operator}' is only supported on types implementing IComparable. PropertyOrField '{PropertyOrField}' is of type '{memberType}'.");
                }
            }
            else if (Operator is FilterOperator.Contains or FilterOperator.NotContains or FilterOperator.StartsWith or FilterOperator.EndsWith or FilterOperator.IsNullOrEmpty or FilterOperator.IsNotNullOrEmpty)
            {
                if (memberType != typeof(string))
                {
                    throw new NotSupportedException($"Operator '{Operator}' is only supported on string fields. PropertyOrField '{PropertyOrField}' is of type '{memberType}'.");
                }
            }

            object? actualValue = Value;
            if (Value is System.Text.Json.JsonElement jsonElement)
            {
                if (Operator == FilterOperator.Between && jsonElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    if (jsonElement.GetArrayLength() != 2)
                    {
                        throw new ArgumentException("Value for Between operator must be an array of two elements.");
                    }

                    object? lower = ExtractValueFromJsonElement(jsonElement[0], memberType);
                    object? upper = ExtractValueFromJsonElement(jsonElement[1], memberType);

                    if (lower is null || upper is null)
                    {
                        throw new ArgumentException("Both lower and upper bounds must be non-null for the Between operator.");
                    }

                    actualValue = new object[]
                    {
                        lower,
                        upper
                    };
                }
                else
                {
                    actualValue = ExtractValueFromJsonElement(jsonElement, memberType);
                }
            }

            if (Operator == FilterOperator.Between)
            {
                if (actualValue is object[] arr && arr.Length == 2)
                {
                    ConstantExpression lower = Expression.Constant(Convert.ChangeType(arr[0], memberType), memberType);
                    ConstantExpression upper = Expression.Constant(Convert.ChangeType(arr[1], memberType), memberType);
                    return Expression.Lambda<Func<T, bool>>(
                        Expression.AndAlso(
                            Expression.GreaterThanOrEqual(member, lower),
                            Expression.LessThanOrEqual(member, upper)
                        ), parameter);
                }
                throw new ArgumentException("Value for Between operator must be an array of two elements.");
            }

            ConstantExpression constant = Expression.Constant(Convert.ChangeType(actualValue, memberType), memberType);

            return Expression.Lambda<Func<T, bool>>(Operator switch
            {
                FilterOperator.Equals => Expression.Equal(member, constant),
                FilterOperator.NotEquals => Expression.NotEqual(member, constant),
                FilterOperator.GreaterThan => Expression.GreaterThan(member, constant),
                FilterOperator.GreaterThanOrEqual => Expression.GreaterThanOrEqual(member, constant),
                FilterOperator.LessThan => Expression.LessThan(member, constant),
                FilterOperator.LessThanOrEqual => Expression.LessThanOrEqual(member, constant),
                FilterOperator.Contains => Expression.Call(member, nameof(string.Contains), null, constant),
                FilterOperator.NotContains => Expression.Not(Expression.Call(member, nameof(string.Contains), null, constant)),
                FilterOperator.StartsWith => Expression.Call(member, nameof(string.StartsWith), null, constant),
                FilterOperator.EndsWith => Expression.Call(member, nameof(string.EndsWith), null, constant),
                FilterOperator.IsNullOrEmpty => Expression.Call(
                    typeof(string), nameof(string.IsNullOrEmpty), null, member),
                FilterOperator.IsNotNullOrEmpty => Expression.Not(
                    Expression.Call(typeof(string), nameof(string.IsNullOrEmpty), null, member)),
                _ => throw new NotSupportedException($"The operator '{Operator}' is not supported.")
            }, parameter);
        }
    }
}
