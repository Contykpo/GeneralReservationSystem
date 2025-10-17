using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace GeneralReservationSystem.Infrastructure.Repositories.Util.Sql.Query
{
    internal class DbExpressionVisitor : ExpressionVisitor
    {
        public override Expression? Visit(Expression? expression)
        {
            return expression == null
                ? null
                : (DbExpressionType)expression.NodeType switch
                {
                    DbExpressionType.Table => VisitTable((TableExpression)expression),
                    DbExpressionType.Column => VisitColumn((ColumnExpression)expression),
                    DbExpressionType.Select => VisitSelect((SelectExpression)expression),
                    DbExpressionType.Projection => VisitProjection((ProjectionExpression)expression),
                    DbExpressionType.Join => VisitJoin((JoinExpression)expression),
                    _ => base.Visit(expression),
                };
        }

        protected virtual Expression VisitTable(TableExpression table)
        {
            return table;
        }

        protected virtual Expression VisitColumn(ColumnExpression column)
        {
            return column;
        }

        protected virtual Expression VisitSelect(SelectExpression select)
        {
            Expression from = VisitSource(select.From)!;
            Expression where = Visit(select.Where)!;

            ReadOnlyCollection<ColumnDeclaration> columns = VisitColumnDeclarations(select.Columns);

            ReadOnlyCollection<OrderExpression>? orderBy = VisitOrderBy(select.OrderBy);

            return from != select.From || where != select.Where || columns != select.Columns || orderBy != select.OrderBy
                ? new SelectExpression(select.Type, select.Alias, columns, from, where, orderBy)
                : select;
        }

        protected ReadOnlyCollection<OrderExpression>? VisitOrderBy(ReadOnlyCollection<OrderExpression>? expressions)
        {
            if (expressions != null)
            {
                List<OrderExpression>? alternate = null;

                for (int i = 0, n = expressions.Count; i < n; i++)
                {
                    OrderExpression expression = expressions[i];
                    Expression e = Visit(expression.Expression)!;

                    if (alternate == null && e != expression.Expression)
                    {
                        alternate = [.. expressions.Take(i)];
                    }

                    alternate?.Add(new OrderExpression(expression.OrderType, e));
                }

                if (alternate != null)
                {
                    return alternate.AsReadOnly();
                }
            }
            return expressions;
        }

        protected virtual Expression VisitSource(Expression source)
        {
            return Visit(source)!;
        }

        protected virtual Expression VisitProjection(ProjectionExpression proj)
        {
            SelectExpression source = (SelectExpression)Visit(proj.Source)!;
            Expression projector = Visit(proj.Projector)!;

            return source != proj.Source || projector != proj.Projector ? new ProjectionExpression(source, projector) : proj;
        }

        protected ReadOnlyCollection<ColumnDeclaration> VisitColumnDeclarations(ReadOnlyCollection<ColumnDeclaration> columns)
        {
            List<ColumnDeclaration>? alternate = null;

            for (int i = 0, n = columns.Count; i < n; i++)
            {
                ColumnDeclaration column = columns[i];
                Expression e = Visit(column.Expression)!;

                if (alternate == null && e != column.Expression)
                {
                    alternate = [.. columns.Take(i)];
                }

                alternate?.Add(new ColumnDeclaration(column.Name, e));
            }

            return alternate != null ? alternate.AsReadOnly() : columns;
        }

        protected virtual Expression VisitJoin(JoinExpression join)
        {
            Expression left = Visit(join.Left)!;
            Expression right = Visit(join.Right)!;
            Expression condition = Visit(join.Condition)!;
            return left != join.Left || right != join.Right || condition != join.Condition
                ? new JoinExpression(join.Type, join.Join, left, right, condition)
                : join;
        }

        /*protected virtual ReadOnlyCollection<Expression>? VisitExpressionList(ReadOnlyCollection<Expression>? original)
        {
            if (original != null)
            {
                List<Expression> list = [];
                for (int i = 0, n = original.Count; i < n; i++)
                {
                    Expression p = Visit(original[i])!;
                    if (list != null)
                    {
                        list.Add(p);
                    }
                    else if (p != original[i])
                    {
                        list = new List<Expression>(n);
                        for (int j = 0; j < i; j++)
                        {
                            list.Add(original[j]);
                        }
                        list.Add(p);
                    }
                }
                if (list != null)
                {
                    return list.AsReadOnly();
                }
            }

            return original;
        }*/
    }
}
