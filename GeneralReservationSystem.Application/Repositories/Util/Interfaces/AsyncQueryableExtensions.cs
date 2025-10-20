using System.Linq.Expressions;

namespace GeneralReservationSystem.Application.Repositories.Util.Interfaces
{
    public static class AsyncQueryableExtensions
    {
        public static async Task<List<T>> ToListAsync<T>(this IQueryable<T> query, CancellationToken cancellationToken = default)
        {
            if (query is RepositoryQuery<T>)
            {
                RepositoryQueryProvider provider = (query.Provider as RepositoryQueryProvider)!;
                Expression expression = query.Expression;
                IEnumerable<T> result = await provider.ExecuteAsync<IEnumerable<T>>(expression, cancellationToken);
                return [.. result];
            }
            // Fallback to synchronous execution
            return [.. query];
        }

        public static async Task<T?> FirstOrDefaultAsync<T>(this IQueryable<T> query, CancellationToken cancellationToken = default)
        {
            if (query is RepositoryQuery<T>)
            {
                RepositoryQueryProvider provider = (query.Provider as RepositoryQueryProvider)!;
                // Build a FirstOrDefault expression: Queryable.FirstOrDefault(query)
                Expression firstExpr = Expression.Call(
                    typeof(Queryable),
                    nameof(Queryable.FirstOrDefault),
                    [typeof(T)],
                    query.Expression
                );
                return await provider.ExecuteAsync<T?>(firstExpr, cancellationToken);
            }
            // Fallback to synchronous execution
            return query.FirstOrDefault();
        }

        public static async Task<T?> SingleOrDefaultAsync<T>(this IQueryable<T> query, CancellationToken cancellationToken = default)
        {
            if (query is RepositoryQuery<T>)
            {
                RepositoryQueryProvider provider = (query.Provider as RepositoryQueryProvider)!;
                // Build a SingleOrDefault expression: Queryable.SingleOrDefault(query)
                Expression singleExpr = Expression.Call(
                    typeof(Queryable),
                    nameof(Queryable.SingleOrDefault),
                    [typeof(T)],
                    query.Expression
                );
                return await provider.ExecuteAsync<T?>(singleExpr, cancellationToken);
            }
            // Fallback to synchronous execution
            return query.SingleOrDefault();
        }

        public static async Task<int> CountAsync<T>(this IQueryable<T> query, CancellationToken cancellationToken = default)
        {
            if (query is RepositoryQuery<T>)
            {
                RepositoryQueryProvider provider = (query.Provider as RepositoryQueryProvider)!;
                // Build a Count expression: Queryable.Count(query)
                Expression countExpr = Expression.Call(
                    typeof(Queryable),
                    nameof(Queryable.Count),
                    [typeof(T)],
                    query.Expression
                );
                return await provider.ExecuteAsync<int>(countExpr, cancellationToken);
            }
            // Fallback to synchronous execution
            return query.Count();
        }
    }
}
