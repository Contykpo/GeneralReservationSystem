using System.Collections;
using System.Linq.Expressions;

namespace GeneralReservationSystem.Application.Repositories.Util.Interfaces
{
    public class RepositoryQuery<T> : IOrderedQueryable<T>
    {
        private readonly RepositoryQueryProvider queryProvider;
        private readonly Expression expression;

        Type IQueryable.ElementType => typeof(T);

        Expression IQueryable.Expression => expression;

        IQueryProvider IQueryable.Provider => queryProvider;

        public RepositoryQuery(RepositoryQueryProvider queryProvider)
        {
            this.queryProvider = queryProvider;
            this.expression = Expression.Constant(this);
        }

        public RepositoryQuery(RepositoryQueryProvider queryProvider, Expression expression)
        {
            if (!typeof(IQueryable<T>).IsAssignableFrom(expression.Type))
            {
                throw new ArgumentOutOfRangeException(nameof(expression));
            }

            this.queryProvider = queryProvider;
            this.expression = expression;
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            //return (this as IQueryable).Provider.Execute<IEnumerator<T>>(expression);
            return ((IEnumerable<T>)queryProvider.ExecuteExpression(expression)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            //return (this as IQueryable).Provider.Execute<IEnumerator<T>>(expression);
            return ((IEnumerable)queryProvider.ExecuteExpression(expression)).GetEnumerator();
        }

        private async Task<IEnumerator<T>> GetEnumeratorAsync(CancellationToken cancellationToken = default)
        {
            //return queryProvider.ExecuteAsync<IEnumerator<T>>(expression, cancellationToken);
            return ((IEnumerable<T>)await queryProvider.ExecuteExpressionAsync(expression, cancellationToken)).GetEnumerator();
        }

        public override string ToString()
        {
            return queryProvider.GetQueryText(expression);
        }
    }
}
