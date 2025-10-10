using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.Helpers;
using GeneralReservationSystem.Application.Repositories.Util.Interfaces;
using GeneralReservationSystem.Infrastructure.Helpers;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;

namespace GeneralReservationSystem.Infrastructure.Repositories.Util.Sql
{
    public class Query<T>(Func<DbConnection> connectionFactory, DbTransaction? transaction = null) : IQuery<T>
    {
        protected QueryModel<T> Model { get; set; } = new QueryModel<T>(
                Filters: [],
                Projection: null,
                Group: null,
                Aggregates: [],
                Joins: [],
                Orders: [],
                Pagination: null,
                IsDistinct: false
            );
        protected readonly Func<DbConnection> ConnectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        protected readonly DbTransaction? Transaction = transaction;

        protected Query(Func<DbConnection> connectionFactory, DbTransaction? transaction, QueryModel<T> model)
            : this(connectionFactory, transaction)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
        }

        private static IReadOnlyList<OrderDescriptor<T, object>> AppendOrder(IReadOnlyList<OrderDescriptor<T, object>> existing, OrderDescriptor<T, object> newOrder)
        {
            var list = existing?.ToList() ?? [];
            var prioritized = new OrderDescriptor<T, object>(newOrder.KeySelector, newOrder.Ascending, list.Count + 1);
            list.Add(prioritized);
            return list;
        }

        private static Expression<Func<T, object>> ConvertToObjectSelector<TKey>(Expression<Func<T, TKey>> selector)
        {
            ArgumentNullException.ThrowIfNull(selector);
            var param = selector.Parameters.Single();
            var body = Expression.Convert(selector.Body, typeof(object));
            return Expression.Lambda<Func<T, object>>(body, param);
        }

        public IQuery<T> Where(Expression<Func<T, bool>> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            var filters = Model.Filters?.ToList() ?? [];
            filters.Add(new FilterDescriptor<T>(predicate));
            Model = new QueryModel<T>(
                Filters: filters,
                Projection: Model.Projection,
                Group: Model.Group,
                Aggregates: Model.Aggregates,
                Joins: Model.Joins,
                Orders: Model.Orders,
                Pagination: Model.Pagination,
                IsDistinct: Model.IsDistinct
            );
            return this;
        }

        public IQuery<T> ApplyFilters(IEnumerable<Filter> filters)
        {
            ArgumentNullException.ThrowIfNull(filters);

            var query = this;
            foreach (var filter in filters)
            {
                var predicate = SqlExpressionBuilder.BuildFilterPredicate<T>(filter);
                if (predicate != null)
                {
                    query = (Query<T>)query.Where(predicate);
                }
            }

            return query;
        }

        public IQuery<T> ApplySorting(IEnumerable<SortOption> sortOptions)
        {
            ArgumentNullException.ThrowIfNull(sortOptions);

            var query = this;
            var isFirst = true;

            foreach (var sortOption in sortOptions)
            {
                var keySelector = SqlExpressionBuilder.BuildSortExpression<T>(sortOption);
                if (keySelector != null)
                {
                    var ascending = sortOption.Direction == SortDirection.Asc;
                    
                    if (isFirst)
                    {
                        query = (Query<T>)query.OrderBy(keySelector, ascending);
                        isFirst = false;
                    }
                    else
                    {
                        query = (Query<T>)query.ThenBy(keySelector, ascending);
                    }
                }
            }

            return query;
        }

        public IQuery<TResult> Select<TResult>(Expression<Func<T, TResult>> selector)
        {
            ArgumentNullException.ThrowIfNull(selector);

            var newModel = new QueryModel<TResult>(
                Filters: [],
                Projection: new ProjectionDescriptor<TResult, object>(selector),
                Group: null,
                Aggregates: [],
                Joins: [],
                Orders: [],
                Pagination: null,
                IsDistinct: Model.IsDistinct
            );

            return new Query<TResult>(ConnectionFactory, Transaction, newModel);
        }

        public IQuery<TResult> SelectMany<TCollection, TResult>(Expression<Func<T, IEnumerable<TCollection>>> collectionSelector, Expression<Func<T, TCollection, TResult>> resultSelector)
        {
            ArgumentNullException.ThrowIfNull(collectionSelector);
            ArgumentNullException.ThrowIfNull(resultSelector);

            var newModel = new QueryModel<TResult>(
                Filters: [],
                Projection: new ProjectionDescriptor<TResult, object>(resultSelector),
                Group: null,
                Aggregates: [],
                Joins: [],
                Orders: [],
                Pagination: null,
                IsDistinct: Model.IsDistinct
            );

            return new Query<TResult>(ConnectionFactory, Transaction, newModel);
        }

        public IQuery<(T Outer, TInner Inner)> Join<TInner>(Expression<Func<T, TInner, bool>> onPredicate, JoinType joinType = JoinType.Inner)
        {
            ArgumentNullException.ThrowIfNull(onPredicate);

            var outerParam = Expression.Parameter(typeof(T), "outer");
            var innerParam = Expression.Parameter(typeof(TInner), "inner");
            var tupleType = typeof(ValueTuple<,>).MakeGenericType(typeof(T), typeof(TInner));
            var ctor = tupleType.GetConstructor([typeof(T), typeof(TInner)])!;
            var body = Expression.New(ctor, outerParam, innerParam);
            var resultSelector = Expression.Lambda(body, outerParam, innerParam);

            return Join<TInner, (T Outer, TInner Inner)>(onPredicate, (Expression<Func<T, TInner, (T, TInner)>>)resultSelector, joinType);
        }

        public IQuery<TResult> Join<TInner, TResult>(Expression<Func<T, TInner, bool>> onPredicate, Expression<Func<T, TInner, TResult>> resultSelector, JoinType joinType = JoinType.Inner)
        {
            ArgumentNullException.ThrowIfNull(onPredicate);
            ArgumentNullException.ThrowIfNull(resultSelector);

            var outerParam = Expression.Parameter(typeof(T), "outer");
            var innerObjParam = Expression.Parameter(typeof(object), "innerObj");

            var convertedInnerForOn = Expression.Convert(innerObjParam, typeof(TInner));
            var onInvoke = Expression.Invoke(onPredicate, outerParam, convertedInnerForOn);
            var onWrapper = Expression.Lambda<Func<T, object, bool>>(onInvoke, outerParam, innerObjParam);

            var resultInvoke = Expression.Invoke(resultSelector, outerParam, convertedInnerForOn);
            var resultConvertToObject = Expression.Convert(resultInvoke, typeof(object));
            var resultWrapper = Expression.Lambda<Func<T, object, object>>(resultConvertToObject, outerParam, innerObjParam);

            var joinDesc = new JoinDescriptor<T, object, object>(
                On: onWrapper,
                ResultSelector: resultWrapper,
                JoinType: joinType
            );

            var joins = Model.Joins?.ToList() ?? [];
            joins.Add(joinDesc);

            Model = new QueryModel<T>(
                Filters: Model.Filters,
                Projection: Model.Projection,
                Group: Model.Group,
                Aggregates: Model.Aggregates,
                Joins: joins,
                Orders: Model.Orders,
                Pagination: Model.Pagination,
                IsDistinct: Model.IsDistinct
            );

            return new Query<TResult>(ConnectionFactory, Transaction, (QueryModel<TResult>)(object)Model);
        }

        public IQuery<TResult> GroupBy<TKey, TResult>(Expression<Func<T, TKey>> keySelector, Expression<Func<IGrouping<TKey, T>, TResult>> resultSelector)
        {
            ArgumentNullException.ThrowIfNull(keySelector);
            ArgumentNullException.ThrowIfNull(resultSelector);

            var groupDesc = new GroupDescriptor<T, object>(keySelector);
            var projection = new ProjectionDescriptor<TResult, object>(resultSelector);

            Model = new QueryModel<T>(
                Filters: Model.Filters,
                Projection: (ProjectionDescriptor<T, object>)(object)projection,
                Group: (GroupDescriptor<T, object>)(object)groupDesc,
                Aggregates: Model.Aggregates,
                Joins: Model.Joins,
                Orders: Model.Orders,
                Pagination: Model.Pagination,
                IsDistinct: Model.IsDistinct
            );

            return new Query<TResult>(ConnectionFactory, Transaction, (QueryModel<TResult>)(object)Model);
        }

        public IQuery<TResult> Having<TKey, TResult>(Expression<Func<IGrouping<TKey, T>, bool>> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            var projection = new ProjectionDescriptor<TResult, object>(predicate);

            Model = new QueryModel<T>(
                Filters: Model.Filters,
                Projection: (ProjectionDescriptor<T, object>)(object)projection,
                Group: Model.Group,
                Aggregates: Model.Aggregates,
                Joins: Model.Joins,
                Orders: Model.Orders,
                Pagination: Model.Pagination,
                IsDistinct: Model.IsDistinct
            );

            return new Query<TResult>(ConnectionFactory, Transaction, (QueryModel<TResult>)(object)Model);
        }

        public IQuery<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector, bool ascending = false)
        {
            ArgumentNullException.ThrowIfNull(keySelector);
            var converted = ConvertToObjectSelector(keySelector);
            var order = new OrderDescriptor<T, object>(converted, ascending, 0);
            var orders = AppendOrder(Model.Orders, order);
            Model = new QueryModel<T>(
                Filters: Model.Filters,
                Projection: Model.Projection,
                Group: Model.Group,
                Aggregates: Model.Aggregates,
                Joins: Model.Joins,
                Orders: orders,
                Pagination: Model.Pagination,
                IsDistinct: Model.IsDistinct
            );
            return this;
        }

        public IQuery<T> ThenBy<TKey>(Expression<Func<T, TKey>> keySelector, bool ascending = false)
        {
            return OrderBy(keySelector, ascending);
        }

        public IQuery<T> ClearOrdering()
        {
            Model = new QueryModel<T>(
                Filters: Model.Filters,
                Projection: Model.Projection,
                Group: Model.Group,
                Aggregates: Model.Aggregates,
                Joins: Model.Joins,
                Orders: [],
                Pagination: Model.Pagination,
                IsDistinct: Model.IsDistinct
            );
            return this;
        }

        public IQuery<T> Page(int page, int pageSize)
        {
            Model = new QueryModel<T>(
                Filters: Model.Filters,
                Projection: Model.Projection,
                Group: Model.Group,
                Aggregates: Model.Aggregates,
                Joins: Model.Joins,
                Orders: Model.Orders,
                Pagination: new PaginationDescriptor(Page: page, PageSize: pageSize),
                IsDistinct: Model.IsDistinct
            );
            return this;
        }

        public IQuery<T> Skip(int count)
        {
            var pagination = Model.Pagination ?? new PaginationDescriptor();
            Model = new QueryModel<T>(
                Filters: Model.Filters,
                Projection: Model.Projection,
                Group: Model.Group,
                Aggregates: Model.Aggregates,
                Joins: Model.Joins,
                Orders: Model.Orders,
                Pagination: new PaginationDescriptor(Skip: count, Take: pagination?.Take, Page: pagination?.Page, PageSize: pagination?.PageSize),
                IsDistinct: Model.IsDistinct
            );
            return this;
        }

        public IQuery<T> Take(int count)
        {
            var pagination = Model.Pagination ?? new PaginationDescriptor();
            Model = new QueryModel<T>(
                Filters: Model.Filters,
                Projection: Model.Projection,
                Group: Model.Group,
                Aggregates: Model.Aggregates,
                Joins: Model.Joins,
                Orders: Model.Orders,
                Pagination: new PaginationDescriptor(Skip: pagination?.Skip, Take: count, Page: pagination?.Page, PageSize: pagination?.PageSize),
                IsDistinct: Model.IsDistinct
            );
            return this;
        }

        public IQuery<T> ClearPagination()
        {
            Model = new QueryModel<T>(
                Filters: Model.Filters,
                Projection: Model.Projection,
                Group: Model.Group,
                Aggregates: Model.Aggregates,
                Joins: Model.Joins,
                Orders: Model.Orders,
                Pagination: null,
                IsDistinct: Model.IsDistinct
            );
            return this;
        }

        public IQuery<T> Distinct(bool isDistinct = true)
        {
            Model = new QueryModel<T>(
                Filters: Model.Filters,
                Projection: Model.Projection,
                Group: Model.Group,
                Aggregates: Model.Aggregates,
                Joins: Model.Joins,
                Orders: Model.Orders,
                Pagination: Model.Pagination,
                IsDistinct: isDistinct
            );
            return this;
        }

        private sealed class SqlQueryBuilder(QueryModel<T> model)
        {
            private readonly List<KeyValuePair<string, object?>> _parameters = [];
            private int _paramCounter = 0;

            public (string SelectSql, IReadOnlyList<(string Column, string Alias)> Selected, bool SelectAll, Type SourceType) BuildSelectClause()
            {
                Type sourceType = typeof(T);
                LambdaExpression? projectionLambda = null;
                if (model.Projection != null)
                {
                    projectionLambda = model.Projection.Selector as LambdaExpression;
                    if (projectionLambda != null && projectionLambda.Parameters.Count > 0)
                        sourceType = projectionLambda.Parameters[0].Type;
                }

                var tableName = EntityHelper.GetTableName(sourceType);

                var selected = new List<(string Column, string Alias)>();
                bool selectAll = false;
                if (projectionLambda == null)
                {
                    foreach (var prop in sourceType.GetProperties())
                    {
                        var col = EntityHelper.GetColumnName(prop);
                        selected.Add((col, prop.Name));
                    }
                    selectAll = true;
                }
                else
                {
                    var body = projectionLambda.Body;
                    if (body is MemberInitExpression mi)
                    {
                        foreach (MemberAssignment assign in mi.Bindings.Cast<MemberAssignment>())
                        {
                            if (assign.Expression is MemberExpression mex && mex.Member is PropertyInfo pinfo)
                            {
                                var col = EntityHelper.GetColumnName(pinfo);
                                selected.Add((col, assign.Member.Name));
                            }
                            else { selectAll = true; break; }
                        }
                    }
                    else if (body is NewExpression ne)
                    {
                        for (int i = 0; i < ne.Arguments.Count; i++)
                        {
                            var arg = ne.Arguments[i];
                            var member = ne.Members != null && i < ne.Members.Count ? ne.Members[i] : null;
                            if (arg is MemberExpression mex && mex.Member is PropertyInfo pinfo && member != null)
                            {
                                var col = EntityHelper.GetColumnName(pinfo);
                                selected.Add((col, member.Name));
                            }
                            else { selectAll = true; break; }
                        }
                    }
                    else if (body is MemberExpression mb && mb.Member is PropertyInfo pinfo)
                    {
                        var col = EntityHelper.GetColumnName(pinfo);
                        selected.Add((col, pinfo.Name));
                    }
                    else if (body is ParameterExpression)
                    {
                        foreach (var prop in sourceType.GetProperties())
                        {
                            var col = EntityHelper.GetColumnName(prop);
                            selected.Add((col, prop.Name));
                        }
                        selectAll = true;
                    }
                    else
                    {
                        foreach (var prop in sourceType.GetProperties())
                        {
                            var col = EntityHelper.GetColumnName(prop);
                            selected.Add((col, prop.Name));
                        }
                        selectAll = true;
                    }
                }

                var sb = new System.Text.StringBuilder();
                sb.Append("SELECT ");
                if (model.IsDistinct) sb.Append("DISTINCT ");
                if (selected.Count == 0) sb.Append('*');
                else sb.Append(SqlCommandHelper.BuildColumnListWithAliases(selected, tableName));

                return (sb.ToString(), selected, selectAll, sourceType);
            }

            public (string Sql, IReadOnlyList<KeyValuePair<string, object?>> Parameters, IReadOnlyList<(string Column, string Alias)> SelectedColumns, Type SourceType, bool SelectAll) Build()
            {
                var (selectSql, selected, selectAll, sourceType) = BuildSelectClause();
                var tableName = EntityHelper.GetTableName(sourceType);
                var qualifiedTable = SqlCommandHelper.FormatQualifiedTableName(tableName);

                var sb = new System.Text.StringBuilder();
                sb.Append(selectSql);
                sb.Append(" FROM ");
                sb.Append(qualifiedTable);

                var joins = BuildJoins(tableName);
                if (!string.IsNullOrEmpty(joins)) { sb.Append(' ').Append(joins); }

                var (whereSql, _) = BuildWhereClause(tableName);
                if (!string.IsNullOrEmpty(whereSql)) { sb.Append(' ').Append(whereSql); }

                var group = BuildGroupBy(tableName);
                if (!string.IsNullOrEmpty(group)) { sb.Append(' ').Append(group); }

                var order = BuildOrderBy(tableName);
                if (!string.IsNullOrEmpty(order))
                {
                    sb.Append(' ').Append(order);
                }
                else if (model.Pagination != null)
                {
                    var firstProp = sourceType.GetProperties().FirstOrDefault();
                    if (firstProp != null)
                    {
                        var col = EntityHelper.GetColumnName(firstProp);
                        var qualified = SqlCommandHelper.FormatQualifiedTableName(tableName);
                        sb.Append(" ORDER BY ").Append($"{qualified}.[{col}]");
                    }
                }

                var pagination = BuildPagination();
                if (!string.IsNullOrEmpty(pagination)) { sb.Append(' ').Append(pagination); }

                return (sb.ToString(), _parameters, selected, sourceType, selectAll);
            }

            public (string Sql, IReadOnlyList<KeyValuePair<string, object?>> Parameters) BuildWhereClause(string tableName)
            {
                if (model.Filters == null || model.Filters.Count == 0)
                    return (string.Empty, Array.Empty<KeyValuePair<string, object?>>().ToList());

                var parts = new List<string>();
                foreach (var f in model.Filters)
                {
                    if (f.Predicate is LambdaExpression le)
                    {
                        var resolver = new Func<ParameterExpression, string>(p => tableName);
                        parts.Add(SqlExpressionBuilder.TranslateExpression(le.Body, resolver, _parameters, ref _paramCounter));
                    }
                }
                if (parts.Count == 0) return (string.Empty, Array.Empty<KeyValuePair<string, object?>>().ToList());
                return ("WHERE " + string.Join(" AND ", parts), _parameters.ToList());
            }

            public string BuildOrderBy(string tableName)
            {
                var orders = model.Orders?.OrderBy(o => o.Priority).ToList();
                if (orders == null || orders.Count == 0) return string.Empty;
                var parts = new List<string>();
                foreach (var o in orders)
                {
                    if (o.KeySelector is LambdaExpression lle)
                    {
                        var member = SqlExpressionBuilder.ExtractMember(lle.Body);
                        if (member != null && member.Member is PropertyInfo pi)
                        {
                            var col = EntityHelper.GetColumnName(pi);
                            var qualified = SqlCommandHelper.FormatQualifiedTableName(tableName);
                            parts.Add($"{qualified}.[{col}] {(o.Ascending ? "ASC" : "DESC")}");
                        }
                    }
                }
                if (parts.Count == 0) return string.Empty;
                return "ORDER BY " + string.Join(", ", parts);
            }

            public string BuildPagination()
            {
                if (model.Pagination == null) return string.Empty;

                var pagination = model.Pagination;
                int offset = 0;
                int fetch = 0;

                if (pagination.Skip.HasValue)
                {
                    offset = pagination.Skip.Value;
                }
                else if (pagination.Page.HasValue && pagination.PageSize.HasValue)
                {
                    offset = (pagination.Page.Value - 1) * pagination.PageSize.Value;
                }

                if (pagination.Take.HasValue)
                {
                    fetch = pagination.Take.Value;
                }
                else if (pagination.PageSize.HasValue)
                {
                    fetch = pagination.PageSize.Value;
                }

                if (fetch > 0)
                {
                    return $"OFFSET {offset} ROWS FETCH NEXT {fetch} ROWS ONLY";
                }

                return string.Empty;
            }

            public string BuildJoins(string outerTableName)
            {
                if (model.Joins == null || model.Joins.Count == 0) return string.Empty;
                var parts = new List<string>();
                foreach (var join in model.Joins)
                {
                    if (join.On is LambdaExpression onLambda)
                    {
                        var body = onLambda.Body;
                        LambdaExpression? origOn = null;
                        if (body is InvocationExpression inv)
                        {
                            origOn = inv.Expression as LambdaExpression;
                        }
                        if (origOn == null && onLambda.Parameters.Count == 2)
                        {
                            origOn = onLambda as LambdaExpression;
                        }

                        if (origOn != null && origOn.Parameters.Count >= 2)
                        {
                            var innerParam = origOn.Parameters[1];
                            var innerType = innerParam.Type;
                            var innerTableName = EntityHelper.GetTableName(innerType);
                            var qualifiedInnerTable = SqlCommandHelper.FormatQualifiedTableName(innerTableName);

                            var joinKeyword = join.JoinType switch
                            {
                                JoinType.Left => "LEFT JOIN",
                                JoinType.Right => "RIGHT JOIN",
                                JoinType.Full => "FULL JOIN",
                                JoinType.Cross => "CROSS JOIN",
                                _ => "INNER JOIN"
                            };

                            string onSql;
                            try
                            {
                                var resolver = new Func<ParameterExpression, string>(p =>
                                {
                                    if (p == origOn.Parameters[0]) return outerTableName;
                                    if (p == origOn.Parameters[1]) return innerTableName;
                                    return outerTableName;
                                });
                                onSql = SqlExpressionBuilder.TranslateExpression(origOn.Body, resolver, _parameters, ref _paramCounter);
                            }
                            catch
                            {
                                continue;
                            }

                            parts.Add($"{joinKeyword} {qualifiedInnerTable} ON {onSql}");
                        }
                    }
                }
                return string.Join(" ", parts);
            }

            public string BuildGroupBy(string tableName)
            {
                if (model.Group == null) return string.Empty;
                if (model.Group.Selector is LambdaExpression gLE && gLE.Body is MemberExpression gm && gm.Member is PropertyInfo gpi)
                {
                    var gcol = EntityHelper.GetColumnName(gpi);
                    var qualified = SqlCommandHelper.FormatQualifiedTableName(tableName);
                    return "GROUP BY " + $"{qualified}.[{gcol}]";
                }
                return string.Empty;
            }

            public (string SqlFragment, IReadOnlyList<string> Names) BuildAggregates(IEnumerable<AggregateDescriptor<T, object>> aggregates)
            {
                ArgumentNullException.ThrowIfNull(aggregates);
                var aggs = aggregates.ToList();
                if (aggs.Count == 0) return (string.Empty, Array.Empty<string>());

                Type sourceType = typeof(T);
                if (model.Projection != null)
                {
                    if (model.Projection.Selector is LambdaExpression projectionLambda && projectionLambda.Parameters.Count > 0)
                        sourceType = projectionLambda.Parameters[0].Type;
                }

                var tableName = EntityHelper.GetTableName(sourceType);
                var qualifiedTable = SqlCommandHelper.FormatQualifiedTableName(tableName);

                var parts = new List<string>();
                var names = new List<string>();
                foreach (var agg in aggs)
                {
                    var alias = agg.Name ?? agg.Function.ToString();
                    if (agg.Function == AggregateFunction.Count)
                    {
                        if (agg.Selector is LambdaExpression sel && SqlExpressionBuilder.ExtractMember(sel.Body) is MemberExpression m && m.Member is PropertyInfo pi)
                        {
                            var col = EntityHelper.GetColumnName(pi);
                            parts.Add($"COUNT({qualifiedTable}.[{col}]) AS [{alias}]");
                        }
                        else parts.Add($"COUNT(1) AS [{alias}]");
                    }
                    else
                    {
                        if (agg.Selector is LambdaExpression sel && SqlExpressionBuilder.ExtractMember(sel.Body) is MemberExpression m && m.Member is PropertyInfo pi)
                        {
                            var col = EntityHelper.GetColumnName(pi);
                            var func = agg.Function switch
                            {
                                AggregateFunction.Sum => "SUM",
                                AggregateFunction.Min => "MIN",
                                AggregateFunction.Max => "MAX",
                                AggregateFunction.Average => "AVG",
                                _ => throw new NotSupportedException($"Aggregate function {agg.Function} is not supported")
                            };
                            parts.Add($"{func}({qualifiedTable}.[{col}]) AS [{alias}]");
                        }
                        else throw new NotSupportedException("Aggregate selector must be a simple member access for SUM/MIN/MAX/AVG");
                    }
                    names.Add(alias);
                }
                return (string.Join(", ", parts), names);
            }

            public (string Sql, IReadOnlyList<KeyValuePair<string, object?>> Parameters, IReadOnlyList<string> AggregateNames) BuildAggregate(IEnumerable<AggregateDescriptor<T, object>> aggregates)
            {
                ArgumentNullException.ThrowIfNull(aggregates);
                Type sourceType = typeof(T);
                if (model.Projection != null)
                {
                    if (model.Projection.Selector is LambdaExpression projectionLambda && projectionLambda.Parameters.Count > 0)
                        sourceType = projectionLambda.Parameters[0].Type;
                }
                var tableName = EntityHelper.GetTableName(sourceType);
                var qualifiedTable = SqlCommandHelper.FormatQualifiedTableName(tableName);

                var (fragment, names) = BuildAggregates(aggregates);
                var sb = new System.Text.StringBuilder();
                sb.Append("SELECT ");
                sb.Append(fragment);
                sb.Append(" FROM ");
                sb.Append(qualifiedTable);

                var joins = BuildJoins(tableName);
                if (!string.IsNullOrEmpty(joins)) sb.Append(' ').Append(joins);

                var (whereSql, _) = BuildWhereClause(tableName);
                if (!string.IsNullOrEmpty(whereSql)) sb.Append(' ').Append(whereSql);

                var group = BuildGroupBy(tableName);
                if (!string.IsNullOrEmpty(group)) sb.Append(' ').Append(group);

                return (sb.ToString(), _parameters, names);
            }
        }

        public AggregateResult Aggregate(params AggregateDescriptor<T, object>[] aggregates)
        {
            ArgumentNullException.ThrowIfNull(aggregates);
            var builder = new SqlQueryBuilder(Model);
            var (sql, parameters, names) = builder.BuildAggregate(aggregates);

            var values = new Dictionary<string, object?>();
            using var conn = ConnectionFactory();
            if (conn.State != System.Data.ConnectionState.Open) conn.Open();
            using var cmd = SqlCommandHelper.CreateCommand(conn, Transaction);
            cmd.CommandText = sql;
            SqlCommandHelper.AddParameters(cmd, parameters);
            using var reader = SqlCommandHelper.ExecuteReader(cmd);
            if (reader.Read())
            {
                for (int i = 0; i < names.Count; i++)
                {
                    var val = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    values[names[i]] = val;
                }
            }
            return CreateAggregateResult(values);
        }

        public Task<AggregateResult> AggregateAsync(IEnumerable<AggregateDescriptor<T, object>> aggregates, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(aggregates);
            return AggregateAsyncInternal([.. aggregates], cancellationToken);
        }

        private async Task<AggregateResult> AggregateAsyncInternal(AggregateDescriptor<T, object>[] aggregates, CancellationToken cancellationToken)
        {
            var builder = new SqlQueryBuilder(Model);
            var (sql, parameters, names) = builder.BuildAggregate(aggregates);

            await using var conn = await SqlCommandHelper.CreateAndOpenConnectionAsync(ConnectionFactory, cancellationToken);
            await using var cmd = SqlCommandHelper.CreateCommand(conn, Transaction);
            cmd.CommandText = sql;
            SqlCommandHelper.AddParameters(cmd, parameters);
            await using var reader = await SqlCommandHelper.ExecuteReaderAsync(cmd, cancellationToken);
            var values = new Dictionary<string, object?>();
            if (await reader.ReadAsync(cancellationToken))
            {
                for (int i = 0; i < names.Count; i++)
                {
                    var val = await reader.IsDBNullAsync(i, cancellationToken) ? null : reader.GetValue(i);
                    values[names[i]] = val;
                }
            }
            return CreateAggregateResult(values);
        }

        private static AggregateResult CreateAggregateResult(Dictionary<string, object?> values)
        {
            var ctor = typeof(AggregateResult).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .FirstOrDefault(c => c.GetParameters().Length == 1 && c.GetParameters()[0].ParameterType == typeof(Dictionary<string, object?>)) ?? throw new InvalidOperationException("Could not locate AggregateResult constructor via reflection.");
            return (AggregateResult)ctor.Invoke([values ?? []]);
        }

        private static Expression<Func<T, object>> ConvertSelectorToObject<TResult>(Expression<Func<T, TResult>> selector)
        {
            ArgumentNullException.ThrowIfNull(selector);
            var param = selector.Parameters.Single();
            var body = Expression.Convert(selector.Body, typeof(object));
            return Expression.Lambda<Func<T, object>>(body, param);
        }

        public long Count()
        {
            var agg = new AggregateDescriptor<T, object>(AggregateFunction.Count, (Expression<Func<T, object>>)(t => 1), "Count");
            var builder = new SqlQueryBuilder(Model);
            var (sql, parameters, _) = builder.BuildAggregate([agg]);
            var obj = SqlCommandHelper.ExecuteScalar(ConnectionFactory, Transaction, sql, parameters);
            return Convert.ToInt64(obj ?? 0L);
        }

        public long Count(Expression<Func<T, bool>> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            var filters = Model.Filters?.ToList() ?? [];
            filters.Add(new FilterDescriptor<T>(predicate));
            var tempModel = new QueryModel<T>(filters, Model.Projection, Model.Group, Model.Aggregates, Model.Joins, Model.Orders, Model.Pagination, Model.IsDistinct);
            var agg = new AggregateDescriptor<T, object>(AggregateFunction.Count, (Expression<Func<T, object>>)(t => 1), "Count");
            var builder = new SqlQueryBuilder(tempModel);
            var (sql, parameters, _) = builder.BuildAggregate([agg]);
            var obj = SqlCommandHelper.ExecuteScalar(ConnectionFactory, Transaction, sql, parameters);
            return Convert.ToInt64(obj ?? 0L);
        }

        public Task<long> CountAsync(CancellationToken cancellationToken = default)
        {
            return CountAsyncInternal(null, cancellationToken);
        }

        public Task<long> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return CountAsyncInternal(predicate, cancellationToken);
        }

        private async Task<long> CountAsyncInternal(Expression<Func<T, bool>>? predicate, CancellationToken cancellationToken)
        {
            QueryModel<T> model = Model;
            if (predicate != null)
            {
                var filters = Model.Filters?.ToList() ?? [];
                filters.Add(new FilterDescriptor<T>(predicate));
                model = new QueryModel<T>(filters, Model.Projection, Model.Group, Model.Aggregates, Model.Joins, Model.Orders, Model.Pagination, Model.IsDistinct);
            }
            var agg = new AggregateDescriptor<T, object>(AggregateFunction.Count, (Expression<Func<T, object>>)(t => 1), "Count");
            var builder = new SqlQueryBuilder(model);
            var (sql, parameters, _) = builder.BuildAggregate([agg]);
            var obj = await SqlCommandHelper.ExecuteScalarAsync(ConnectionFactory, Transaction, sql, parameters, cancellationToken);
            return Convert.ToInt64(obj ?? 0L);
        }

        public bool Any()
        {
            return Count() > 0;
        }

        public bool Any(Expression<Func<T, bool>> predicate)
        {
            return Count(predicate) > 0;
        }

        public Task<bool> AnyAsync(CancellationToken cancellationToken = default)
        {
            return AnyAsyncInternal(null, cancellationToken);
        }

        public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return AnyAsyncInternal(predicate, cancellationToken);
        }

        private async Task<bool> AnyAsyncInternal(Expression<Func<T, bool>>? predicate, CancellationToken cancellationToken)
        {
            var count = await CountAsyncInternal(predicate, cancellationToken);
            return count > 0;
        }

        public TResult Sum<TResult>(Expression<Func<T, TResult>> selector)
        {
            ArgumentNullException.ThrowIfNull(selector);
            var objSel = ConvertSelectorToObject(selector);
            var agg = new AggregateDescriptor<T, object>(AggregateFunction.Sum, objSel, "Sum");
            var builder = new SqlQueryBuilder(Model);
            var (sql, parameters, _) = builder.BuildAggregate([agg]);
            var obj = SqlCommandHelper.ExecuteScalar(ConnectionFactory, Transaction, sql, parameters);
            if (obj == null || obj == DBNull.Value) return default!;
            return (TResult)Convert.ChangeType(obj, typeof(TResult));
        }

        public Task<TResult> SumAsync<TResult>(Expression<Func<T, TResult>> selector, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(selector);
            return SumAsyncInternal(selector, cancellationToken);
        }

        private async Task<TResult> SumAsyncInternal<TResult>(Expression<Func<T, TResult>> selector, CancellationToken cancellationToken)
        {
            var objSel = ConvertSelectorToObject(selector);
            var agg = new AggregateDescriptor<T, object>(AggregateFunction.Sum, objSel, "Sum");
            var builder = new SqlQueryBuilder(Model);
            var (sql, parameters, _) = builder.BuildAggregate([agg]);
            var obj = await SqlCommandHelper.ExecuteScalarAsync(ConnectionFactory, Transaction, sql, parameters, cancellationToken);
            if (obj == null || obj == DBNull.Value) return default!;
            return (TResult)Convert.ChangeType(obj, typeof(TResult));
        }

        public TResult Min<TResult>(Expression<Func<T, TResult>> selector)
        {
            ArgumentNullException.ThrowIfNull(selector);
            var objSel = ConvertSelectorToObject(selector);
            var agg = new AggregateDescriptor<T, object>(AggregateFunction.Min, objSel, "Min");
            var builder = new SqlQueryBuilder(Model);
            var (sql, parameters, _) = builder.BuildAggregate([agg]);
            var obj = SqlCommandHelper.ExecuteScalar(ConnectionFactory, Transaction, sql, parameters);
            if (obj == null || obj == DBNull.Value) return default!;
            return (TResult)Convert.ChangeType(obj, typeof(TResult));
        }

        public Task<TResult> MinAsync<TResult>(Expression<Func<T, TResult>> selector, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(selector);
            return MinAsyncInternal(selector, cancellationToken);
        }

        private async Task<TResult> MinAsyncInternal<TResult>(Expression<Func<T, TResult>> selector, CancellationToken cancellationToken)
        {
            var objSel = ConvertSelectorToObject(selector);
            var agg = new AggregateDescriptor<T, object>(AggregateFunction.Min, objSel, "Min");
            var builder = new SqlQueryBuilder(Model);
            var (sql, parameters, _) = builder.BuildAggregate([agg]);
            var obj = await SqlCommandHelper.ExecuteScalarAsync(ConnectionFactory, Transaction, sql, parameters, cancellationToken);
            if (obj == null || obj == DBNull.Value) return default!;
            return (TResult)Convert.ChangeType(obj, typeof(TResult));
        }

        public TResult Max<TResult>(Expression<Func<T, TResult>> selector)
        {
            ArgumentNullException.ThrowIfNull(selector);
            var objSel = ConvertSelectorToObject(selector);
            var agg = new AggregateDescriptor<T, object>(AggregateFunction.Max, objSel, "Max");
            var builder = new SqlQueryBuilder(Model);
            var (sql, parameters, _) = builder.BuildAggregate([agg]);
            var obj = SqlCommandHelper.ExecuteScalar(ConnectionFactory, Transaction, sql, parameters);
            if (obj == null || obj == DBNull.Value) return default!;
            return (TResult)Convert.ChangeType(obj, typeof(TResult));
        }

        public Task<TResult> MaxAsync<TResult>(Expression<Func<T, TResult>> selector, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(selector);
            return MaxAsyncInternal(selector, cancellationToken);
        }

        private async Task<TResult> MaxAsyncInternal<TResult>(Expression<Func<T, TResult>> selector, CancellationToken cancellationToken)
        {
            var objSel = ConvertSelectorToObject(selector);
            var agg = new AggregateDescriptor<T, object>(AggregateFunction.Max, objSel, "Max");
            var builder = new SqlQueryBuilder(Model);
            var (sql, parameters, _) = builder.BuildAggregate([agg]);
            var obj = await SqlCommandHelper.ExecuteScalarAsync(ConnectionFactory, Transaction, sql, parameters, cancellationToken);
            if (obj == null || obj == DBNull.Value) return default!;
            return (TResult)Convert.ChangeType(obj, typeof(TResult));
        }

        public double Average<TResult>(Expression<Func<T, TResult>> selector)
        {
            ArgumentNullException.ThrowIfNull(selector);
            var objSel = ConvertSelectorToObject(selector);
            var agg = new AggregateDescriptor<T, object>(AggregateFunction.Average, objSel, "Average");
            var builder = new SqlQueryBuilder(Model);
            var (sql, parameters, _) = builder.BuildAggregate([agg]);
            var obj = SqlCommandHelper.ExecuteScalar(ConnectionFactory, Transaction, sql, parameters);
            if (obj == null || obj == DBNull.Value) return 0.0;
            return Convert.ToDouble(obj);
        }

        public Task<double> AverageAsync<TResult>(Expression<Func<T, TResult>> selector, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(selector);
            return AverageAsyncInternal(selector, cancellationToken);
        }

        private async Task<double> AverageAsyncInternal<TResult>(Expression<Func<T, TResult>> selector, CancellationToken cancellationToken)
        {
            var objSel = ConvertSelectorToObject(selector);
            var agg = new AggregateDescriptor<T, object>(AggregateFunction.Average, objSel, "Average");
            var builder = new SqlQueryBuilder(Model);
            var (sql, parameters, _) = builder.BuildAggregate([agg]);
            var obj = await SqlCommandHelper.ExecuteScalarAsync(ConnectionFactory, Transaction, sql, parameters, cancellationToken);
            if (obj == null || obj == DBNull.Value) return 0.0;
            return Convert.ToDouble(obj);
        }

        public List<T> ToList()
        {
            var (sql, parameters, selected, sourceType, selectAll) = new SqlQueryBuilder(Model).Build();

            using var conn = ConnectionFactory();
            if (conn.State != System.Data.ConnectionState.Open) conn.Open();
            using var cmd = SqlCommandHelper.CreateCommand(conn, Transaction);
            cmd.CommandText = sql;
            SqlCommandHelper.AddParameters(cmd, parameters);
            using var reader = SqlCommandHelper.ExecuteReader(cmd);
            var result = new List<T>();

            while (reader.Read())
            {
                result.Add(DataReaderMapper.MapReaderToEntityWithAliases<T>(reader, selected, selectAll));
            }
            return result;
        }

        public async Task<List<T>> ToListAsync(CancellationToken cancellationToken = default)
        {
            var (sql, parameters, selected, sourceType, selectAll) = new SqlQueryBuilder(Model).Build();
            await using var conn = await SqlCommandHelper.CreateAndOpenConnectionAsync(ConnectionFactory, cancellationToken);
            await using var cmd = SqlCommandHelper.CreateCommand(conn, Transaction);
            cmd.CommandText = sql;
            SqlCommandHelper.AddParameters(cmd, parameters);
            var result = new List<T>();
            await using var reader = await SqlCommandHelper.ExecuteReaderAsync(cmd, cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(await DataReaderMapper.MapReaderToEntityWithAliasesAsync<T>(reader, selected, selectAll, cancellationToken));
            }
            return result;
        }

        public T[] ToArray()
        {
            return [.. ToList()];
        }

        public async Task<T[]> ToArrayAsync(CancellationToken cancellationToken = default)
        {
            var list = await ToListAsync(cancellationToken);
            return [.. list];
        }

        public T First()
        {
            var list = Page(1, 1).ToList();
            if (list.Count == 0) throw new InvalidOperationException("Sequence contains no elements");
            return list[0];
        }

        public async Task<T> FirstAsync(CancellationToken cancellationToken = default)
        {
            var q = Page(1, 1);
            var list = await q.ToListAsync(cancellationToken);
            if (list.Count == 0) throw new InvalidOperationException("Sequence contains no elements");
            return list[0];
        }

        public T? FirstOrDefault()
        {
            var list = Page(1, 1).ToList();
            return list.Count == 0 ? default : list[0];
        }

        public async Task<T?> FirstOrDefaultAsync(CancellationToken cancellationToken = default)
        {
            var q = Page(1, 1);
            var list = await q.ToListAsync(cancellationToken);
            return list.Count == 0 ? default : list[0];
        }

        public T Single()
        {
            var list = ToList();
            if (list.Count == 0) throw new InvalidOperationException("Sequence contains no elements");
            if (list.Count > 1) throw new InvalidOperationException("Sequence contains more than one element");
            return list[0];
        }

        public async Task<T> SingleAsync(CancellationToken cancellationToken = default)
        {
            var list = await ToListAsync(cancellationToken);
            if (list.Count == 0) throw new InvalidOperationException("Sequence contains no elements");
            if (list.Count > 1) throw new InvalidOperationException("Sequence contains more than one element");
            return list[0];
        }

        public T? SingleOrDefault()
        {
            var list = ToList();
            if (list.Count > 1) throw new InvalidOperationException("Sequence contains more than one element");
            return list.Count == 0 ? default : list[0];
        }

        public async Task<T?> SingleOrDefaultAsync(CancellationToken cancellationToken = default)
        {
            var list = await ToListAsync(cancellationToken);
            if (list.Count > 1) throw new InvalidOperationException("Sequence contains more than one element");
            return list.Count == 0 ? default : list[0];
        }

        public PagedResult<T> ToPagedResult()
        {
            var pagination = Model.Pagination ?? new PaginationDescriptor();
            int page = pagination.Page ?? (pagination.Skip.HasValue && pagination.PageSize.HasValue ? (pagination.Skip.Value / pagination.PageSize.Value) + 1 : 1);
            int pageSize = pagination.PageSize ?? pagination.Take ?? 0;

            var items = Page(page, pageSize).ToList();

            var countModel = new QueryModel<T>(Model.Filters, Model.Projection, Model.Group, Model.Aggregates, Model.Joins, [], null, Model.IsDistinct);
            var countQuery = new SqlQueryBuilder(countModel);
            var (countSql, countParams, _, _, _) = countQuery.Build();

            using var conn = ConnectionFactory();
            if (conn.State != System.Data.ConnectionState.Open) conn.Open();
            using var cmd = SqlCommandHelper.CreateCommand(conn, Transaction);
            var countCommand = "SELECT COUNT(1) FROM (" + countSql + ") AS [__countSub]";
            cmd.CommandText = countCommand;
            SqlCommandHelper.AddParameters(cmd, countParams);
            var total = Convert.ToInt32(SqlCommandHelper.ExecuteScalar(ConnectionFactory, Transaction, countCommand, countParams));

            return new PagedResult<T> { Items = items, TotalCount = total, PageNumber = page, PageSize = pageSize };
        }

        public async Task<PagedResult<T>> ToPagedResultAsync(CancellationToken cancellationToken = default)
        {
            var pagination = Model.Pagination ?? new PaginationDescriptor();
            int page = pagination.Page ?? (pagination.Skip.HasValue && pagination.PageSize.HasValue ? (pagination.Skip.Value / pagination.PageSize.Value) + 1 : 1);
            int pageSize = pagination.PageSize ?? pagination.Take ?? 0;

            var items = await Page(page, pageSize).ToListAsync(cancellationToken);

            var countModel = new QueryModel<T>(Model.Filters, Model.Projection, Model.Group, Model.Aggregates, Model.Joins, [], null, Model.IsDistinct);
            var countQuery = new SqlQueryBuilder(countModel);
            var (countSql, countParams, _, _, _) = countQuery.Build();

            var countCommand2 = "SELECT COUNT(1) FROM (" + countSql + ") AS [__countSub]";
            var totalObj = await SqlCommandHelper.ExecuteScalarAsync(ConnectionFactory, Transaction, countCommand2, countParams, cancellationToken);
            var total = Convert.ToInt32(totalObj);

            return new PagedResult<T> { Items = items, TotalCount = total, PageNumber = page, PageSize = pageSize };
        }
    }
}