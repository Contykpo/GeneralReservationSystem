using GeneralReservationSystem.Application.Common;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace GeneralReservationSystem.Application.Repositories.Util.Interfaces
{
    public enum JoinType { Inner, Left, Right, Full, Cross }
    public enum AggregateFunction { Count, Sum, Min, Max, Average, Custom }

    public sealed record FilterDescriptor<T>(Expression<Func<T, bool>> Predicate);
    public sealed record OrderDescriptor<T, TKey>(Expression<Func<T, TKey>> KeySelector, bool Ascending = false, int Priority = 0);
    public sealed record ProjectionDescriptor<T, TResult>(Expression Selector);
    public sealed record GroupDescriptor<T, TKey>(Expression Selector);
    public sealed record AggregateDescriptor<T, TResult>(AggregateFunction Function, Expression<Func<T, TResult>> Selector, string Name);
    public sealed record PaginationDescriptor(int? Skip = null, int? Take = null, int? Page = null, int? PageSize = null);
    public sealed record JoinDescriptor<TOuter, TInner, TResult>(Expression<Func<TOuter, TInner, bool>> On, Expression<Func<TOuter, TInner, TResult>> ResultSelector, JoinType JoinType = JoinType.Inner);

    public sealed record QueryModel<T>(
        IReadOnlyList<FilterDescriptor<T>> Filters,
        ProjectionDescriptor<T, object>? Projection,
        GroupDescriptor<T, object>? Group,
        IReadOnlyList<AggregateDescriptor<T, object>> Aggregates,
        IReadOnlyList<JoinDescriptor<T, object, object>> Joins,
        IReadOnlyList<OrderDescriptor<T, object>> Orders,
        PaginationDescriptor? Pagination,
        bool IsDistinct
    );

    public sealed class AggregateResult
    {
        private readonly Dictionary<string, object?> _values = new();

        internal AggregateResult(Dictionary<string, object?> values) => _values = values ?? new();

        public TResult Get<TResult>(string name)
        {
            if (_values.TryGetValue(name, out var value) && value is TResult typed)
                return typed;
            throw new InvalidOperationException($"Aggregate '{name}' not found or wrong type.");
        }

        public bool TryGet<TResult>(string name, out TResult? value)
        {
            if (_values.TryGetValue(name, out var raw) && raw is TResult typed)
            {
                value = typed;
                return true;
            }
            value = default;
            return false;
        }

        public IReadOnlyDictionary<string, object?> AsDictionary() => new ReadOnlyDictionary<string, object?>(_values);
    }

    public sealed class GroupResult<TKey, TElement>
    {
        public TKey Key { get; init; } = default!;
        public IReadOnlyList<TElement> Items { get; init; } = Array.Empty<TElement>();
    }

    public interface IQuery<T>
    {
        IQuery<T> Where(Expression<Func<T, bool>> predicate);

        IQuery<TResult> Select<TResult>(Expression<Func<T, TResult>> selector);

        IQuery<(T Outer, TInner Inner)> Join<TInner>(
            Expression<Func<T, TInner, bool>> onPredicate,
            JoinType joinType = JoinType.Inner);

        IQuery<TResult> Join<TInner, TResult>(
            Expression<Func<T, TInner, bool>> onPredicate,
            Expression<Func<T, TInner, TResult>> resultSelector,
            JoinType joinType = JoinType.Inner);

        IQuery<TResult> GroupBy<TKey, TResult>(
            Expression<Func<T, TKey>> keySelector,
            Expression<Func<IGrouping<TKey, T>, TResult>> resultSelector);

        IQuery<TResult> Having<TKey, TResult>(
            Expression<Func<IGrouping<TKey, T>, bool>> predicate);

        IQuery<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector, bool ascending = false);
        IQuery<T> ThenBy<TKey>(Expression<Func<T, TKey>> keySelector, bool ascending = false);
        IQuery<T> ClearOrdering();

        IQuery<T> Page(int page, int pageSize);
        IQuery<T> Skip(int count);
        IQuery<T> Take(int count);
        IQuery<T> ClearPagination();

        IQuery<T> Distinct(bool isDistinct = true);

        AggregateResult Aggregate(params AggregateDescriptor<T, object>[] aggregates);
        Task<AggregateResult> AggregateAsync(IEnumerable<AggregateDescriptor<T, object>> aggregates, CancellationToken cancellationToken = default);

        long Count();
        long Count(Expression<Func<T, bool>> predicate);
        Task<long> CountAsync(CancellationToken cancellationToken = default);
        Task<long> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
        bool Any();
        bool Any(Expression<Func<T, bool>> predicate);
        Task<bool> AnyAsync(CancellationToken cancellationToken = default);
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
        TResult Sum<TResult>(Expression<Func<T, TResult>> selector);
        Task<TResult> SumAsync<TResult>(Expression<Func<T, TResult>> selector, CancellationToken cancellationToken = default);
        TResult Min<TResult>(Expression<Func<T, TResult>> selector);
        Task<TResult> MinAsync<TResult>(Expression<Func<T, TResult>> selector, CancellationToken cancellationToken = default);
        TResult Max<TResult>(Expression<Func<T, TResult>> selector);
        Task<TResult> MaxAsync<TResult>(Expression<Func<T, TResult>> selector, CancellationToken cancellationToken = default);
        double Average<TResult>(Expression<Func<T, TResult>> selector);
        Task<double> AverageAsync<TResult>(Expression<Func<T, TResult>> selector, CancellationToken cancellationToken = default);

        List<T> ToList();
        Task<List<T>> ToListAsync(CancellationToken cancellationToken = default);
        T[] ToArray();
        Task<T[]> ToArrayAsync(CancellationToken cancellationToken = default);
        T First();
        Task<T> FirstAsync(CancellationToken cancellationToken = default);
        T? FirstOrDefault();
        Task<T?> FirstOrDefaultAsync(CancellationToken cancellationToken = default);
        T Single();
        Task<T> SingleAsync(CancellationToken cancellationToken = default);
        T? SingleOrDefault();
        Task<T?> SingleOrDefaultAsync(CancellationToken cancellationToken = default);

        PagedResult<T> ToPagedResult();
        Task<PagedResult<T>> ToPagedResultAsync(CancellationToken cancellationToken = default);
    }
}
