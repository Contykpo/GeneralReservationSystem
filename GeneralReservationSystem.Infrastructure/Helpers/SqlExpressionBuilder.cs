using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.Helpers;
using System.Linq.Expressions;
using System.Reflection;

namespace GeneralReservationSystem.Infrastructure.Helpers
{
    public static class SqlExpressionBuilder
    {
        public static string TranslateExpression(
            Expression expression,
            Func<ParameterExpression, string> aliasResolver,
            List<KeyValuePair<string, object?>> parameters,
            ref int paramCounter)
        {
            var visitor = new ExpressionToSqlVisitor(aliasResolver, parameters, ref paramCounter);
            return visitor.Translate(expression);
        }

        public static MemberExpression? ExtractMember(Expression expr)
        {
            if (expr is UnaryExpression ue && ue.NodeType == ExpressionType.Convert)
                return ExtractMember(ue.Operand);
            if (expr is MemberExpression me)
                return me;
            return null;
        }

        public static Expression<Func<T, bool>>? BuildFilterPredicate<T>(Filter filter)
        {
            if (filter == null || string.IsNullOrWhiteSpace(filter.Field))
                return null;

            var parameter = Expression.Parameter(typeof(T), "x");
            var property = typeof(T).GetProperty(filter.Field, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (property == null)
                return null;

            if (!property.CanRead)
                return null;

            var propertyAccess = Expression.Property(parameter, property);

            Expression? body = null;
            try
            {
                body = filter.Operator switch
                {
                    FilterOperator.Equals => BuildEqualsExpression(propertyAccess, filter.Value),
                    FilterOperator.NotEquals => BuildNotEqualsExpression(propertyAccess, filter.Value),
                    FilterOperator.GreaterThan => BuildComparisonExpression(propertyAccess, filter.Value, ExpressionType.GreaterThan),
                    FilterOperator.GreaterThanOrEqual => BuildComparisonExpression(propertyAccess, filter.Value, ExpressionType.GreaterThanOrEqual),
                    FilterOperator.LessThan => BuildComparisonExpression(propertyAccess, filter.Value, ExpressionType.LessThan),
                    FilterOperator.LessThanOrEqual => BuildComparisonExpression(propertyAccess, filter.Value, ExpressionType.LessThanOrEqual),
                    FilterOperator.Contains => BuildStringMethodExpression(propertyAccess, filter.Value, nameof(string.Contains)),
                    FilterOperator.StartsWith => BuildStringMethodExpression(propertyAccess, filter.Value, nameof(string.StartsWith)),
                    FilterOperator.EndsWith => BuildStringMethodExpression(propertyAccess, filter.Value, nameof(string.EndsWith)),
                    FilterOperator.IsNullOrEmpty => BuildIsNullOrEmptyExpression(propertyAccess),
                    FilterOperator.IsNotNullOrEmpty => Expression.Not(BuildIsNullOrEmptyExpression(propertyAccess)),
                    _ => null
                };
            }
            catch
            {
                return null;
            }

            if (body == null)
                return null;

            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }

        public static Expression<Func<T, object>>? BuildSortExpression<T>(SortOption sortOption)
        {
            if (sortOption == null || string.IsNullOrWhiteSpace(sortOption.Field))
                return null;

            var parameter = Expression.Parameter(typeof(T), "x");
            var property = typeof(T).GetProperty(sortOption.Field, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (property == null)
                return null;

            if (!property.CanRead)
                return null;

            var propertyType = property.PropertyType;
            var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            if (!IsSortableType(underlyingType))
                return null;

            try
            {
                var propertyAccess = Expression.Property(parameter, property);
                var convertedAccess = Expression.Convert(propertyAccess, typeof(object));
                return Expression.Lambda<Func<T, object>>(convertedAccess, parameter);
            }
            catch
            {
                return null;
            }
        }

        private static Expression BuildEqualsExpression(MemberExpression property, object? value)
        {
            ArgumentNullException.ThrowIfNull(property);

            if (value == null)
                return Expression.Equal(property, Expression.Constant(null));

            try
            {
                var convertedValue = ConvertValue(value, property.Type);
                var constant = Expression.Constant(convertedValue, property.Type);
                return Expression.Equal(property, constant);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Cannot create equality expression for property '{property.Member.Name}' of type '{property.Type.Name}' with value '{value}'.", ex);
            }
        }

        private static Expression BuildNotEqualsExpression(MemberExpression property, object? value)
        {
            ArgumentNullException.ThrowIfNull(property);

            if (value == null)
                return Expression.NotEqual(property, Expression.Constant(null));

            try
            {
                var convertedValue = ConvertValue(value, property.Type);
                var constant = Expression.Constant(convertedValue, property.Type);
                return Expression.NotEqual(property, constant);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Cannot create not-equals expression for property '{property.Member.Name}' of type '{property.Type.Name}' with value '{value}'.", ex);
            }
        }

        private static Expression BuildComparisonExpression(MemberExpression property, object? value, ExpressionType comparisonType)
        {
            ArgumentNullException.ThrowIfNull(property);

            if (value == null)
                throw new ArgumentException($"Comparison operation '{comparisonType}' requires a non-null value for property '{property.Member.Name}'.");

            var propertyType = property.Type;
            var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            if (!IsComparableType(underlyingType))
                throw new InvalidOperationException($"Property '{property.Member.Name}' of type '{propertyType.Name}' does not support comparison operations.");

            try
            {
                var convertedValue = ConvertValue(value, propertyType);
                var constant = Expression.Constant(convertedValue, propertyType);
                return Expression.MakeBinary(comparisonType, property, constant);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Cannot create comparison expression '{comparisonType}' for property '{property.Member.Name}' of type '{propertyType.Name}' with value '{value}'.", ex);
            }
        }

        private static Expression BuildStringMethodExpression(MemberExpression property, object? value, string methodName)
        {
            ArgumentNullException.ThrowIfNull(property);

            if (string.IsNullOrEmpty(methodName))
                throw new ArgumentException("Method name cannot be null or empty.", nameof(methodName));

            var propertyType = Nullable.GetUnderlyingType(property.Type) ?? property.Type;

            if (propertyType != typeof(string))
                throw new InvalidOperationException($"String method '{methodName}' can only be used on string properties. Property '{property.Member.Name}' is of type '{property.Type.Name}'.");

            if (value == null)
                return Expression.Constant(false);

            var stringValue = value.ToString();
            if (stringValue == null)
                return Expression.Constant(false);

            var method = typeof(string).GetMethod(methodName, [typeof(string)]) ?? throw new InvalidOperationException($"Method '{methodName}' not found on string type.");
            var constant = Expression.Constant(stringValue, typeof(string));
            return Expression.Call(property, method, constant);
        }

        private static Expression BuildIsNullOrEmptyExpression(MemberExpression property)
        {
            ArgumentNullException.ThrowIfNull(property);

            var propertyType = Nullable.GetUnderlyingType(property.Type) ?? property.Type;

            if (propertyType == typeof(string))
            {
                var method = typeof(string).GetMethod(nameof(string.IsNullOrEmpty), [typeof(string)]) ?? throw new InvalidOperationException("IsNullOrEmpty method not found on string type.");
                return Expression.Call(method, property);
            }
            else if (!propertyType.IsValueType || Nullable.GetUnderlyingType(property.Type) != null)
            {
                return Expression.Equal(property, Expression.Constant(null, property.Type));
            }
            else
            {
                return Expression.Constant(false);
            }
        }

        private static object? ConvertValue(object? value, Type targetType)
        {
            if (value == null)
                return null;

            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            try
            {
                if (value.GetType() == underlyingType)
                    return value;

                if (underlyingType.IsEnum)
                {
                    if (value is string stringValue)
                        return Enum.Parse(underlyingType, stringValue, ignoreCase: true);
                    return Enum.ToObject(underlyingType, value);
                }

                if (underlyingType == typeof(Guid))
                {
                    if (value is string guidString)
                        return Guid.Parse(guidString);
                    return value;
                }

                if (underlyingType == typeof(DateTime))
                {
                    if (value is string dateString)
                        return DateTime.Parse(dateString);
                    return Convert.ToDateTime(value);
                }

                if (underlyingType == typeof(DateOnly))
                {
                    if (value is string dateOnlyString)
                        return DateOnly.Parse(dateOnlyString);
                    if (value is DateTime dt)
                        return DateOnly.FromDateTime(dt);
                }

                if (underlyingType == typeof(TimeOnly))
                {
                    if (value is string timeOnlyString)
                        return TimeOnly.Parse(timeOnlyString);
                    if (value is TimeSpan ts)
                        return TimeOnly.FromTimeSpan(ts);
                }

                return Convert.ChangeType(value, underlyingType);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Cannot convert value '{value}' of type '{value.GetType().Name}' to target type '{targetType.Name}'.", ex);
            }
        }

        private static bool IsComparableType(Type type)
        {
            if (type == null)
                return false;

            if (typeof(IComparable).IsAssignableFrom(type))
                return true;

            return type == typeof(byte) || type == typeof(sbyte) ||
                   type == typeof(short) || type == typeof(ushort) ||
                   type == typeof(int) || type == typeof(uint) ||
                   type == typeof(long) || type == typeof(ulong) ||
                   type == typeof(float) || type == typeof(double) ||
                   type == typeof(decimal) ||
                   type == typeof(DateTime) || type == typeof(DateTimeOffset) ||
                   type == typeof(DateOnly) || type == typeof(TimeOnly) ||
                   type == typeof(TimeSpan);
        }

        private static bool IsSortableType(Type type)
        {
            if (type == null)
                return false;

            if (typeof(IComparable).IsAssignableFrom(type))
                return true;

            return type == typeof(byte) || type == typeof(sbyte) ||
                   type == typeof(short) || type == typeof(ushort) ||
                   type == typeof(int) || type == typeof(uint) ||
                   type == typeof(long) || type == typeof(ulong) ||
                   type == typeof(float) || type == typeof(double) ||
                   type == typeof(decimal) ||
                   type == typeof(bool) ||
                   type == typeof(char) ||
                   type == typeof(string) ||
                   type == typeof(DateTime) || type == typeof(DateTimeOffset) ||
                   type == typeof(DateOnly) || type == typeof(TimeOnly) ||
                   type == typeof(TimeSpan) ||
                   type == typeof(Guid);
        }

        private sealed class ExpressionToSqlVisitor : ExpressionVisitor
        {
            private readonly Func<ParameterExpression, string> _aliasResolver;
            private readonly List<KeyValuePair<string, object?>> _parameters;
            private readonly System.Text.StringBuilder _sb = new();
            private int _paramCounter;

            public ExpressionToSqlVisitor(
                Func<ParameterExpression, string> aliasResolver,
                List<KeyValuePair<string, object?>> parameters,
                ref int paramCounter)
            {
                _aliasResolver = aliasResolver ?? throw new ArgumentNullException(nameof(aliasResolver));
                _parameters = parameters;
                _paramCounter = paramCounter;
            }

            private string AddParameter(object? value)
            {
                var name = $"@p{_paramCounter++}";
                _parameters.Add(new KeyValuePair<string, object?>(name, value ?? DBNull.Value));
                return name;
            }

            public string Translate(Expression expr)
            {
                Visit(expr);
                return _sb.ToString();
            }

            protected override Expression VisitBinary(BinaryExpression node)
            {
                _sb.Append('(');
                Visit(node.Left);

                switch (node.NodeType)
                {
                    case ExpressionType.Equal: _sb.Append(" = "); break;
                    case ExpressionType.NotEqual: _sb.Append(" <> "); break;
                    case ExpressionType.GreaterThan: _sb.Append(" > "); break;
                    case ExpressionType.GreaterThanOrEqual: _sb.Append(" >= "); break;
                    case ExpressionType.LessThan: _sb.Append(" < "); break;
                    case ExpressionType.LessThanOrEqual: _sb.Append(" <= "); break;
                    case ExpressionType.AndAlso: _sb.Append(" AND "); break;
                    case ExpressionType.OrElse: _sb.Append(" OR "); break;
                    default: throw new NotSupportedException($"Binary operator {node.NodeType} is not supported");
                }

                Visit(node.Right);
                _sb.Append(')');
                return node;
            }

            protected override Expression VisitConstant(ConstantExpression node)
            {
                var pname = AddParameter(node.Value);
                _sb.Append(pname);
                return node;
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                if (node.Expression is ParameterExpression pex)
                {
                    if (node.Member is PropertyInfo pi)
                    {
                        var col = EntityHelper.GetColumnName(pi);
                        var alias = _aliasResolver(pex);
                        _sb.Append($"{SqlCommandHelper.FormatQualifiedTableName(alias)}.[{col}]");
                        return node;
                    }
                }

                var value = Expression.Lambda(node).Compile().DynamicInvoke();
                var pname = AddParameter(value);
                _sb.Append(pname);
                return node;
            }

            protected override Expression VisitUnary(UnaryExpression node)
            {
                if (node.NodeType == ExpressionType.Convert || node.NodeType == ExpressionType.ConvertChecked)
                {
                    Visit(node.Operand);
                    return node;
                }
                return base.VisitUnary(node);
            }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                if (TryHandleStringMethod(node, out var handled) && handled)
                {
                    return node;
                }

                var val = Expression.Lambda(node).Compile().DynamicInvoke();
                var pname = AddParameter(val);
                _sb.Append(pname);
                return node;
            }

            private bool TryHandleStringMethod(MethodCallExpression node, out bool handled)
            {
                handled = false;

                if (node.Object is not MemberExpression mex ||
                    mex.Expression is not ParameterExpression pex ||
                    mex.Member is not PropertyInfo pi ||
                    node.Arguments.Count != 1)
                {
                    return false;
                }

                var method = node.Method.Name;
                if (method is not ("Contains" or "StartsWith" or "EndsWith"))
                {
                    return false;
                }

                if (!TryEvaluateArgument(node.Arguments[0], out var argValue))
                {
                    return false;
                }

                var col = EntityHelper.GetColumnName(pi);
                var alias = _aliasResolver(pex);
                var qualifiedTableName = SqlCommandHelper.FormatQualifiedTableName(alias);
                var pattern = FormatLikePattern(argValue?.ToString() ?? string.Empty, method);
                var paramName = AddParameter(pattern);

                _sb.Append($"{qualifiedTableName}.[{col}] LIKE {paramName}");
                handled = true;
                return true;
            }

            private static bool TryEvaluateArgument(Expression arg, out object? value)
            {
                value = null;

                try
                {
                    if (arg is ConstantExpression ce)
                    {
                        value = ce.Value;
                        return true;
                    }

                    value = Expression.Lambda(arg).Compile().DynamicInvoke();
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            private static string FormatLikePattern(string value, string method)
            {
                return method switch
                {
                    "Contains" => $"%{value}%",
                    "StartsWith" => $"{value}%",
                    "EndsWith" => $"%{value}",
                    _ => throw new InvalidOperationException($"Unsupported string method: {method}")
                };
            }
        }
    }
}
