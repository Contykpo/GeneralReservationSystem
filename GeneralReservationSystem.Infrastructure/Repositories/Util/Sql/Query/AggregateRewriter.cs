using System.Linq.Expressions;

namespace GeneralReservationSystem.Infrastructure.Repositories.Util.Sql.Query
{
    internal class AggregateRewriter : DbExpressionVisitor
    {
        private readonly ILookup<string, AggregateSubqueryExpression> lookup;
        private readonly Dictionary<AggregateSubqueryExpression, Expression> map;

        private AggregateRewriter(Expression? expr)
        {
            map = [];
            lookup = AggregateGatherer.Gather(expr).ToLookup(a => a.GroupByAlias);
        }

        internal static Expression? Rewrite(Expression? expr)
        {
            return new AggregateRewriter(expr).Visit(expr);
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            select = (SelectExpression)base.VisitSelect(select);
            if (lookup.Contains(select.Alias))
            {
                List<ColumnDeclaration> aggColumns = [.. select.Columns];
                foreach (AggregateSubqueryExpression ae in lookup[select.Alias])
                {
                    string name = "agg" + aggColumns.Count;
                    ColumnDeclaration cd = new(name, ae.AggregateInGroupSelect);
                    map.Add(ae, new ColumnExpression(ae.Type, ae.GroupByAlias, name));
                    aggColumns.Add(cd);
                }
                return new SelectExpression(select.Type, select.Alias, aggColumns, select.From, select.Where, select.OrderBy, select.GroupBy, select.IsDistinct, select.Skip, select.Take);
            }
            return select;
        }

        protected override Expression VisitAggregateSubquery(AggregateSubqueryExpression aggregate)
        {
            return map.TryGetValue(aggregate, out Expression? mapped) ? mapped : Visit(aggregate.AggregateAsSubquery)!;
        }

        private class AggregateGatherer : DbExpressionVisitor
        {
            private readonly List<AggregateSubqueryExpression> aggregates = [];
            private AggregateGatherer()
            {
            }

            internal static List<AggregateSubqueryExpression> Gather(Expression? expression)
            {
                AggregateGatherer gatherer = new();
                _ = gatherer.Visit(expression);
                return gatherer.aggregates;
            }

            protected override Expression VisitAggregateSubquery(AggregateSubqueryExpression aggregate)
            {
                aggregates.Add(aggregate);
                return base.VisitAggregateSubquery(aggregate);
            }
        }
    }
}