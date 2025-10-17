using System.Diagnostics;
using System.Linq.Expressions;
using System.Text;

namespace GeneralReservationSystem.Infrastructure.Repositories.Util.Sql.Query
{
    internal class QueryFormatter : DbExpressionVisitor
    {
        private StringBuilder sb = null!;
        private int depth;

        internal string Format(Expression expression)
        {
            sb = new StringBuilder();
            _ = Visit(expression);

            return sb.ToString();
        }

        protected enum Identation
        {
            Same,
            Inner,
            Outer
        }

        internal int IdentationWidth { get; set; } = 2;

        private void AppendNewLine(Identation style)
        {
            _ = sb.AppendLine();

            if (style == Identation.Inner)
            {
                depth++;
            }
            else if (style == Identation.Outer)
            {
                depth--;
                Debug.Assert(depth >= 0);
            }

            for (int i = 0, n = depth * IdentationWidth; i < n; i++)
            {
                _ = sb.Append(' ');
            }
        }

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            throw new NotSupportedException($"The method '{m.Method.Name}' is not supported");
        }

        protected override Expression VisitUnary(UnaryExpression u)
        {
            switch (u.NodeType)
            {
                case ExpressionType.Not:
                    _ = sb.Append(" NOT ");
                    _ = Visit(u.Operand);
                    break;

                default:
                    throw new NotSupportedException($"The unary operator '{u.NodeType}' is not supported");
            }

            return u;
        }

        protected override Expression VisitBinary(BinaryExpression b)
        {
            _ = sb.Append('(');
            _ = Visit(b.Left);

            _ = b.NodeType switch
            {
                ExpressionType.And => sb.Append(" AND "),
                ExpressionType.Or => sb.Append(" OR"),
                ExpressionType.Equal => sb.Append(" = "),
                ExpressionType.NotEqual => sb.Append(" <> "),
                ExpressionType.LessThan => sb.Append(" < "),
                ExpressionType.LessThanOrEqual => sb.Append(" <= "),
                ExpressionType.GreaterThan => sb.Append(" > "),
                ExpressionType.GreaterThanOrEqual => sb.Append(" >= "),
                _ => throw new NotSupportedException($"The binary operator '{b.NodeType}' is not supported"),
            };
            _ = Visit(b.Right);
            _ = sb.Append(')');

            return b;
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            if (c.Value == null)
            {
                _ = sb.Append("NULL");
            }
            else
            {
                switch (Type.GetTypeCode(c.Value.GetType()))
                {
                    case TypeCode.Boolean:
                        _ = sb.Append(((bool)c.Value) ? 1 : 0);
                        break;

                    case TypeCode.String:
                        _ = sb.Append('\'');
                        _ = sb.Append(c.Value);
                        _ = sb.Append('\'');
                        break;

                    case TypeCode.Object:
                        throw new NotSupportedException($"The constant for '{c.Value}' is not supported");

                    default:
                        _ = sb.Append(c.Value);
                        break;
                }
            }

            return c;
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

        protected override Expression VisitSelect(SelectExpression select)
        {
            _ = sb.Append("SELECT ");

            for (int i = 0, n = select.Columns.Count; i < n; i++)
            {
                ColumnDeclaration column = select.Columns[i];

                if (i > 0)
                {
                    _ = sb.Append(", ");
                }

                ColumnExpression c = (Visit(column.Expression) as ColumnExpression)!;

                if (c == null || c.Name != select.Columns[i].Name)
                {
                    _ = sb.Append(" AS ");
                    _ = sb.Append(column.Name);
                }
            }

            if (select.From != null)
            {
                AppendNewLine(Identation.Same);
                _ = sb.Append("FROM ");
                _ = VisitSource(select.From);
            }

            if (select.Where != null)
            {
                AppendNewLine(Identation.Same);
                _ = sb.Append("WHERE ");
                _ = Visit(select.Where);
            }

            return select;
        }

        protected override Expression VisitJoin(JoinExpression join)
        {
            _ = VisitSource(join.Left);
            AppendNewLine(Identation.Same);
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
            }
            _ = VisitSource(join.Right);
            if (join.Condition != null)
            {
                AppendNewLine(Identation.Inner);
                _ = sb.Append("ON ");
                _ = Visit(join.Condition);
                AppendNewLine(Identation.Outer);
            }
            return join;
        }

        protected override Expression VisitSource(Expression? source)
        {
            if (source == null)
            {
                throw new InvalidOperationException("Select source is null");
            }

            switch ((DbExpressionType)source.NodeType)
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
                    AppendNewLine(Identation.Inner);
                    _ = Visit(select);
                    AppendNewLine(Identation.Outer);
                    _ = sb.Append(')');
                    _ = sb.Append(" AS ");
                    _ = sb.Append(select.Alias);
                    break;

                case DbExpressionType.Join:
                    _ = VisitJoin((JoinExpression)source);
                    break;

                default:
                    throw new InvalidOperationException("Select source is not valid type");
            }

            return source;
        }
    }
}
