using System.Linq.Expressions;

namespace GeneralReservationSystem.Infrastructure.Repositories.Util.Sql.Query
{
    internal class SubqueryRemover : DbExpressionVisitor
    {
        private readonly HashSet<SelectExpression> selectsToRemove;
        private readonly Dictionary<string, Dictionary<string, Expression>> map;

        private SubqueryRemover(IEnumerable<SelectExpression> selectsToRemove)
        {
            this.selectsToRemove = [.. selectsToRemove];
            map = this.selectsToRemove.ToDictionary(d => d.Alias, d => d.Columns.ToDictionary(d2 => d2.Name, d2 => d2.Expression));
        }

        internal static SelectExpression Remove(SelectExpression outerSelect, params SelectExpression[] selectsToRemove)
        {
            return Remove(outerSelect, (IEnumerable<SelectExpression>)selectsToRemove)!;
        }

        internal static SelectExpression? Remove(SelectExpression outerSelect, IEnumerable<SelectExpression> selectsToRemove)
        {
            return (SelectExpression)new SubqueryRemover(selectsToRemove).Visit(outerSelect)!;
        }

        internal static ProjectionExpression Remove(ProjectionExpression projection, params SelectExpression[] selectsToRemove)
        {
            return Remove(projection, (IEnumerable<SelectExpression>)selectsToRemove);
        }

        internal static ProjectionExpression Remove(ProjectionExpression projection, IEnumerable<SelectExpression> selectsToRemove)
        {
            return (ProjectionExpression)new SubqueryRemover(selectsToRemove).Visit(projection)!;
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            return selectsToRemove.Contains(select) ? Visit(select.From)! : base.VisitSelect(select);
        }

        protected override Expression VisitColumn(ColumnExpression column)
        {
            return map.TryGetValue(column.Alias, out Dictionary<string, Expression>? nameMap)
                ? nameMap.TryGetValue(column.Name, out Expression? expr) ? Visit(expr)! : throw new Exception("Reference to undefined column")
                : column;
        }
    }
}