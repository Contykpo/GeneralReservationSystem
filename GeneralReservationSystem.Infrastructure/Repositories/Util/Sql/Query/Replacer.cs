using System.Linq.Expressions;

namespace GeneralReservationSystem.Infrastructure.Repositories.Util.Sql.Query
{
    internal class Replacer : DbExpressionVisitor
    {
        private readonly Expression searchFor;
        private readonly Expression replaceWith;
        private Replacer(Expression searchFor, Expression replaceWith)
        {
            this.searchFor = searchFor;
            this.replaceWith = replaceWith;
        }
        internal static Expression? Replace(Expression? expression, Expression searchFor, Expression replaceWith)
        {
            return new Replacer(searchFor, replaceWith).Visit(expression);
        }
        public override Expression? Visit(Expression? exp)
        {
            return exp == searchFor ? replaceWith : base.Visit(exp);
        }
    }
}
