using System.Linq.Expressions;

namespace GeneralReservationSystem.Infrastructure.Repositories.Util.Sql.Query
{
    internal static class DbExtensions
    {
        internal static SelectExpression AddColumn(this SelectExpression select, ColumnDeclaration column)
        {
            List<ColumnDeclaration> columns = [.. select.Columns, column];
            return new SelectExpression(select.Type, select.Alias, columns, select.From, select.Where, select.OrderBy, select.GroupBy, select.IsDistinct, select.Skip, select.Take);
        }

        internal static SelectExpression RemoveColumn(this SelectExpression select, ColumnDeclaration column)
        {
            List<ColumnDeclaration> columns = [.. select.Columns];
            _ = columns.Remove(column);
            return new SelectExpression(select.Type, select.Alias, columns, select.From, select.Where, select.OrderBy, select.GroupBy, select.IsDistinct, select.Skip, select.Take);
        }

        internal static SelectExpression SetDistinct(this SelectExpression select, bool isDistinct)
        {
            return select.IsDistinct != isDistinct
                ? new SelectExpression(select.Type, select.Alias, select.Columns, select.From, select.Where, select.OrderBy, select.GroupBy, isDistinct, select.Skip, select.Take)
                : select;
        }

        internal static SelectExpression SetWhere(this SelectExpression select, Expression where)
        {
            return where != select.Where
                ? new SelectExpression(select.Type, select.Alias, select.Columns, select.From, where, select.OrderBy, select.GroupBy, select.IsDistinct, select.Skip, select.Take)
                : select;
        }

        internal static SelectExpression AddOrderExpression(this SelectExpression select, OrderExpression ordering)
        {
            List<OrderExpression> orderby = [];
            if (select.OrderBy != null)
            {
                orderby.AddRange(select.OrderBy);
            }

            orderby.Add(ordering);
            return new SelectExpression(select.Type, select.Alias, select.Columns, select.From, select.Where, orderby, select.GroupBy, select.IsDistinct, select.Skip, select.Take);
        }

        internal static SelectExpression RemoveOrderExpression(this SelectExpression select, OrderExpression ordering)
        {
            if (select.OrderBy != null && select.OrderBy.Count > 0)
            {
                List<OrderExpression> orderby = [.. select.OrderBy];
                _ = orderby.Remove(ordering);
                return new SelectExpression(select.Type, select.Alias, select.Columns, select.From, select.Where, orderby, select.GroupBy, select.IsDistinct, select.Skip, select.Take);
            }
            return select;
        }

        internal static SelectExpression AddGroupExpression(this SelectExpression select, Expression expression)
        {
            List<Expression> groupby = [];
            if (select.GroupBy != null)
            {
                groupby.AddRange(select.GroupBy);
            }

            groupby.Add(expression);
            return new SelectExpression(select.Type, select.Alias, select.Columns, select.From, select.Where, select.OrderBy, groupby, select.IsDistinct, select.Skip, select.Take);
        }

        internal static SelectExpression RemoveGroupExpression(this SelectExpression select, Expression expression)
        {
            if (select.GroupBy != null && select.GroupBy.Count > 0)
            {
                List<Expression> groupby = [.. select.GroupBy];
                _ = groupby.Remove(expression);
                return new SelectExpression(select.Type, select.Alias, select.Columns, select.From, select.Where, select.OrderBy, groupby, select.IsDistinct, select.Skip, select.Take);
            }
            return select;
        }

        internal static SelectExpression SetSkip(this SelectExpression select, Expression? skip)
        {
            return skip != select.Skip
                ? new SelectExpression(select.Type, select.Alias, select.Columns, select.From, select.Where, select.OrderBy, select.GroupBy, select.IsDistinct, skip, select.Take)
                : select;
        }

        internal static SelectExpression SetTake(this SelectExpression select, Expression? take)
        {
            return take != select.Take
                ? new SelectExpression(select.Type, select.Alias, select.Columns, select.From, select.Where, select.OrderBy, select.GroupBy, select.IsDistinct, select.Skip, take)
                : select;
        }

        internal static SelectExpression AddRedundantSelect(this SelectExpression select, string newAlias)
        {
            SelectExpression mapped = (SelectExpression)ColumnMapper.Map(AliasesProduced.Gather(select.From!), newAlias, select);
            SelectExpression newFrom = new(select.Type, newAlias, select.Columns, select.From, select.Where, select.OrderBy, select.GroupBy, select.IsDistinct, select.Skip, select.Take);
            return new SelectExpression(select.Type, select.Alias, mapped.Columns, newFrom, null, null, null, false, null, null);
        }

        internal static SelectExpression RemoveRedundantFrom(this SelectExpression select)
        {
            return select.From is SelectExpression fromSelect ? SubqueryRemover.Remove(select, fromSelect) : select;
        }

        private class ColumnMapper : DbExpressionVisitor
        {
            private readonly HashSet<string> oldAliases;
            private readonly string newAlias;

            private ColumnMapper(IEnumerable<string> oldAliases, string newAlias)
            {
                this.oldAliases = [.. oldAliases];
                this.newAlias = newAlias;
            }

            internal static Expression Map(IEnumerable<string> oldAliases, string newAlias, Expression expression)
            {
                return new ColumnMapper(oldAliases, newAlias).Visit(expression)!;
            }

            protected override Expression VisitColumn(ColumnExpression column)
            {
                return oldAliases.Contains(column.Alias) ? new ColumnExpression(column.Type, newAlias, column.Name) : column;
            }
        }
    }
}