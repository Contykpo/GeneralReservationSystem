using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace GeneralReservationSystem.Infrastructure.Repositories.Util.Sql.Query
{
    internal class UnusedColumnRemover : DbExpressionVisitor
    {
        private readonly Dictionary<string, HashSet<string>> allColumnsUsed = [];

        internal Expression? Remove(Expression? expression)
        {
            return Visit(expression);
        }

        protected override Expression VisitColumn(ColumnExpression column)
        {
            if (!allColumnsUsed.TryGetValue(column.Alias, out HashSet<string>? columns))
            {
                columns = [];
                allColumnsUsed.Add(column.Alias, columns);
            }
            _ = columns.Add(column.Name);
            return column;
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            ReadOnlyCollection<ColumnDeclaration> columns = select.Columns;

            if (allColumnsUsed.TryGetValue(select.Alias, out HashSet<string>? columnsUsed))
            {
                List<ColumnDeclaration>? alternate = null;
                for (int i = 0, n = select.Columns.Count; i < n; i++)
                {
                    ColumnDeclaration? decl = select.Columns[i];
                    if (!columnsUsed.Contains(decl.Name))
                    {
                        decl = null;  // null means it gets omitted
                    }
                    else
                    {
                        Expression? expr = Visit(decl.Expression);
                        if (expr != decl.Expression)
                        {
                            decl = new ColumnDeclaration(decl.Name, decl.Expression);
                        }
                    }
                    if (decl != select.Columns[i] && alternate == null)
                    {
                        alternate = [];
                        for (int j = 0; j < i; j++)
                        {
                            alternate.Add(select.Columns[j]);
                        }
                    }
                    if (decl != null && alternate != null)
                    {
                        alternate.Add(decl);
                    }
                }
                if (alternate != null)
                {
                    columns = alternate.AsReadOnly();
                }
            }

            ReadOnlyCollection<OrderExpression>? orderbys = VisitOrderBy(select.OrderBy);
            Expression? where = Visit(select.Where);
            Expression from = Visit(select.From)!;

            return columns != select.Columns || orderbys != select.OrderBy || where != select.Where || from != select.From
                ? new SelectExpression(select.Type, select.Alias, columns, from, where, orderbys)
                : select;
        }

        protected override Expression VisitProjection(ProjectionExpression projection)
        {
            Expression projector = Visit(projection.Projector)!;
            SelectExpression source = (SelectExpression?)Visit(projection.Source)!;
            return projector != projection.Projector || source != projection.Source ? new ProjectionExpression(source, projector) : projection;
        }

        protected override Expression VisitJoin(JoinExpression join)
        {
            Expression? condition = Visit(join.Condition);
            Expression? right = VisitSource(join.Right);
            Expression? left = VisitSource(join.Left);
            return left != join.Left || right != join.Right || condition != join.Condition
                ? new JoinExpression(join.Type, join.Join, left, right, condition)
                : join;
        }
    }
}
