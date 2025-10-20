using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace GeneralReservationSystem.Infrastructure.Repositories.Util.Sql.Query
{
    internal class UnusedColumnRemover : DbExpressionVisitor
    {
        private readonly Dictionary<string, HashSet<string>> allColumnsUsed;

        private UnusedColumnRemover()
        {
            allColumnsUsed = [];
        }

        internal static Expression? Remove(Expression? expression)
        {
            return new UnusedColumnRemover().Visit(expression);
        }

        private void MarkColumnAsUsed(string alias, string name)
        {
            if (!allColumnsUsed.TryGetValue(alias, out HashSet<string>? columns))
            {
                columns = [];
                allColumnsUsed.Add(alias, columns);
            }
            _ = columns.Add(name);
        }

        private bool IsColumnUsed(string alias, string name)
        {
            if (allColumnsUsed.TryGetValue(alias, out HashSet<string>? columnsUsed))
            {
                if (columnsUsed != null)
                {
                    return columnsUsed.Contains(name);
                }
            }
            return false;
        }

        private void ClearColumnsUsed(string alias)
        {
            allColumnsUsed[alias] = [];
        }

        protected override Expression VisitColumn(ColumnExpression column)
        {
            MarkColumnAsUsed(column.Alias, column.Name);
            return column;
        }

        protected override Expression VisitSubquery(SubqueryExpression subquery)
        {
            if ((subquery.NodeType == (ExpressionType)DbExpressionType.Scalar ||
                subquery.NodeType == (ExpressionType)DbExpressionType.In) &&
                subquery.Select != null)
            {
                System.Diagnostics.Debug.Assert(subquery.Select.Columns.Count == 1);
                MarkColumnAsUsed(subquery.Select.Alias, subquery.Select.Columns[0].Name);
            }
            return base.VisitSubquery(subquery);
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            ReadOnlyCollection<ColumnDeclaration> columns = select.Columns;

            List<ColumnDeclaration>? alternate = null;
            for (int i = 0, n = select.Columns.Count; i < n; i++)
            {
                ColumnDeclaration? decl = select.Columns[i];
                if (select.IsDistinct || IsColumnUsed(select.Alias, decl.Name))
                {
                    Expression expr = Visit(decl.Expression)!;
                    if (expr != decl.Expression)
                    {
                        decl = new ColumnDeclaration(decl.Name, expr);
                    }
                }
                else
                {
                    decl = null;
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

            Expression? take = Visit(select.Take);
            Expression? skip = Visit(select.Skip);
            ReadOnlyCollection<Expression>? groupbys = VisitExpressionList(select.GroupBy);
            ReadOnlyCollection<OrderExpression>? orderbys = VisitOrderBy(select.OrderBy);
            Expression where = Visit(select.Where)!;
            Expression from = Visit(select.From)!;

            ClearColumnsUsed(select.Alias);

            if (columns != select.Columns
                || take != select.Take
                || skip != select.Skip
                || orderbys != select.OrderBy
                || groupbys != select.GroupBy
                || where != select.Where
                || from != select.From)
            {
                select = new SelectExpression(select.Type, select.Alias, columns, from, where, orderbys, groupbys, select.IsDistinct, skip, take);
            }

            return select;
        }

        protected override Expression VisitProjection(ProjectionExpression projection)
        {
            Expression projector = Visit(projection.Projector)!;
            SelectExpression source = (SelectExpression)Visit(projection.Source)!;
            return projector != projection.Projector || source != projection.Source
                ? new ProjectionExpression(source, projector, projection.Aggregator)
                : projection;
        }

        protected override Expression VisitJoin(JoinExpression join)
        {
            Expression condition = Visit(join.Condition)!;
            Expression right = VisitSource(join.Right)!;
            Expression left = VisitSource(join.Left)!;
            return left != join.Left || right != join.Right || condition != join.Condition
                ? new JoinExpression(join.Type, join.Join, left, right, condition)
                : join;
        }
    }
}