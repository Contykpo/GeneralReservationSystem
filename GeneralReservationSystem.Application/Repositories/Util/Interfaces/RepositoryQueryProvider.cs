using GeneralReservationSystem.Application.Helpers;
using System.Linq.Expressions;

namespace GeneralReservationSystem.Application.Repositories.Util.Interfaces
{
    public abstract class RepositoryQueryProvider : IQueryProvider
    {
        IQueryable IQueryProvider.CreateQuery(Expression expression)
        {
            Type elementType = TypeHelpers.GetElementType(expression.Type);

            return (IQueryable)Activator.CreateInstance(typeof(RepositoryQuery<>).MakeGenericType(elementType), [this, expression])!;
        }

        IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(Expression expression)
        {
            Type elementType = TypeHelpers.GetElementType(expression.Type);

            return (IQueryable<TElement>)Activator.CreateInstance(typeof(RepositoryQuery<>).MakeGenericType(elementType), [this, expression])!;
        }

        object IQueryProvider.Execute(Expression expression)
        {
            return ExecuteExpression(expression);
        }

        TResult IQueryProvider.Execute<TResult>(Expression expression)
        {
            return (TResult)ExecuteExpression(expression)!;
        }

        public Task<object> ExecuteAsync(Expression expression, CancellationToken cancellationToken = default)
        {
            return ExecuteExpressionAsync(expression, cancellationToken);
        }

        public Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            return ExecuteExpressionAsync(expression, cancellationToken)
                .ContinueWith(task => (TResult)task.Result!, cancellationToken);
        }

        public abstract string GetQueryText(Expression expression);

        public abstract object ExecuteExpression(Expression expression);

        public abstract Task<object> ExecuteExpressionAsync(Expression expression, CancellationToken cancellationToken = default);
    }
}
