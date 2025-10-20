using System.Linq.Expressions;
using System.Text;

namespace GeneralReservationSystem.Infrastructure.Repositories.Util.Sql.Query
{
    internal class QueryFormatter : DbExpressionVisitor
    {
        private readonly StringBuilder sb;
        private int depth;

        private QueryFormatter()
        {
            sb = new StringBuilder();
        }

        internal static string Format(Expression expression)
        {
            QueryFormatter formatter = new();
            _ = formatter.Visit(expression);
            return formatter.sb.ToString();
        }

        protected enum Indentation
        {
            Same,
            Inner,
            Outer
        }

        internal int IndentationWidth { get; set; } = 2;

        private void AppendNewLine(Indentation style)
        {
            _ = sb.AppendLine();
            Indent(style);
            for (int i = 0, n = depth * IndentationWidth; i < n; i++)
            {
                _ = sb.Append(' ');
            }
        }

        private void Indent(Indentation style)
        {
            if (style == Indentation.Inner)
            {
                depth++;
            }
            else if (style == Indentation.Outer)
            {
                depth--;
                System.Diagnostics.Debug.Assert(depth >= 0);
            }
        }

        public override Expression? Visit(Expression? exp)
        {
            return exp == null
                ? null
                : exp.NodeType switch
                {
                    ExpressionType.Negate or ExpressionType.NegateChecked or ExpressionType.Not or ExpressionType.Convert or ExpressionType.ConvertChecked or ExpressionType.UnaryPlus or ExpressionType.Add or ExpressionType.AddChecked or ExpressionType.Subtract or ExpressionType.SubtractChecked or ExpressionType.Multiply or ExpressionType.MultiplyChecked or ExpressionType.Divide or ExpressionType.Modulo or ExpressionType.And or ExpressionType.AndAlso or ExpressionType.Or or ExpressionType.OrElse or ExpressionType.LessThan or ExpressionType.LessThanOrEqual or ExpressionType.GreaterThan or ExpressionType.GreaterThanOrEqual or ExpressionType.Equal or ExpressionType.NotEqual or ExpressionType.Coalesce or ExpressionType.RightShift or ExpressionType.LeftShift or ExpressionType.ExclusiveOr or ExpressionType.Power or ExpressionType.Conditional or ExpressionType.Constant or ExpressionType.MemberAccess or ExpressionType.Call or ExpressionType.New or (ExpressionType)DbExpressionType.Table or (ExpressionType)DbExpressionType.Column or (ExpressionType)DbExpressionType.Select or (ExpressionType)DbExpressionType.Join or (ExpressionType)DbExpressionType.Aggregate or (ExpressionType)DbExpressionType.Scalar or (ExpressionType)DbExpressionType.Exists or (ExpressionType)DbExpressionType.In or (ExpressionType)DbExpressionType.AggregateSubquery or (ExpressionType)DbExpressionType.IsNull or (ExpressionType)DbExpressionType.Between or (ExpressionType)DbExpressionType.RowCount or (ExpressionType)DbExpressionType.Projection or (ExpressionType)DbExpressionType.NamedValue => base.Visit(exp),
                    _ => throw new Exception(string.Format("The LINQ expression node of type {0} is not supported", exp.NodeType)),
                };
        }

        protected override Expression VisitMember(MemberExpression m)
        {
            if (m.Member.DeclaringType == typeof(string))
            {
                switch (m.Member.Name)
                {
                    case "Length":
                        _ = sb.Append("LEN(");
                        _ = Visit(m.Expression);
                        _ = sb.Append(')');
                        return m;
                }
            }
            else if (m.Member.DeclaringType == typeof(DateTime) || m.Member.DeclaringType == typeof(DateTimeOffset))
            {
                switch (m.Member.Name)
                {
                    case "Day":
                        _ = sb.Append("DAY(");
                        _ = Visit(m.Expression);
                        _ = sb.Append(')');
                        return m;
                    case "Month":
                        _ = sb.Append("MONTH(");
                        _ = Visit(m.Expression);
                        _ = sb.Append(')');
                        return m;
                    case "Year":
                        _ = sb.Append("YEAR(");
                        _ = Visit(m.Expression);
                        _ = sb.Append(')');
                        return m;
                    case "Hour":
                        _ = sb.Append("DATEPART(hour, ");
                        _ = Visit(m.Expression);
                        _ = sb.Append(')');
                        return m;
                    case "Minute":
                        _ = sb.Append("DATEPART(minute, ");
                        _ = Visit(m.Expression);
                        _ = sb.Append(')');
                        return m;
                    case "Second":
                        _ = sb.Append("DATEPART(second, ");
                        _ = Visit(m.Expression);
                        _ = sb.Append(')');
                        return m;
                    case "Millisecond":
                        _ = sb.Append("DATEPART(millisecond, ");
                        _ = Visit(m.Expression);
                        _ = sb.Append(')');
                        return m;
                    case "DayOfWeek":
                        _ = sb.Append("(DATEPART(weekday, ");
                        _ = Visit(m.Expression);
                        _ = sb.Append(") - 1)");
                        return m;
                    case "DayOfYear":
                        _ = sb.Append("(DATEPART(dayofyear, ");
                        _ = Visit(m.Expression);
                        _ = sb.Append(") - 1)");
                        return m;
                }
            }
            throw new NotSupportedException(string.Format("The member '{0}' is not supported", m.Member.Name));
        }

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            if (m.Method.DeclaringType == typeof(string))
            {
                switch (m.Method.Name)
                {
                    case "StartsWith":
                        _ = sb.Append('(');
                        _ = Visit(m.Object);
                        _ = sb.Append(" LIKE ");
                        _ = Visit(m.Arguments[0]);
                        _ = sb.Append(" + '%')");
                        return m;
                    case "EndsWith":
                        _ = sb.Append('(');
                        _ = Visit(m.Object);
                        _ = sb.Append(" LIKE '%' + ");
                        _ = Visit(m.Arguments[0]);
                        _ = sb.Append(')');
                        return m;
                    case "Contains":
                        _ = sb.Append('(');
                        _ = Visit(m.Object);
                        _ = sb.Append(" LIKE '%' + ");
                        _ = Visit(m.Arguments[0]);
                        _ = sb.Append(" + '%')");
                        return m;
                    case "Concat":
                        IList<Expression> args = m.Arguments;
                        if (args.Count == 1 && args[0].NodeType == ExpressionType.NewArrayInit)
                        {
                            args = ((NewArrayExpression)args[0]).Expressions;
                        }
                        for (int i = 0, n = args.Count; i < n; i++)
                        {
                            if (i > 0)
                            {
                                _ = sb.Append(" + ");
                            }

                            _ = Visit(args[i]);
                        }
                        return m;
                    case "IsNullOrEmpty":
                        _ = sb.Append('(');
                        _ = Visit(m.Arguments[0]);
                        _ = sb.Append(" IS NULL OR ");
                        _ = Visit(m.Arguments[0]);
                        _ = sb.Append(" = '')");
                        return m;
                    case "ToUpper":
                        _ = sb.Append("UPPER(");
                        _ = Visit(m.Object);
                        _ = sb.Append(')');
                        return m;
                    case "ToLower":
                        _ = sb.Append("LOWER(");
                        _ = Visit(m.Object);
                        _ = sb.Append(')');
                        return m;
                    case "Replace":
                        _ = sb.Append("REPLACE(");
                        _ = Visit(m.Object);
                        _ = sb.Append(", ");
                        _ = Visit(m.Arguments[0]);
                        _ = sb.Append(", ");
                        _ = Visit(m.Arguments[1]);
                        _ = sb.Append(')');
                        return m;
                    case "Substring":
                        _ = sb.Append("SUBSTRING(");
                        _ = Visit(m.Object);
                        _ = sb.Append(", ");
                        _ = Visit(m.Arguments[0]);
                        _ = sb.Append(" + 1, ");
                        if (m.Arguments.Count == 2)
                        {
                            _ = Visit(m.Arguments[1]);
                        }
                        else
                        {
                            _ = sb.Append("8000");
                        }
                        _ = sb.Append(')');
                        return m;
                    case "Remove":
                        _ = sb.Append("STUFF(");
                        _ = Visit(m.Object);
                        _ = sb.Append(", ");
                        _ = Visit(m.Arguments[0]);
                        _ = sb.Append(" + 1, ");
                        if (m.Arguments.Count == 2)
                        {
                            _ = Visit(m.Arguments[1]);
                        }
                        else
                        {
                            _ = sb.Append("8000");
                        }
                        _ = sb.Append(", '')");
                        return m;
                    case "IndexOf":
                        _ = sb.Append("(CHARINDEX(");
                        _ = Visit(m.Object);
                        _ = sb.Append(", ");
                        _ = Visit(m.Arguments[0]);
                        if (m.Arguments.Count == 2 && m.Arguments[1].Type == typeof(int))
                        {
                            _ = sb.Append(", ");
                            _ = Visit(m.Arguments[1]);
                        }
                        _ = sb.Append(") - 1)");
                        return m;
                    case "Trim":
                        _ = sb.Append("RTRIM(LTRIM(");
                        _ = Visit(m.Object);
                        _ = sb.Append("))");
                        return m;
                }
            }
            else if (m.Method.DeclaringType == typeof(DateTime))
            {
                switch (m.Method.Name)
                {
                    case "op_Subtract":
                        if (m.Arguments[1].Type == typeof(DateTime))
                        {
                            _ = sb.Append("DATEDIFF(");
                            _ = Visit(m.Arguments[0]);
                            _ = sb.Append(", ");
                            _ = Visit(m.Arguments[1]);
                            _ = sb.Append(')');
                            return m;
                        }
                        break;
                }
            }
            else if (m.Method.DeclaringType == typeof(decimal))
            {
                switch (m.Method.Name)
                {
                    case "Add":
                    case "Subtract":
                    case "Multiply":
                    case "Divide":
                    case "Remainder":
                        _ = sb.Append('(');
                        _ = VisitValue(m.Arguments[0]);
                        _ = sb.Append(' ');
                        _ = sb.Append(GetOperator(m.Method.Name));
                        _ = sb.Append(' ');
                        _ = VisitValue(m.Arguments[1]);
                        _ = sb.Append(')');
                        return m;
                    case "Negate":
                        _ = sb.Append('-');
                        _ = Visit(m.Arguments[0]);
                        _ = sb.Append("");
                        return m;
                    case "Ceiling":
                    case "Floor":
                        _ = sb.Append([.. m.Method.Name]);
                        _ = sb.Append('(');
                        _ = Visit(m.Arguments[0]);
                        _ = sb.Append(')');
                        return m;
                    case "Round":
                        if (m.Arguments.Count == 1)
                        {
                            _ = sb.Append("ROUND(");
                            _ = Visit(m.Arguments[0]);
                            _ = sb.Append(", 0)");
                            return m;
                        }
                        else if (m.Arguments.Count == 2 && m.Arguments[1].Type == typeof(int))
                        {
                            _ = sb.Append("ROUND(");
                            _ = Visit(m.Arguments[0]);
                            _ = sb.Append(", ");
                            _ = Visit(m.Arguments[1]);
                            _ = sb.Append(')');
                            return m;
                        }
                        break;
                    case "Truncate":
                        _ = sb.Append("ROUND(");
                        _ = Visit(m.Arguments[0]);
                        _ = sb.Append(", 0, 1)");
                        return m;
                }
            }
            else if (m.Method.DeclaringType == typeof(Math))
            {
                switch (m.Method.Name)
                {
                    case "Abs":
                    case "Acos":
                    case "Asin":
                    case "Atan":
                    case "Cos":
                    case "Exp":
                    case "Log10":
                    case "Sin":
                    case "Tan":
                    case "Sqrt":
                    case "Sign":
                    case "Ceiling":
                    case "Floor":
                        _ = sb.Append([.. m.Method.Name]);
                        _ = sb.Append('(');
                        _ = Visit(m.Arguments[0]);
                        _ = sb.Append(')');
                        return m;
                    case "Atan2":
                        _ = sb.Append("ATN2(");
                        _ = Visit(m.Arguments[0]);
                        _ = sb.Append(", ");
                        _ = Visit(m.Arguments[1]);
                        _ = sb.Append(')');
                        return m;
                    case "Log":
                        if (m.Arguments.Count == 1)
                        {
                            goto case "Log10";
                        }
                        break;
                    case "Pow":
                        _ = sb.Append("POWER(");
                        _ = Visit(m.Arguments[0]);
                        _ = sb.Append(", ");
                        _ = Visit(m.Arguments[1]);
                        _ = sb.Append(')');
                        return m;
                    case "Round":
                        if (m.Arguments.Count == 1)
                        {
                            _ = sb.Append("ROUND(");
                            _ = Visit(m.Arguments[0]);
                            _ = sb.Append(", 0)");
                            return m;
                        }
                        else if (m.Arguments.Count == 2 && m.Arguments[1].Type == typeof(int))
                        {
                            _ = sb.Append("ROUND(");
                            _ = Visit(m.Arguments[0]);
                            _ = sb.Append(", ");
                            _ = Visit(m.Arguments[1]);
                            _ = sb.Append(')');
                            return m;
                        }
                        break;
                    case "Truncate":
                        _ = sb.Append("ROUND(");
                        _ = Visit(m.Arguments[0]);
                        _ = sb.Append(", 0, 1)");
                        return m;
                }
            }
            if (m.Method.Name == "ToString")
            {
                if (m.Object!.Type == typeof(string))
                {
                    _ = Visit(m.Object);  // no op
                }
                else
                {
                    _ = sb.Append("CONVERT(VARCHAR, ");
                    _ = Visit(m.Object);
                    _ = sb.Append(')');
                }
                return m;
            }
            else if (m.Method.Name == "Equals")
            {
                if (m.Method.IsStatic && m.Method.DeclaringType == typeof(object))
                {
                    _ = sb.Append('(');
                    _ = Visit(m.Arguments[0]);
                    _ = sb.Append(" = ");
                    _ = Visit(m.Arguments[1]);
                    _ = sb.Append(')');
                    return m;
                }
                else if (!m.Method.IsStatic && m.Arguments.Count == 1 && m.Arguments[0].Type == m.Object!.Type)
                {
                    _ = sb.Append('(');
                    _ = Visit(m.Object);
                    _ = sb.Append(" = ");
                    _ = Visit(m.Arguments[0]);
                    _ = sb.Append(')');
                    return m;
                }
            }

            throw new NotSupportedException(string.Format("The method '{0}' is not supported", m.Method.Name));
        }

        protected override NewExpression VisitNew(NewExpression nex)
        {
            if (nex.Constructor!.DeclaringType == typeof(DateTime))
            {
                if (nex.Arguments.Count == 3)
                {
                    _ = sb.Append("DATEADD(year, ");
                    _ = Visit(nex.Arguments[0]);
                    _ = sb.Append(", DATEADD(month, ");
                    _ = Visit(nex.Arguments[1]);
                    _ = sb.Append(", DATEADD(day, ");
                    _ = Visit(nex.Arguments[2]);
                    _ = sb.Append(", 0)))");
                    return nex;
                }
                else if (nex.Arguments.Count == 6)
                {
                    _ = sb.Append("DATEADD(year, ");
                    _ = Visit(nex.Arguments[0]);
                    _ = sb.Append(", DATEADD(month, ");
                    _ = Visit(nex.Arguments[1]);
                    _ = sb.Append(", DATEADD(day, ");
                    _ = Visit(nex.Arguments[2]);
                    _ = sb.Append(", DATEADD(hour, ");
                    _ = Visit(nex.Arguments[3]);
                    _ = sb.Append(", DATEADD(minute, ");
                    _ = Visit(nex.Arguments[4]);
                    _ = sb.Append(", DATEADD(second, ");
                    _ = Visit(nex.Arguments[5]);
                    _ = sb.Append(", 0))))))");
                    return nex;
                }
            }
            throw new NotSupportedException(string.Format("The construtor '{0}' is not supported", nex.Constructor));
        }

        protected override Expression VisitUnary(UnaryExpression u)
        {
            string op = GetOperator(u);
            switch (u.NodeType)
            {
                case ExpressionType.Not:
                    if (IsBoolean(u.Operand.Type))
                    {
                        _ = sb.Append(op);
                        _ = sb.Append(' ');
                        _ = VisitPredicate(u.Operand);
                    }
                    else
                    {
                        _ = sb.Append(op);
                        _ = VisitValue(u.Operand);
                    }
                    break;
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                    _ = sb.Append(op);
                    _ = VisitValue(u.Operand);
                    break;
                case ExpressionType.UnaryPlus:
                    _ = VisitValue(u.Operand);
                    break;
                case ExpressionType.Convert:
                    // ignore conversions for now
                    _ = Visit(u.Operand);
                    break;
                default:
                    throw new NotSupportedException(string.Format("The unary operator '{0}' is not supported", u.NodeType));
            }
            return u;
        }

        protected override Expression VisitBinary(BinaryExpression b)
        {
            string op = GetOperator(b);
            Expression left = b.Left;
            Expression right = b.Right;

            if (b.NodeType == ExpressionType.Power)
            {
                _ = sb.Append("POWER(");
                _ = VisitValue(left);
                _ = sb.Append(", ");
                _ = VisitValue(right);
                _ = sb.Append(')');
                return b;
            }
            else if (b.NodeType == ExpressionType.Coalesce)
            {
                _ = sb.Append("COALESCE(");
                _ = VisitValue(left);
                _ = sb.Append(", ");
                while (right.NodeType == ExpressionType.Coalesce)
                {
                    BinaryExpression rb = (BinaryExpression)right;
                    _ = VisitValue(rb.Left);
                    _ = sb.Append(", ");
                    right = rb.Right;
                }
                _ = VisitValue(right);
                _ = sb.Append(')');
                return b;
            }
            else
            {
                _ = sb.Append('(');
                switch (b.NodeType)
                {
                    case ExpressionType.And:
                    case ExpressionType.AndAlso:
                    case ExpressionType.Or:
                    case ExpressionType.OrElse:
                        if (IsBoolean(left.Type))
                        {
                            _ = VisitPredicate(left);
                            _ = sb.Append(' ');
                            _ = sb.Append(op);
                            _ = sb.Append(' ');
                            _ = VisitPredicate(right);
                        }
                        else
                        {
                            _ = VisitValue(left);
                            _ = sb.Append(' ');
                            _ = sb.Append(op);
                            _ = sb.Append(' ');
                            _ = VisitValue(right);
                        }
                        break;
                    case ExpressionType.Equal:
                    case ExpressionType.NotEqual:
                        if (right.NodeType == ExpressionType.Constant)
                        {
                            ConstantExpression ce = (ConstantExpression)right;
                            if (ce.Value == null)
                            {
                                _ = Visit(left);
                                _ = sb.Append(" IS NULL");
                                break;
                            }
                        }
                        else if (left.NodeType == ExpressionType.Constant)
                        {
                            ConstantExpression ce = (ConstantExpression)left;
                            if (ce.Value == null)
                            {
                                _ = Visit(right);
                                _ = sb.Append(" IS NULL");
                                break;
                            }
                        }
                        goto case ExpressionType.LessThan;
                    case ExpressionType.LessThan:
                    case ExpressionType.LessThanOrEqual:
                    case ExpressionType.GreaterThan:
                    case ExpressionType.GreaterThanOrEqual:
                        // check for special x.CompareTo(y) && type.Compare(x,y)
                        if (left.NodeType == ExpressionType.Call && right.NodeType == ExpressionType.Constant)
                        {
                            MethodCallExpression mc = (MethodCallExpression)left;
                            ConstantExpression ce = (ConstantExpression)right;
                            if (ce.Value != null && ce.Value.GetType() == typeof(int) && ((int)ce.Value) == 0)
                            {
                                if (mc.Method.Name == "CompareTo" && !mc.Method.IsStatic && mc.Arguments.Count == 1)
                                {
                                    left = mc.Object!;
                                    right = mc.Arguments[0];
                                }
                                else if (
                                    (mc.Method.DeclaringType == typeof(string) || mc.Method.DeclaringType == typeof(decimal))
                                      && mc.Method.Name == "Compare" && mc.Method.IsStatic && mc.Arguments.Count == 2)
                                {
                                    left = mc.Arguments[0];
                                    right = mc.Arguments[1];
                                }
                            }
                        }
                        goto case ExpressionType.Add;
                    case ExpressionType.Add:
                    case ExpressionType.AddChecked:
                    case ExpressionType.Subtract:
                    case ExpressionType.SubtractChecked:
                    case ExpressionType.Multiply:
                    case ExpressionType.MultiplyChecked:
                    case ExpressionType.Divide:
                    case ExpressionType.Modulo:
                    case ExpressionType.ExclusiveOr:
                        _ = VisitValue(left);
                        _ = sb.Append(' ');
                        _ = sb.Append(op);
                        _ = sb.Append(' ');
                        _ = VisitValue(right);
                        break;
                    case ExpressionType.RightShift:
                        _ = VisitValue(left);
                        _ = sb.Append(" / POWER(2, ");
                        _ = VisitValue(right);
                        _ = sb.Append(')');
                        break;
                    case ExpressionType.LeftShift:
                        _ = VisitValue(left);
                        _ = sb.Append(" * POWER(2, ");
                        _ = VisitValue(right);
                        _ = sb.Append(')');
                        break;
                    default:
                        throw new NotSupportedException(string.Format("The binary operator '{0}' is not supported", b.NodeType));
                }
                _ = sb.Append(')');
            }
            return b;
        }

        private static string? GetOperator(string methodName)
        {
            return methodName switch
            {
                "Add" => "+",
                "Subtract" => "-",
                "Multiply" => "*",
                "Divide" => "/",
                "Negate" => "-",
                "Remainder" => "%",
                _ => null,
            };
        }

        private static string GetOperator(UnaryExpression u)
        {
            return u.NodeType switch
            {
                ExpressionType.Negate or ExpressionType.NegateChecked => "-",
                ExpressionType.UnaryPlus => "+",
                ExpressionType.Not => IsBoolean(u.Operand.Type) ? "NOT" : "~",
                _ => "",
            };
        }

        private static string GetOperator(BinaryExpression b)
        {
            return b.NodeType switch
            {
                ExpressionType.And or ExpressionType.AndAlso => IsBoolean(b.Left.Type) ? "AND" : "&",
                ExpressionType.Or or ExpressionType.OrElse => IsBoolean(b.Left.Type) ? "OR" : "|",
                ExpressionType.Equal => "=",
                ExpressionType.NotEqual => "<>",
                ExpressionType.LessThan => "<",
                ExpressionType.LessThanOrEqual => "<=",
                ExpressionType.GreaterThan => ">",
                ExpressionType.GreaterThanOrEqual => ">=",
                ExpressionType.Add or ExpressionType.AddChecked => "+",
                ExpressionType.Subtract or ExpressionType.SubtractChecked => "-",
                ExpressionType.Multiply or ExpressionType.MultiplyChecked => "*",
                ExpressionType.Divide => "/",
                ExpressionType.Modulo => "%",
                ExpressionType.ExclusiveOr => "^",
                _ => "",
            };
        }

        private static bool IsBoolean(Type type)
        {
            return type == typeof(bool) || type == typeof(bool?);
        }

        private static bool IsPredicate(Expression expr)
        {
            return expr.NodeType switch
            {
                ExpressionType.And or ExpressionType.AndAlso or ExpressionType.Or or ExpressionType.OrElse => IsBoolean(((BinaryExpression)expr).Type),
                ExpressionType.Not => IsBoolean(((UnaryExpression)expr).Type),
                ExpressionType.Equal or ExpressionType.NotEqual or ExpressionType.LessThan or ExpressionType.LessThanOrEqual or ExpressionType.GreaterThan or ExpressionType.GreaterThanOrEqual or (ExpressionType)DbExpressionType.IsNull or (ExpressionType)DbExpressionType.Between or (ExpressionType)DbExpressionType.Exists or (ExpressionType)DbExpressionType.In => true,
                ExpressionType.Call => IsBoolean(((MethodCallExpression)expr).Type),
                _ => false,
            };
        }

        protected virtual Expression VisitPredicate(Expression expr)
        {
            _ = Visit(expr);
            if (!IsPredicate(expr))
            {
                _ = sb.Append(" = 1");
            }
            return expr;
        }

        protected virtual Expression VisitValue(Expression expr)
        {
            if (IsPredicate(expr))
            {
                _ = sb.Append("CASE WHEN (");
                _ = Visit(expr);
                _ = sb.Append(") THEN 1 ELSE 0 END");
            }
            else
            {
                _ = Visit(expr);
            }
            return expr;
        }

        protected override Expression VisitConditional(ConditionalExpression c)
        {
            if (IsPredicate(c.Test))
            {
                _ = sb.Append("(CASE WHEN ");
                _ = VisitPredicate(c.Test);
                _ = sb.Append(" THEN ");
                _ = VisitValue(c.IfTrue);
                Expression ifFalse = c.IfFalse;
                while (ifFalse != null && ifFalse.NodeType == ExpressionType.Conditional)
                {
                    ConditionalExpression fc = (ConditionalExpression)ifFalse;
                    _ = sb.Append(" WHEN ");
                    _ = VisitPredicate(fc.Test);
                    _ = sb.Append(" THEN ");
                    _ = VisitValue(fc.IfTrue);
                    ifFalse = fc.IfFalse;
                }
                if (ifFalse != null)
                {
                    _ = sb.Append(" ELSE ");
                    _ = VisitValue(ifFalse);
                }
                _ = sb.Append(" END)");
            }
            else
            {
                _ = sb.Append("(CASE ");
                _ = VisitValue(c.Test);
                _ = sb.Append(" WHEN 1 THEN ");
                _ = VisitValue(c.IfTrue);
                _ = sb.Append(" ELSE ");
                _ = VisitValue(c.IfFalse);
                _ = sb.Append(" END)");
            }
            return c;
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            WriteValue(c.Value!);
            return c;
        }

        protected virtual void WriteValue(object value)
        {
            if (value == null)
            {
                _ = sb.Append("NULL");
            }
            else
            {
                switch (Type.GetTypeCode(value.GetType()))
                {
                    case TypeCode.Boolean:
                        _ = sb.Append(((bool)value) ? 1 : 0);
                        break;
                    case TypeCode.String:
                        _ = sb.Append('\'');
                        _ = sb.Append(value);
                        _ = sb.Append('\'');
                        break;
                    case TypeCode.Object:
                        _ = value.GetType().IsEnum
                            ? sb.Append(Convert.ChangeType(value, typeof(int)))
                            : throw new NotSupportedException(string.Format("The constant for '{0}' is not supported", value));
                        break;
                    default:
                        _ = sb.Append(value);
                        break;
                }
            }
        }

        protected override Expression VisitColumn(ColumnExpression column)
        {
            if (!string.IsNullOrEmpty(column.Alias))
            {
                _ = sb.Append(column.Alias);
                _ = sb.Append('.');
            }
            _ = sb.Append(column.Name);
            return column;
        }

        protected override Expression VisitProjection(ProjectionExpression proj)
        {
            _ = Visit(proj.Source);
            return proj;
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            _ = sb.Append("SELECT ");
            if (select.IsDistinct)
            {
                _ = sb.Append("DISTINCT ");
            }
            if (select.Take != null)
            {
                _ = sb.Append("TOP (");
                _ = Visit(select.Take);
                _ = sb.Append(") ");
            }
            if (select.Columns.Count > 0)
            {
                for (int i = 0, n = select.Columns.Count; i < n; i++)
                {
                    ColumnDeclaration column = select.Columns[i];
                    if (i > 0)
                    {
                        _ = sb.Append(", ");
                    }
                    ColumnExpression? c = VisitValue(column.Expression) as ColumnExpression;
                    if (!string.IsNullOrEmpty(column.Name) && (c == null || c.Name != column.Name))
                    {
                        _ = sb.Append(" AS ");
                        _ = sb.Append(column.Name);
                    }
                }
            }
            else
            {
                _ = sb.Append("NULL ");
                if (isNested)
                {
                    _ = sb.Append("AS tmp ");
                }
            }
            if (select.From != null)
            {
                AppendNewLine(Indentation.Same);
                _ = sb.Append("FROM ");
                _ = VisitSource(select.From);
            }
            if (select.Where != null)
            {
                AppendNewLine(Indentation.Same);
                _ = sb.Append("WHERE ");
                _ = VisitPredicate(select.Where);
            }
            if (select.GroupBy != null && select.GroupBy.Count > 0)
            {
                AppendNewLine(Indentation.Same);
                _ = sb.Append("GROUP BY ");
                for (int i = 0, n = select.GroupBy.Count; i < n; i++)
                {
                    if (i > 0)
                    {
                        _ = sb.Append(", ");
                    }
                    _ = VisitValue(select.GroupBy[i]);
                }
            }
            if (select.OrderBy != null && select.OrderBy.Count > 0)
            {
                AppendNewLine(Indentation.Same);
                _ = sb.Append("ORDER BY ");
                for (int i = 0, n = select.OrderBy.Count; i < n; i++)
                {
                    OrderExpression exp = select.OrderBy[i];
                    if (i > 0)
                    {
                        _ = sb.Append(", ");
                    }
                    _ = VisitValue(exp.Expression);
                    if (exp.OrderType != OrderType.Ascending)
                    {
                        _ = sb.Append(" DESC");
                    }
                }
            }
            return select;
        }

        private bool isNested = false;

        protected override Expression VisitSource(Expression? source)
        {
            bool saveIsNested = isNested;
            isNested = true;
            switch ((DbExpressionType)source!.NodeType)
            {
                case DbExpressionType.Table:
                    TableExpression table = (TableExpression)source;
                    _ = sb.Append(table.Name);
                    _ = sb.Append(" AS ");
                    _ = sb.Append(table.Alias);
                    break;
                case DbExpressionType.Select:
                    SelectExpression select = (SelectExpression)source;
                    _ = sb.Append('(');
                    AppendNewLine(Indentation.Inner);
                    _ = Visit(select);
                    AppendNewLine(Indentation.Same);
                    _ = sb.Append(')');
                    _ = sb.Append(" AS ");
                    _ = sb.Append(select.Alias);
                    Indent(Indentation.Outer);
                    break;
                case DbExpressionType.Join:
                    _ = VisitJoin((JoinExpression)source);
                    break;
                default:
                    throw new InvalidOperationException("Select source is not valid type");
            }
            isNested = saveIsNested;
            return source;
        }

        protected override Expression VisitJoin(JoinExpression join)
        {
            _ = VisitSource(join.Left);
            AppendNewLine(Indentation.Same);
            switch (join.Join)
            {
                case JoinType.CrossJoin:
                    _ = sb.Append("CROSS JOIN ");
                    break;
                case JoinType.InnerJoin:
                    _ = sb.Append("INNER JOIN ");
                    break;
                case JoinType.CrossApply:
                    _ = sb.Append("CROSS APPLY ");
                    break;
                case JoinType.LeftOuter:
                    _ = sb.Append("LEFT OUTER ");
                    break;
            }
            _ = VisitSource(join.Right);
            if (join.Condition != null)
            {
                AppendNewLine(Indentation.Inner);
                _ = sb.Append("ON ");
                _ = VisitPredicate(join.Condition);
                Indent(Indentation.Outer);
            }
            return join;
        }

        private static string GetAggregateName(AggregateType aggregateType)
        {
            return aggregateType switch
            {
                AggregateType.Count => "COUNT",
                AggregateType.Min => "MIN",
                AggregateType.Max => "MAX",
                AggregateType.Sum => "SUM",
                AggregateType.Average => "AVG",
                _ => throw new Exception(string.Format("Unknown aggregate type: {0}", aggregateType)),
            };
        }

        private static bool RequiresAsteriskWhenNoArgument(AggregateType aggregateType)
        {
            return aggregateType == AggregateType.Count;
        }

        protected override Expression VisitAggregate(AggregateExpression aggregate)
        {
            _ = sb.Append(GetAggregateName(aggregate.AggregateType));
            _ = sb.Append('(');
            if (aggregate.IsDistinct)
            {
                _ = sb.Append("DISTINCT ");
            }
            if (aggregate.Argument != null)
            {
                _ = VisitValue(aggregate.Argument);
            }
            else if (RequiresAsteriskWhenNoArgument(aggregate.AggregateType))
            {
                _ = sb.Append('*');
            }
            _ = sb.Append(')');
            return aggregate;
        }

        protected override Expression VisitIsNull(IsNullExpression isnull)
        {
            _ = VisitValue(isnull.Expression);
            _ = sb.Append(" IS NULL");
            return isnull;
        }

        protected override Expression VisitBetween(BetweenExpression between)
        {
            _ = VisitValue(between.Expression);
            _ = sb.Append(" BETWEEN ");
            _ = VisitValue(between.Lower);
            _ = sb.Append(" AND ");
            _ = VisitValue(between.Upper);
            return between;
        }

        protected override Expression VisitRowNumber(RowNumberExpression rowNumber)
        {
            _ = sb.Append("ROW_NUMBER() OVER(");
            if (rowNumber.OrderBy != null && rowNumber.OrderBy.Count > 0)
            {
                _ = sb.Append("ORDER BY ");
                for (int i = 0, n = rowNumber.OrderBy.Count; i < n; i++)
                {
                    OrderExpression exp = rowNumber.OrderBy[i];
                    if (i > 0)
                    {
                        _ = sb.Append(", ");
                    }
                    _ = VisitValue(exp.Expression);
                    if (exp.OrderType != OrderType.Ascending)
                    {
                        _ = sb.Append(" DESC");
                    }
                }
            }
            _ = sb.Append(')');
            return rowNumber;
        }

        protected override Expression VisitScalar(ScalarExpression subquery)
        {
            _ = sb.Append('(');
            AppendNewLine(Indentation.Inner);
            _ = Visit(subquery.Select);
            AppendNewLine(Indentation.Same);
            _ = sb.Append(')');
            Indent(Indentation.Outer);
            return subquery;
        }

        protected override Expression VisitExists(ExistsExpression exists)
        {
            _ = sb.Append("EXISTS(");
            AppendNewLine(Indentation.Inner);
            _ = Visit(exists.Select);
            AppendNewLine(Indentation.Same);
            _ = sb.Append(')');
            Indent(Indentation.Outer);
            return exists;
        }

        protected override Expression VisitIn(InExpression @in)
        {
            _ = VisitValue(@in.Expression);
            _ = sb.Append(" IN (");
            if (@in.Select != null)
            {
                AppendNewLine(Indentation.Inner);
                _ = Visit(@in.Select);
                AppendNewLine(Indentation.Same);
                _ = sb.Append(')');
                Indent(Indentation.Outer);
            }
            else if (@in.Values != null)
            {
                for (int i = 0, n = @in.Values.Count; i < n; i++)
                {
                    if (i > 0)
                    {
                        _ = sb.Append(", ");
                    }

                    _ = VisitValue(@in.Values[i]);
                }
                _ = sb.Append(')');
            }
            return @in;
        }

        protected override Expression VisitNamedValue(NamedValueExpression value)
        {
            _ = sb.Append("@" + value.Name);
            return value;
        }
    }
}
