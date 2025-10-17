using GeneralReservationSystem.Application.Helpers;
using System.Linq.Expressions;
using System.Reflection;

namespace GeneralReservationSystem.Infrastructure.Repositories.Util.Sql.Query
{
    internal class ProjectionBuilder : DbExpressionVisitor
    {
        private ParameterExpression row = null!;
        private string rowAlias = null!;
        private static readonly MethodInfo miGetValue = typeof(ProjectionRow).GetMethod("GetValue")!;
        private static readonly MethodInfo miExecuteSubQuery = typeof(ProjectionRow).GetMethod("ExecuteSubQuery")!;

        internal LambdaExpression Build(Expression expression, string alias)
        {
            row = Expression.Parameter(typeof(ProjectionRow), "row");
            rowAlias = alias;
            Expression body = Visit(expression)!;

            return Expression.Lambda(body, row);
        }

        protected override Expression VisitColumn(ColumnExpression column)
        {
            return column.Alias == rowAlias
                ? Expression.Convert(Expression.Call(row, miGetValue, Expression.Constant(column.Ordinal)), column.Type)
                : column;
        }

        protected override Expression VisitProjection(ProjectionExpression proj)
        {
            LambdaExpression subQuery = Expression.Lambda(base.VisitProjection(proj), row);
            Type elementType = TypeHelpers.GetElementType(subQuery.Body.Type);
            MethodInfo mi = miExecuteSubQuery.MakeGenericMethod(elementType);
            return Expression.Convert(
                Expression.Call(row, mi, Expression.Constant(subQuery)),
                proj.Type
            );
        }
    }
}
