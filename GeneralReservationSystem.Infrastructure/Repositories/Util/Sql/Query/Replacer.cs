using System.Linq.Expressions;

namespace GeneralReservationSystem.Infrastructure.Repositories.Util.Sql.Query
{
    internal class Replacer : DbExpressionVisitor
    {
        private Expression searchFor = null!;
        private Expression replaceWith = null!;

        internal Expression Replace(Expression expression, Expression searchFor, Expression replaceWith)
        {
            this.searchFor = searchFor;
            this.replaceWith = replaceWith;
            return Visit(expression)!;
        }

        public override Expression? Visit(Expression? exp)
        {
            return exp == searchFor ? replaceWith : base.Visit(exp);
        }
    }
}
