using System.Linq.Expressions;

namespace GeneralReservationSystem.Infrastructure.Repositories.Util.Sql.Query
{
    internal class RedundantSubqueryRemover : DbExpressionVisitor
    {
        internal Expression? Remove(Expression? expression)
        {
            return Visit(expression);
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            select = (SelectExpression)base.VisitSelect(select);

            List<SelectExpression>? redundant = new RedundantSubqueryGatherer().Gather(select.From);
            if (redundant != null)
            {
                select = (SelectExpression)new SubqueryRemover().Remove(select, redundant)!;
            }

            if (select.From is SelectExpression fromSelect)
            {
                if (HasSimpleProjection(fromSelect))
                {
                    select = (SelectExpression)new SubqueryRemover().Remove(select, fromSelect)!;
                    Expression? where = select.Where;
                    if (fromSelect.Where != null)
                    {
                        where = where != null ? Expression.And(fromSelect.Where, where) : fromSelect.Where;
                    }
                    if (where != select.Where)
                    {
                        return new SelectExpression(select.Type, select.Alias, select.Columns, select.From, where, select.OrderBy);
                    }
                }
            }

            return select;
        }

        private static bool IsRedudantSubquery(SelectExpression select)
        {
            return HasSimpleProjection(select)
              && select.Where == null
              && (select.OrderBy == null || select.OrderBy.Count == 0);
        }

        private static bool HasSimpleProjection(SelectExpression select)
        {
            foreach (ColumnDeclaration decl in select.Columns)
            {
                if (decl.Expression is not ColumnExpression col || decl.Name != col.Name)
                {
                    return false;
                }
            }
            return true;
        }

        private class RedundantSubqueryGatherer : DbExpressionVisitor
        {
            private List<SelectExpression>? redundant;

            internal List<SelectExpression>? Gather(Expression source)
            {
                _ = Visit(source);
                return redundant;
            }

            protected override Expression VisitSelect(SelectExpression select)
            {
                if (IsRedudantSubquery(select))
                {
                    redundant ??= [];
                    redundant.Add(select);
                }
                return select;
            }
        }
    }
}
