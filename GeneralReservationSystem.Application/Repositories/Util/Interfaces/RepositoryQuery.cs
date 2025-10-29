using GeneralReservationSystem.Application.Common;
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
            expression = Expression.Constant(this);
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
            return ((IEnumerable<T>)queryProvider.ExecuteExpression(expression)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)queryProvider.ExecuteExpression(expression)).GetEnumerator();
        }

        private async Task<IEnumerator<T>> GetEnumeratorAsync(CancellationToken cancellationToken = default)
        {
            return ((IEnumerable<T>)await queryProvider.ExecuteExpressionAsync(expression, cancellationToken)).GetEnumerator();
        }

        public override string ToString()
        {
            return expression.NodeType == ExpressionType.Constant &&
                ((ConstantExpression)expression).Value == this
                ? "Table(" + typeof(T) + ")"
                : expression.ToString();
        }
    }

    public static class QueryableExtensions
    {
        public static IQueryable<T> ApplyFilters<T>(this IQueryable<T> query, IEnumerable<Filter> filters)
        {
            var filterList = filters?.ToList();
            if (filterList == null || filterList.Count == 0)
                return query;

            var expressions = filterList.Select(f => f.ToExpression<T>()).ToList();
            var parameter = Expression.Parameter(typeof(T), "x");
            Expression? body = null;
            foreach (var expr in expressions)
            {
                var replaced = new ReplaceParameterVisitor(expr.Parameters[0], parameter).Visit(expr.Body);
                body = body == null ? replaced : Expression.OrElse(body, replaced);
            }
            var lambda = Expression.Lambda<Func<T, bool>>(body!, parameter);
            return query.Where(lambda);
        }

        public static IQueryable<T> ApplySorts<T>(this IQueryable<T> query, IEnumerable<SortOption> sorts)
        {
            bool first = true;
            IOrderedQueryable<T>? orderedQuery = query as IOrderedQueryable<T>;

            ArgumentNullException.ThrowIfNull(orderedQuery, "The query must be of type IOrderedQueryable<T> to apply sorts.");

            foreach (SortOption sort in sorts)
            {
                Expression<Func<T, object>> sortExpression = sort.ToExpression<T>();
                orderedQuery = first
                    ? (sort.Direction == SortDirection.Asc
                        ? orderedQuery.OrderBy(sortExpression)
                        : orderedQuery.OrderByDescending(sortExpression))
                    : (sort.Direction == SortDirection.Asc
                        ? orderedQuery.ThenBy(sortExpression)
                        : orderedQuery.ThenByDescending(sortExpression));
                first = false;
            }
            if (first)
            {
                string firstPropertyOrFieldName = typeof(T).GetProperties().FirstOrDefault()?.Name ??
                    typeof(T).GetFields().FirstOrDefault()?.Name ??
                    throw new InvalidOperationException($"No fields or properties found for type '{typeof(T)}'");
                Expression<Func<T, object>> defaultSortExpression = new SortOption(firstPropertyOrFieldName).ToExpression<T>();
                orderedQuery = orderedQuery.OrderBy(defaultSortExpression);
            }
            return orderedQuery;
        }

        private class ReplaceParameterVisitor : ExpressionVisitor
        {
            private readonly ParameterExpression _oldParam;
            private readonly ParameterExpression _newParam;
            public ReplaceParameterVisitor(ParameterExpression oldParam, ParameterExpression newParam)
            {
                _oldParam = oldParam;
                _newParam = newParam;
            }
            protected override Expression VisitParameter(ParameterExpression node)
            {
                return node == _oldParam ? _newParam : base.VisitParameter(node);
            }
        }
    }
}
