using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace GeneralReservationSystem.Infrastructure.Repositories.Util.Sql.Query
{
    internal class RedundantSubqueryRemover : DbExpressionVisitor
    {
        private RedundantSubqueryRemover()
        {
        }

        internal static Expression? Remove(Expression? expression)
        {
            expression = new RedundantSubqueryRemover().Visit(expression);
            expression = SubqueryMerger.Merge(expression);
            return expression;
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            select = (SelectExpression)base.VisitSelect(select);

            List<SelectExpression> redundant = RedundantSubqueryGatherer.Gather(select.From!);
            if (redundant != null)
            {
                select = SubqueryRemover.Remove(select, redundant)!;
            }

            return select;
        }

        protected override Expression VisitProjection(ProjectionExpression proj)
        {
            proj = (ProjectionExpression)base.VisitProjection(proj);
            if (proj.Source.From is SelectExpression)
            {
                List<SelectExpression> redundant = RedundantSubqueryGatherer.Gather(proj.Source);
                if (redundant != null)
                {
                    proj = SubqueryRemover.Remove(proj, redundant);
                }
            }
            return proj;
        }

        internal static bool IsSimpleProjection(SelectExpression select)
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

        internal static bool IsNameMapProjection(SelectExpression select)
        {
            if (select.From is not SelectExpression fromSelect || select.Columns.Count != fromSelect.Columns.Count)
            {
                return false;
            }

            for (int i = 0, n = select.Columns.Count; i < n; i++)
            {
                if (select.Columns[i].Expression is not ColumnExpression col || !(col.Name == fromSelect.Columns[i].Name))
                {
                    return false;
                }
            }
            return true;
        }

        internal static bool IsInitialProjection(SelectExpression select)
        {
            return select.From is TableExpression;
        }

        private class RedundantSubqueryGatherer : DbExpressionVisitor
        {
            private List<SelectExpression> redundant = null!;

            internal static List<SelectExpression> Gather(Expression source)
            {
                RedundantSubqueryGatherer gatherer = new();
                _ = gatherer.Visit(source);
                return gatherer.redundant;
            }

            private static bool IsRedudantSubquery(SelectExpression select)
            {
                return (select.From is SelectExpression || select.From is TableExpression)
                    && (IsSimpleProjection(select) || IsNameMapProjection(select))
                    && !select.IsDistinct
                    && select.Take == null
                    && select.Skip == null
                    && select.Where == null
                    && (select.OrderBy == null || select.OrderBy.Count == 0)
                    && (select.GroupBy == null || select.GroupBy.Count == 0);
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

            protected override Expression VisitSubquery(SubqueryExpression subquery)
            {
                // don't gather inside scalar & exists
                return subquery;
            }
        }

        private class AggregateChecker : DbExpressionVisitor
        {
            private bool hasAggregate = false;
            private AggregateChecker()
            {
            }

            internal static bool HasAggregates(Expression expression)
            {
                AggregateChecker checker = new();
                _ = checker.Visit(expression);
                return checker.hasAggregate;
            }

            protected override Expression VisitAggregate(AggregateExpression aggregate)
            {
                hasAggregate = true;
                return aggregate;
            }

            protected override Expression VisitSelect(SelectExpression select)
            {
                // only consider aggregates in these locations
                _ = Visit(select.Where);
                _ = VisitOrderBy(select.OrderBy);
                _ = VisitColumnDeclarations(select.Columns);
                return select;
            }

            protected override Expression VisitSubquery(SubqueryExpression subquery)
            {
                // don't count aggregates in subqueries
                return subquery;
            }
        }

        internal class SubqueryMerger : DbExpressionVisitor
        {
            private SubqueryMerger()
            {
            }

            internal static Expression? Merge(Expression? expression)
            {
                return new SubqueryMerger().Visit(expression);
            }

            private bool isTopLevel = true;

            protected override Expression VisitSelect(SelectExpression select)
            {
                bool wasTopLevel = isTopLevel;
                isTopLevel = false;

                select = (SelectExpression)base.VisitSelect(select);

                while (CanMergeWithFrom(select, wasTopLevel))
                {
                    SelectExpression fromSelect = (SelectExpression)select.From!;

                    select = SubqueryRemover.Remove(select, fromSelect);

                    Expression where = select.Where!;
                    if (fromSelect.Where != null)
                    {
                        where = where != null ? Expression.And(fromSelect.Where, where) : fromSelect.Where;
                    }
                    ReadOnlyCollection<OrderExpression>? orderBy = select.OrderBy != null && select.OrderBy.Count > 0 ? select.OrderBy : fromSelect.OrderBy;
                    ReadOnlyCollection<Expression>? groupBy = select.GroupBy != null && select.GroupBy.Count > 0 ? select.GroupBy : fromSelect.GroupBy;
                    Expression? skip = select.Skip ?? fromSelect.Skip;
                    Expression? take = select.Take ?? fromSelect.Take;
                    bool isDistinct = select.IsDistinct | fromSelect.IsDistinct;

                    if (where != select.Where
                        || orderBy != select.OrderBy
                        || groupBy != select.GroupBy
                        || isDistinct != select.IsDistinct
                        || skip != select.Skip
                        || take != select.Take)
                    {
                        select = new SelectExpression(select.Type, select.Alias, select.Columns, select.From, where, orderBy, groupBy, isDistinct, skip, take);
                    }
                }

                return select;
            }

            private static bool CanMergeWithFrom(SelectExpression select, bool isTopLevel)
            {
                if (select.From is not SelectExpression fromSelect)
                {
                    return false;
                }

                bool frmHasSimpleProjection = IsSimpleProjection(fromSelect);
                bool frmHasNameMapProjection = IsNameMapProjection(fromSelect);
                if (!(frmHasSimpleProjection || frmHasNameMapProjection))
                {
                    return false;
                }

                bool selHasNameMapProjection = IsNameMapProjection(select);
                bool selHasOrderBy = select.OrderBy != null && select.OrderBy.Count > 0;
                bool selHasGroupBy = select.GroupBy != null && select.GroupBy.Count > 0;
                bool selHasAggregates = AggregateChecker.HasAggregates(select);
                bool frmHasOrderBy = fromSelect.OrderBy != null && fromSelect.OrderBy.Count > 0;
                bool frmHasGroupBy = fromSelect.GroupBy != null && fromSelect.GroupBy.Count > 0;
                // Both cannot have orderby
                if (selHasOrderBy && frmHasOrderBy)
                {
                    return false;
                }
                // Both cannot have groupby
                if (selHasOrderBy && frmHasOrderBy)
                {
                    return false;
                }
                // Cannot move forward order-by if outer has group-by
                if (frmHasOrderBy && (selHasGroupBy || selHasAggregates || select.IsDistinct))
                {
                    return false;
                }
                // Cannot move forward group-by if outer has where clause
                if (frmHasGroupBy && (select.Where != null))
                {
                    return false;
                }
                // Cannot move forward a take if outer has take or skip or distinct
                if (fromSelect.Take != null && (select.Take != null || select.Skip != null || select.IsDistinct || selHasAggregates || selHasGroupBy))
                {
                    return false;
                }
                // Cannot move forward a skip if outer has skip or distinct
                if (fromSelect.Skip != null && (select.Skip != null || select.IsDistinct || selHasAggregates || selHasGroupBy))
                {
                    return false;
                }
                // Cannot move forward a distinct if outer has take, skip, groupby or a different projection
                return !fromSelect.IsDistinct || (select.Take == null && select.Skip == null && selHasNameMapProjection && !selHasGroupBy && !selHasAggregates && (!selHasOrderBy || isTopLevel));
            }
        }
    }
}