using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace GeneralReservationSystem.Infrastructure.Repositories.Util.Sql.Query
{
    internal class OrderByRewriter : DbExpressionVisitor
    {
        private IEnumerable<OrderExpression>? gatheredOrderings;
        private bool isOuterMostSelect;

        public Expression? Rewrite(Expression? expression)
        {
            isOuterMostSelect = true;
            return Visit(expression);
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            bool saveIsOuterMostSelect = isOuterMostSelect;
            try
            {
                isOuterMostSelect = false;
                select = (SelectExpression)base.VisitSelect(select);
                if (select.OrderBy != null && select.OrderBy.Count > 0)
                {
                    PrependOrderings(select.OrderBy);
                }

                bool canHaveOrderBy = saveIsOuterMostSelect;
                bool canPassOnOrderings = !saveIsOuterMostSelect;
                IEnumerable<OrderExpression>? orderings = canHaveOrderBy ? gatheredOrderings : null;
                ReadOnlyCollection<ColumnDeclaration> columns = select.Columns;
                if (gatheredOrderings != null)
                {
                    if (canPassOnOrderings)
                    {
                        HashSet<string> producedAliases = new AliasesProduced().Gather(select.From);
                        BindResult project = RebindOrderings(gatheredOrderings, select.Alias, producedAliases,
                            select.Columns);
                        gatheredOrderings = project.Orderings;
                        columns = project.Columns;
                    }
                    else
                    {
                        gatheredOrderings = null;
                    }
                }

                if (orderings != select.OrderBy || columns != select.Columns)
                {
                    select = new SelectExpression(select.Type, select.Alias, columns, select.From, select.Where, orderings);
                }

                return select;
            }
            finally
            {
                isOuterMostSelect = saveIsOuterMostSelect;
            }
        }

        protected override Expression VisitJoin(JoinExpression join)
        {
            Expression left = VisitSource(join.Left);
            IEnumerable<OrderExpression>? leftOrders = gatheredOrderings;
            gatheredOrderings = null;
            Expression right = VisitSource(join.Right);
            PrependOrderings(leftOrders);
            Expression? condition = Visit(join.Condition);
            return left != join.Left || right != join.Right || condition != join.Condition
                ? new JoinExpression(join.Type, join.Join, left, right, condition)
                : join;
        }

        protected void PrependOrderings(IEnumerable<OrderExpression>? newOrderings)
        {
            if (newOrderings != null)
            {
                if (gatheredOrderings == null)
                {
                    gatheredOrderings = newOrderings;
                }
                else
                {
                    if (gatheredOrderings is not List<OrderExpression> list)
                    {
                        gatheredOrderings = list = [.. gatheredOrderings];
                    }

                    list.InsertRange(0, newOrderings);
                }
            }
        }

        protected class BindResult(IEnumerable<ColumnDeclaration> columns, IEnumerable<OrderExpression> orderings)
        {
            public ReadOnlyCollection<ColumnDeclaration> Columns { get; } = columns as ReadOnlyCollection<ColumnDeclaration> ?? new List<ColumnDeclaration>(columns).AsReadOnly();

            public ReadOnlyCollection<OrderExpression> Orderings { get; } = orderings as ReadOnlyCollection<OrderExpression> ?? new List<OrderExpression>(orderings).AsReadOnly();
        }

        protected virtual BindResult RebindOrderings(IEnumerable<OrderExpression> orderings, string alias,
            HashSet<string> existingAliases, IEnumerable<ColumnDeclaration> existingColumns)
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
                            (column != null && declColumn != null && column.Alias == declColumn.Alias &&
                             column.Name == declColumn.Name))
                        {
                            expr = new ColumnExpression(column!.Type, alias, decl.Name, iOrdinal);
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
                        expr = new ColumnExpression(expr.Type, alias, colName, iOrdinal);
                    }

                    newOrderings.Add(new OrderExpression(ordering.OrderType, expr));
                }
            }

            return new BindResult(existingColumns, newOrderings);
        }
    }

    internal class AliasesProduced : DbExpressionVisitor
    {
        private HashSet<string> aliases = null!;

        public HashSet<string> Gather(Expression source)
        {
            aliases = [];
            _ = Visit(source);
            return aliases;
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            _ = aliases.Add(select.Alias);
            return select;
        }

        protected override Expression VisitTable(TableExpression table)
        {
            _ = aliases.Add(table.Alias);
            return table;
        }
    }
}
