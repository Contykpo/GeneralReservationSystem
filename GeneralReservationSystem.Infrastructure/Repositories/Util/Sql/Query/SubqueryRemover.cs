using System.Linq.Expressions;

namespace GeneralReservationSystem.Infrastructure.Repositories.Util.Sql.Query
{
    internal class SubqueryRemover : DbExpressionVisitor
    {
        private HashSet<SelectExpression> selectsToRemove = null!;
        private Dictionary<string, Dictionary<string, Expression>> map = null!;

        public Expression? Remove(SelectExpression outerSelect, params SelectExpression[] selectsToRemove)
        {
            return Remove(outerSelect, (IEnumerable<SelectExpression>)selectsToRemove);
        }

        public Expression? Remove(SelectExpression outerSelect, IEnumerable<SelectExpression> selectsToRemove)
        {
            this.selectsToRemove = [.. selectsToRemove];
            map = selectsToRemove.ToDictionary(d => d.Alias, d => d.Columns.ToDictionary(d2 => d2.Name, d2 => d2.Expression));
            return Visit(outerSelect);
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            return selectsToRemove.Contains(select) ? Visit(select.From)! : base.VisitSelect(select);
        }

        protected override Expression VisitColumn(ColumnExpression column)
        {
            return map.TryGetValue(column.Alias, out Dictionary<string, Expression>? nameMap)
                ? nameMap!.TryGetValue(column.Name, out Expression? expression) ? Visit(expression)! : throw new Exception("Reference to undefined column")
                : column;
        }
    }
}
