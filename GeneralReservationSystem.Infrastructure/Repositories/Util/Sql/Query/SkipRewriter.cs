using System.Linq.Expressions;

namespace GeneralReservationSystem.Infrastructure.Repositories.Util.Sql.Query
{
    internal class SkipRewriter : DbExpressionVisitor
    {
        private int aliasCount;

        private SkipRewriter()
        {
        }

        internal static Expression? Rewrite(Expression? expression)
        {
            return new SkipRewriter().Visit(expression);
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            select = (SelectExpression)base.VisitSelect(select);
            if (select.Skip != null)
            {
                SelectExpression newSelect = select.SetSkip(null).SetTake(null);
                bool canAddColumn = !select.IsDistinct && (select.GroupBy == null || select.GroupBy.Count == 0);
                if (!canAddColumn)
                {
                    newSelect = newSelect.AddRedundantSelect("s" + aliasCount++);
                }
                newSelect = newSelect.AddColumn(new ColumnDeclaration("rownum", new RowNumberExpression(select.OrderBy)));

                newSelect = newSelect.AddRedundantSelect("s" + aliasCount++);
                newSelect = newSelect.RemoveColumn(newSelect.Columns[^1]);

                string newAlias = ((SelectExpression)newSelect.From!).Alias;
                ColumnExpression rnCol = new(typeof(int), newAlias, "rownum");
                Expression where = select.Take != null
                    ? new BetweenExpression(rnCol, Expression.Add(select.Skip, Expression.Constant(1)), Expression.Add(select.Skip, select.Take))
                    : Expression.GreaterThan(rnCol, select.Skip);
                if (newSelect.Where != null)
                {
                    where = Expression.And(newSelect.Where, where);
                }
                newSelect = newSelect.SetWhere(where);

                select = newSelect;
            }
            return select;
        }
    }
}