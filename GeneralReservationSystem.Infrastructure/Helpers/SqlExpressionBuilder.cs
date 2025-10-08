using GeneralReservationSystem.Application.Helpers;
using System.Data.Common;
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

            private bool TryEvaluateArgument(Expression arg, out object? value)
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
