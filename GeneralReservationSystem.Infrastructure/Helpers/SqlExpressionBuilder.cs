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
            ExpressionToSqlVisitor visitor = new(aliasResolver, parameters, ref paramCounter);
            string sql = visitor.Translate(expression);
            paramCounter = visitor.ParamCounter;
            return sql;
        }

        public static MemberExpression? ExtractMember(Expression expr)
        {
            return expr is UnaryExpression ue && ue.NodeType == ExpressionType.Convert
                ? ExtractMember(ue.Operand)
                : expr is MemberExpression me ? me : null;
        }

        public static Expression<Func<T, bool>>? BuildFilterPredicate<T>(Filter filter)
        {
            if (filter == null || string.IsNullOrWhiteSpace(filter.Field))
            {
                return null;
            }

            ParameterExpression parameter = Expression.Parameter(typeof(T), "x");
            PropertyInfo? property = typeof(T).GetProperty(filter.Field, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (property == null)
            {
                return null;
            }

            if (!property.CanRead)
            {
                return null;
            }

            MemberExpression propertyAccess = Expression.Property(parameter, property);

            Expression? body = null;
            Type propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            switch (filter.Operator)
            {
                case FilterOperator.Equals:
                    if (propertyType == typeof(string))
                    {
                        // For string, use case-insensitive
                        string? valueStr = filter.Value?.ToString();
                        body = Expression.Equal(propertyAccess, Expression.Constant(valueStr, typeof(string)));
                    }
                    else
                    {
                        body = BuildEqualsExpression(propertyAccess, filter.Value);
                    }
                    break;
                case FilterOperator.NotEquals:
                    if (propertyType == typeof(string))
                    {
                        string? valueStr = filter.Value?.ToString();
                        body = Expression.NotEqual(propertyAccess, Expression.Constant(valueStr, typeof(string)));
                    }
                    else
                    {
                        body = BuildNotEqualsExpression(propertyAccess, filter.Value);
                    }
                    break;
                case FilterOperator.GreaterThan:
                    body = BuildComparisonExpression(propertyAccess, filter.Value, ExpressionType.GreaterThan);
                    break;
                case FilterOperator.GreaterThanOrEqual:
                    body = BuildComparisonExpression(propertyAccess, filter.Value, ExpressionType.GreaterThanOrEqual);
                    break;
                case FilterOperator.LessThan:
                    body = BuildComparisonExpression(propertyAccess, filter.Value, ExpressionType.LessThan);
                    break;
                case FilterOperator.LessThanOrEqual:
                    body = BuildComparisonExpression(propertyAccess, filter.Value, ExpressionType.LessThanOrEqual);
                    break;
                case FilterOperator.Contains:
                    body = propertyType == typeof(string)
                        ? (Expression?)BuildStringMethodExpression(propertyAccess, filter.Value, nameof(string.Contains))
                        : throw new InvalidOperationException($"Contains operator can only be used on string properties. Property '{property.Name}' is of type '{propertyType.Name}'.");
                    break;
                case FilterOperator.StartsWith:
                    body = propertyType == typeof(string)
                        ? (Expression?)BuildStringMethodExpression(propertyAccess, filter.Value, nameof(string.StartsWith))
                        : throw new InvalidOperationException($"StartsWith operator can only be used on string properties. Property '{property.Name}' is of type '{propertyType.Name}'.");
                    break;
                case FilterOperator.EndsWith:
                    body = propertyType == typeof(string)
                        ? (Expression?)BuildStringMethodExpression(propertyAccess, filter.Value, nameof(string.EndsWith))
                        : throw new InvalidOperationException($"EndsWith operator can only be used on string properties. Property '{property.Name}' is of type '{propertyType.Name}'.");
                    break;
                case FilterOperator.IsNullOrEmpty:
                    body = BuildIsNullOrEmptyExpression(propertyAccess);
                    break;
                case FilterOperator.IsNotNullOrEmpty:
                    body = Expression.Not(BuildIsNullOrEmptyExpression(propertyAccess));
                    break;
                default:
                    body = null;
                    break;
            }

            return body == null ? null : Expression.Lambda<Func<T, bool>>(body, parameter);
        }

        public static Expression<Func<T, object>>? BuildSortExpression<T>(SortOption sortOption)
        {
            if (sortOption == null || string.IsNullOrWhiteSpace(sortOption.Field))
            {
                return null;
            }

            ParameterExpression parameter = Expression.Parameter(typeof(T), "x");
            PropertyInfo? property = typeof(T).GetProperty(sortOption.Field, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (property == null)
            {
                return null;
            }

            if (!property.CanRead)
            {
                return null;
            }

            Type propertyType = property.PropertyType;
            Type underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            if (!IsSortableType(underlyingType))
            {
                return null;
            }

            try
            {
                MemberExpression propertyAccess = Expression.Property(parameter, property);
                UnaryExpression convertedAccess = Expression.Convert(propertyAccess, typeof(object));
                return Expression.Lambda<Func<T, object>>(convertedAccess, parameter);
            }
            catch
            {
                return null;
            }
        }

        private static BinaryExpression BuildEqualsExpression(MemberExpression property, object? value)
        {
            ArgumentNullException.ThrowIfNull(property);

            if (value == null)
            {
                return Expression.Equal(property, Expression.Constant(null));
            }

            try
            {
                object? convertedValue = ConvertValue(value, property.Type);
                ConstantExpression constant = Expression.Constant(convertedValue, property.Type);
                return Expression.Equal(property, constant);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Cannot create equality expression for property '{property.Member.Name}' of type '{property.Type.Name}' with value '{value}'.", ex);
            }
        }

        private static BinaryExpression BuildNotEqualsExpression(MemberExpression property, object? value)
        {
            ArgumentNullException.ThrowIfNull(property);

            if (value == null)
            {
                return Expression.NotEqual(property, Expression.Constant(null));
            }

            try
            {
                object? convertedValue = ConvertValue(value, property.Type);
                ConstantExpression constant = Expression.Constant(convertedValue, property.Type);
                return Expression.NotEqual(property, constant);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Cannot create not-equals expression for property '{property.Member.Name}' of type '{property.Type.Name}' with value '{value}'.", ex);
            }
        }

        private static BinaryExpression BuildComparisonExpression(MemberExpression property, object? value, ExpressionType comparisonType)
        {
            ArgumentNullException.ThrowIfNull(property);

            if (value == null)
            {
                throw new ArgumentException($"Comparison operation '{comparisonType}' requires a non-null value for property '{property.Member.Name}'.");
            }

            Type propertyType = property.Type;
            Type underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            if (!IsComparableType(underlyingType))
            {
                throw new InvalidOperationException($"Property '{property.Member.Name}' of type '{propertyType.Name}' does not support comparison operations.");
            }

            try
            {
                object? convertedValue = ConvertValue(value, propertyType);
                ConstantExpression constant = Expression.Constant(convertedValue, propertyType);
                return Expression.MakeBinary(comparisonType, property, constant);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Cannot create comparison expression '{comparisonType}' for property '{property.Member.Name}' of type '{propertyType.Name}' with value '{value}'.", ex);
            }
        }

        private static MethodCallExpression? BuildStringMethodExpression(MemberExpression property, object? value, string methodName)
        {
            ArgumentNullException.ThrowIfNull(property);

            if (string.IsNullOrEmpty(methodName))
            {
                throw new ArgumentException("Method name cannot be null or empty.", nameof(methodName));
            }

            Type propertyType = Nullable.GetUnderlyingType(property.Type) ?? property.Type;

            if (propertyType != typeof(string))
            {
                throw new InvalidOperationException($"String method '{methodName}' can only be used on string properties. Property '{property.Member.Name}' is of type '{property.Type.Name}'.");
            }

            if (value == null)
            {
                return null;
            }

            string? stringValue = value.ToString();
            if (string.IsNullOrEmpty(stringValue))
            {
                return null;
            }

            MethodInfo method = typeof(string).GetMethod(methodName, [typeof(string)]) ?? throw new InvalidOperationException($"Method '{methodName}' not found on string type.");
            ConstantExpression constant = Expression.Constant(stringValue, typeof(string));

            Expression nonNullableProperty = property.Type != typeof(string) && propertyType == typeof(string)
                ? Expression.Convert(property, typeof(string))
                : property;

            return Expression.Call(nonNullableProperty, method, constant);
        }

        private static Expression BuildIsNullOrEmptyExpression(MemberExpression property)
        {
            ArgumentNullException.ThrowIfNull(property);

            Type propertyType = Nullable.GetUnderlyingType(property.Type) ?? property.Type;

            if (propertyType == typeof(string))
            {
                MethodInfo method = typeof(string).GetMethod(nameof(string.IsNullOrEmpty), [typeof(string)]) ?? throw new InvalidOperationException("IsNullOrEmpty method not found on string type.");
                return Expression.Call(method, property);
            }
            else
            {
                return !propertyType.IsValueType || Nullable.GetUnderlyingType(property.Type) != null
                    ? Expression.Equal(property, Expression.Constant(null, property.Type))
                    : Expression.Constant(false);
            }
        }

        private static object? ConvertValue(object? value, Type targetType)
        {
            if (value == null)
            {
                return null;
            }

            Type underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            try
            {
                if (value.GetType() == underlyingType)
                {
                    return value;
                }

                if (underlyingType.IsEnum)
                {
                    return value is string stringValue ? Enum.Parse(underlyingType, stringValue, ignoreCase: true) : Enum.ToObject(underlyingType, value);
                }

                if (underlyingType == typeof(Guid))
                {
                    return value is string guidString ? Guid.Parse(guidString) : value;
                }

                if (underlyingType == typeof(DateTime))
                {
                    return value is string dateString ? DateTime.Parse(dateString) : Convert.ToDateTime(value);
                }

                if (underlyingType == typeof(DateOnly))
                {
                    if (value is string dateOnlyString)
                    {
                        return DateOnly.Parse(dateOnlyString);
                    }

                    if (value is DateTime dt)
                    {
                        return DateOnly.FromDateTime(dt);
                    }
                }

                if (underlyingType == typeof(TimeOnly))
                {
                    if (value is string timeOnlyString)
                    {
                        return TimeOnly.Parse(timeOnlyString);
                    }

                    if (value is TimeSpan ts)
                    {
                        return TimeOnly.FromTimeSpan(ts);
                    }
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
            return type != null && (typeof(IComparable).IsAssignableFrom(type) || type == typeof(byte) || type == typeof(sbyte) ||
                   type == typeof(short) || type == typeof(ushort) ||
                   type == typeof(int) || type == typeof(uint) ||
                   type == typeof(long) || type == typeof(ulong) ||
                   type == typeof(float) || type == typeof(double) ||
                   type == typeof(decimal) ||
                   type == typeof(DateTime) || type == typeof(DateTimeOffset) ||
                   type == typeof(DateOnly) || type == typeof(TimeOnly) ||
                   type == typeof(TimeSpan));
        }

        private static bool IsSortableType(Type type)
        {
            return type != null && (typeof(IComparable).IsAssignableFrom(type) || type == typeof(byte) || type == typeof(sbyte) ||
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
                   type == typeof(Guid));
        }

        private sealed class ExpressionToSqlVisitor : ExpressionVisitor
        {
            private readonly Func<ParameterExpression, string> _aliasResolver;
            private readonly List<KeyValuePair<string, object?>> _parameters;
            private readonly System.Text.StringBuilder _sb = new();

            public int ParamCounter { get; private set; }

            public ExpressionToSqlVisitor(
                Func<ParameterExpression, string> aliasResolver,
                List<KeyValuePair<string, object?>> parameters,
                ref int paramCounter)
            {
                _aliasResolver = aliasResolver ?? throw new ArgumentNullException(nameof(aliasResolver));
                _parameters = parameters;
                ParamCounter = paramCounter;
            }

            private string AddParameter(object? value)
            {
                string name = $"@p{ParamCounter++}";
                _parameters.Add(new KeyValuePair<string, object?>(name, value ?? DBNull.Value));
                return name;
            }

            public string Translate(Expression expr)
            {
                _ = Visit(expr);
                return _sb.ToString();
            }

            protected override Expression VisitBinary(BinaryExpression node)
            {
                if ((node.NodeType == ExpressionType.Equal || node.NodeType == ExpressionType.NotEqual) &&
                    ((node.Left is ConstantExpression lce && lce.Value == null) || (node.Right is ConstantExpression rce && rce.Value == null)))
                {
                    _ = _sb.Append('(');
                    Expression nonNullSide = node.Left is ConstantExpression ? node.Right : node.Left;
                    _ = Visit(nonNullSide);
                    _ = _sb.Append(node.NodeType == ExpressionType.Equal ? " IS NULL" : " IS NOT NULL");
                    _ = _sb.Append(')');
                    return node;
                }

                _ = _sb.Append('(');
                _ = Visit(node.Left);

                _ = node.NodeType switch
                {
                    ExpressionType.Equal => _sb.Append(" = "),
                    ExpressionType.NotEqual => _sb.Append(" <> "),
                    ExpressionType.GreaterThan => _sb.Append(" > "),
                    ExpressionType.GreaterThanOrEqual => _sb.Append(" >= "),
                    ExpressionType.LessThan => _sb.Append(" < "),
                    ExpressionType.LessThanOrEqual => _sb.Append(" <= "),
                    ExpressionType.AndAlso => _sb.Append(" AND "),
                    ExpressionType.OrElse => _sb.Append(" OR "),
                    _ => throw new NotSupportedException($"Binary operator {node.NodeType} is not supported"),
                };
                _ = Visit(node.Right);
                _ = _sb.Append(')');
                return node;
            }

            protected override Expression VisitConstant(ConstantExpression node)
            {
                string pname = AddParameter(node.Value);
                _ = _sb.Append(pname);
                return node;
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                if (node.Expression is ParameterExpression pex)
                {
                    if (node.Member is PropertyInfo pi)
                    {
                        string col = EntityHelper.GetColumnName(pi);
                        string alias = _aliasResolver(pex);
                        _ = _sb.Append($"{SqlCommandHelper.FormatQualifiedTableName(alias)}.[{col}]");
                        return node;
                    }
                }

                object? value = Expression.Lambda(node).Compile().DynamicInvoke();
                string pname = AddParameter(value);
                _ = _sb.Append(pname);
                return node;
            }

            protected override Expression VisitUnary(UnaryExpression node)
            {
                if (node.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
                {
                    _ = Visit(node.Operand);
                    return node;
                }
                if (node.NodeType == ExpressionType.Not)
                {
                    _ = _sb.Append("(NOT ");
                    _ = Visit(node.Operand);
                    _ = _sb.Append(')');
                    return node;
                }
                return base.VisitUnary(node);
            }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                if (TryHandleStringMethod(node, out bool handled) && handled)
                {
                    return node;
                }

                if (node.Method.DeclaringType == typeof(string) && node.Method.Name == nameof(string.IsNullOrEmpty) && node.Arguments.Count == 1)
                {
                    Expression arg = node.Arguments[0];
                    Expression memberExpr = arg;
                    while (memberExpr is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } ue)
                    {
                        memberExpr = ue.Operand;
                    }
                    if (memberExpr is MemberExpression mex && mex.Expression is ParameterExpression pex && mex.Member is PropertyInfo pi)
                    {
                        string col = EntityHelper.GetColumnName(pi);
                        string alias = _aliasResolver(pex);
                        string qualifiedTableName = SqlCommandHelper.FormatQualifiedTableName(alias);
                        _ = _sb.Append("((");
                        _ = _sb.Append($"{qualifiedTableName}.[{col}] IS NULL OR {qualifiedTableName}.[{col}] = '')");
                        _ = _sb.Append(')');
                        return node;
                    }
                }

                object? val = Expression.Lambda(node).Compile().DynamicInvoke();
                string pname = AddParameter(val);
                _ = _sb.Append(pname);
                return node;
            }

            private bool TryHandleStringMethod(MethodCallExpression node, out bool handled)
            {
                handled = false;

                if (node.Object == null || node.Arguments.Count != 1)
                {
                    return false;
                }

                string method = node.Method.Name;
                if (method is not ("Contains" or "StartsWith" or "EndsWith"))
                {
                    return false;
                }

                // Unwrap Convert expressions to get to the actual MemberExpression
                Expression objectExpr = node.Object;
                while (objectExpr is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } ue)
                {
                    objectExpr = ue.Operand;
                }

                if (objectExpr is not MemberExpression mex ||
                    mex.Expression is not ParameterExpression pex ||
                    mex.Member is not PropertyInfo pi)
                {
                    return false;
                }

                if (!TryEvaluateArgument(node.Arguments[0], out object? argValue))
                {
                    return false;
                }

                string col = EntityHelper.GetColumnName(pi);
                string alias = _aliasResolver(pex);
                string qualifiedTableName = SqlCommandHelper.FormatQualifiedTableName(alias);
                string pattern = FormatLikePattern(argValue?.ToString() ?? string.Empty, method);
                string paramName = AddParameter(pattern);

                _ = _sb.Append($"{qualifiedTableName}.[{col}] LIKE {paramName}");
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
