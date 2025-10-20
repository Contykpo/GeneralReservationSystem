using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace GeneralReservationSystem.Infrastructure.Repositories.Util.Sql.Query
{
    internal class OrderByRewriter : DbExpressionVisitor
    {
        private IList<OrderExpression>? gatheredOrderings = null;
        private HashSet<string>? uniqueColumns = null;
        private bool isOuterMostSelect = true;

        internal static Expression? Rewrite(Expression? expression)
        {
            return new OrderByRewriter().Visit(expression);
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            bool saveIsOuterMostSelect = isOuterMostSelect;
            try
            {
                isOuterMostSelect = false;
                select = (SelectExpression)base.VisitSelect(select);

                bool hasOrderBy = select.OrderBy != null && select.OrderBy.Count > 0;
                bool hasGroupBy = select.GroupBy != null && select.GroupBy.Count > 0;
                bool canHaveOrderBy = saveIsOuterMostSelect || select.Take != null || select.Skip != null;
                bool canReceiveOrderings = canHaveOrderBy && !hasGroupBy && !select.IsDistinct;

                if (hasOrderBy)
                {
                    PrependOrderings(select.OrderBy!);
                }

                IEnumerable<OrderExpression>? orderings = null;
                if (canReceiveOrderings)
                {
                    orderings = gatheredOrderings;
                }
                else if (canHaveOrderBy)
                {
                    orderings = select.OrderBy;
                }
                bool canPassOnOrderings = !saveIsOuterMostSelect && !hasGroupBy && !select.IsDistinct;
                ReadOnlyCollection<ColumnDeclaration> columns = select.Columns;
                if (gatheredOrderings != null)
                {
                    if (canPassOnOrderings)
                    {
                        HashSet<string> producedAliases = AliasesProduced.Gather(select.From!);
                        BindResult project = RebindOrderings(gatheredOrderings, select.Alias, producedAliases, select.Columns);
                        gatheredOrderings = null;
                        PrependOrderings(project.Orderings);
                        columns = project.Columns!;
                    }
                    else
                    {
                        gatheredOrderings = null;
                    }
                }
                if (orderings != select.OrderBy || columns != select.Columns)
                {
                    select = new SelectExpression(select.Type, select.Alias, columns, select.From, select.Where, orderings, select.GroupBy, select.IsDistinct, select.Skip, select.Take);
                }
                return select;
            }
            finally
            {
                isOuterMostSelect = saveIsOuterMostSelect;
            }
        }

        protected override Expression VisitSubquery(SubqueryExpression subquery)
        {
            IList<OrderExpression>? saveOrderings = gatheredOrderings;
            gatheredOrderings = null;
            Expression result = base.VisitSubquery(subquery);
            gatheredOrderings = saveOrderings;
            return result;
        }

        protected override Expression VisitJoin(JoinExpression join)
        {
            Expression left = VisitSource(join.Left)!;
            IList<OrderExpression>? leftOrders = gatheredOrderings;
            gatheredOrderings = null;
            Expression right = VisitSource(join.Right)!;
            PrependOrderings(leftOrders);
            Expression condition = Visit(join.Condition)!;
            return left != join.Left || right != join.Right || condition != join.Condition
                ? new JoinExpression(join.Type, join.Join, left, right, condition)
                : join;
        }

        protected void PrependOrderings(IList<OrderExpression>? newOrderings)
        {
            if (newOrderings != null)
            {
                if (gatheredOrderings == null)
                {
                    gatheredOrderings = [];
                    uniqueColumns = [];
                }
                for (int i = newOrderings.Count - 1; i >= 0; i--)
                {
                    OrderExpression ordering = newOrderings[i];
                    if (ordering.Expression is ColumnExpression column)
                    {
                        string hash = column.Alias + ":" + column.Name;
                        if (!uniqueColumns!.Contains(hash))
                        {
                            gatheredOrderings.Insert(0, ordering);
                            _ = uniqueColumns.Add(hash);
                        }
                    }
                    else
                    {
                        gatheredOrderings.Insert(0, ordering);
                    }
                }
            }
        }

        protected class BindResult(IEnumerable<ColumnDeclaration> columns, IEnumerable<OrderExpression> orderings)
        {
            public ReadOnlyCollection<ColumnDeclaration>? Columns { get; } = columns as ReadOnlyCollection<ColumnDeclaration> ?? new List<ColumnDeclaration>(columns).AsReadOnly();
            public ReadOnlyCollection<OrderExpression>? Orderings { get; } = orderings as ReadOnlyCollection<OrderExpression> ?? new List<OrderExpression>(orderings).AsReadOnly();
        }

        protected virtual BindResult RebindOrderings(IEnumerable<OrderExpression> orderings, string alias, HashSet<string> existingAliases, IEnumerable<ColumnDeclaration> existingColumns)
        {
            List<ColumnDeclaration>? newColumns = null;
            List<OrderExpression> newOrderings = [];
            foreach (OrderExpression ordering in orderings)
            {
                Expression expr = ordering.Expression;
                ColumnExpression? column = expr as ColumnExpression;
                if (column == null || (existingAliases != null && existingAliases.Contains(column.Alias)))
                {
                    int iOrdinal = 0;
                    foreach (ColumnDeclaration decl in existingColumns)
                    {
                        ColumnExpression? declColumn = decl.Expression as ColumnExpression;
                        if (decl.Expression == ordering.Expression ||
                            (column != null && declColumn != null && column.Alias == declColumn.Alias && column.Name == declColumn.Name))
                        {
                            expr = new ColumnExpression(column!.Type, alias, decl.Name);
                            break;
                        }
                        iOrdinal++;
                    }
                    if (expr == ordering.Expression)
                    {
                        if (newColumns == null)
                        {
                            newColumns = [.. existingColumns];
                            existingColumns = newColumns;
                        }
                        string colName = column != null ? column.Name : "c" + iOrdinal;
                        newColumns.Add(new ColumnDeclaration(colName, ordering.Expression));
                        expr = new ColumnExpression(expr.Type, alias, colName);
                    }
                    newOrderings.Add(new OrderExpression(ordering.OrderType, expr));
                }
            }
            return new BindResult(existingColumns, newOrderings);
        }
    }
}
