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

        private static List<OrderDescriptor<T, object>> AppendOrder(List<OrderDescriptor<T, object>> existing, OrderDescriptor<T, object> newOrder)
        {
            List<OrderDescriptor<T, object>> list = existing?.ToList() ?? [];
            OrderDescriptor<T, object> prioritized = new(newOrder.KeySelector, newOrder.Ascending, list.Count + 1);
            list.Add(prioritized);
            return list;
        }

        private static Expression<Func<T, object>> ConvertToObjectSelector<TKey>(Expression<Func<T, TKey>> selector)
        {
            ArgumentNullException.ThrowIfNull(selector);
            ParameterExpression param = selector.Parameters.Single();
            UnaryExpression body = Expression.Convert(selector.Body, typeof(object));
            return Expression.Lambda<Func<T, object>>(body, param);
        }

        public IQuery<T> ApplyFilters(IEnumerable<Filter> filters)
        {
            ArgumentNullException.ThrowIfNull(filters);

            Query<T> query = this;
            foreach (Filter filter in filters)
            {
                Expression<Func<T, bool>>? predicate = SqlExpressionBuilder.BuildFilterPredicate<T>(filter);
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

            Query<T> query = this;
            bool isFirst = true;

            foreach (SortOption sortOption in sortOptions)
            {
                Expression<Func<T, object>>? keySelector = SqlExpressionBuilder.BuildSortExpression<T>(sortOption);
                if (keySelector != null)
                {
                    bool ascending = sortOption.Direction == SortDirection.Asc;

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

        public IQuery<T> Where(Expression<Func<T, bool>> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            List<FilterDescriptor<T>> filters = Model.Filters?.ToList() ?? [];
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

        public IQuery<TResult> Select<TResult>(Expression<Func<T, TResult>> selector)
        {
            ArgumentNullException.ThrowIfNull(selector);

            QueryModel<TResult> newModel = new(
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

            QueryModel<TResult> newModel = new(
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

            ParameterExpression outerParam = Expression.Parameter(typeof(T), "outer");
            ParameterExpression innerParam = Expression.Parameter(typeof(TInner), "inner");
            Type tupleType = typeof(ValueTuple<,>).MakeGenericType(typeof(T), typeof(TInner));
            ConstructorInfo ctor = tupleType.GetConstructor([typeof(T), typeof(TInner)])!;
            NewExpression body = Expression.New(ctor, outerParam, innerParam);
            LambdaExpression resultSelector = Expression.Lambda(body, outerParam, innerParam);

            return Join<TInner, (T Outer, TInner Inner)>(onPredicate, (Expression<Func<T, TInner, (T, TInner)>>)resultSelector, joinType);
        }

        public IQuery<TResult> Join<TInner, TResult>(Expression<Func<T, TInner, bool>> onPredicate, Expression<Func<T, TInner, TResult>> resultSelector, JoinType joinType = JoinType.Inner)
        {
            ArgumentNullException.ThrowIfNull(onPredicate);
            ArgumentNullException.ThrowIfNull(resultSelector);
            ParameterExpression resultOuterParam = Expression.Parameter(typeof(TResult), "outerRes");
            ParameterExpression innerObjParam = Expression.Parameter(typeof(object), "innerObj");

            List<JoinDescriptor<TResult, object, object>> accumulatedJoins = [];
            if (Model.Joins != null && Model.Joins.Count > 0)
            {
                foreach (var j in Model.Joins)
                {
                    var prevOnInvoke = Expression.Invoke(j.On, Expression.Default(typeof(T)), innerObjParam);
                    var prevOnWrapped = Expression.Lambda<Func<TResult, object, bool>>(prevOnInvoke, resultOuterParam, innerObjParam);

                    var prevResInvoke = Expression.Invoke(j.ResultSelector, Expression.Default(typeof(T)), innerObjParam);
                    var prevResWrapped = Expression.Lambda<Func<TResult, object, object>>(Expression.Convert(prevResInvoke, typeof(object)), resultOuterParam, innerObjParam);

                    accumulatedJoins.Add(new JoinDescriptor<TResult, object, object>(prevOnWrapped, prevResWrapped, j.JoinType));
                }
            }

            var onInvoke = Expression.Invoke(onPredicate, Expression.Default(typeof(T)), Expression.Convert(innerObjParam, typeof(TInner)));
            var onWrapper = Expression.Lambda<Func<TResult, object, bool>>(onInvoke, resultOuterParam, innerObjParam);

            var resultInvoke = Expression.Invoke(resultSelector, Expression.Default(typeof(T)), Expression.Convert(innerObjParam, typeof(TInner)));
            var resultWrapper = Expression.Lambda<Func<TResult, object, object>>(Expression.Convert(resultInvoke, typeof(object)), resultOuterParam, innerObjParam);

            accumulatedJoins.Add(new JoinDescriptor<TResult, object, object>(onWrapper, resultWrapper, joinType));

            QueryModel<TResult> newModel = new(
                Filters: [],
                Projection: null,
                Group: null,
                Aggregates: [],
                Joins: accumulatedJoins,
                Orders: [],
                Pagination: null,
                IsDistinct: false
            );

            return new Query<TResult>(ConnectionFactory, Transaction, newModel);
        }

        public IQuery<TResult> GroupBy<TKey, TResult>(Expression<Func<T, TKey>> keySelector, Expression<Func<IGrouping<TKey, T>, TResult>> resultSelector)
        {
            ArgumentNullException.ThrowIfNull(keySelector);
            ArgumentNullException.ThrowIfNull(resultSelector);

            GroupDescriptor<T, object> groupDesc = new(keySelector);
            ProjectionDescriptor<TResult, object> projection = new(resultSelector);

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
            ProjectionDescriptor<TResult, object> projection = new(predicate);

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
            Expression<Func<T, object>> converted = ConvertToObjectSelector(keySelector);
            OrderDescriptor<T, object> order = new(converted, ascending, 0);
            List<OrderDescriptor<T, object>> orders = AppendOrder(Model.Orders, order);
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
            PaginationDescriptor? pagination = Model.Pagination ?? new PaginationDescriptor();
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
            PaginationDescriptor? pagination = Model.Pagination ?? new PaginationDescriptor();
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
            private List<string>? _groupByExplicit;

            private static bool TryGetOriginalJoinLambda(LambdaExpression onLambda, out LambdaExpression? original)
            {
                original = null;
                if (onLambda.Body is InvocationExpression inv && inv.Expression is LambdaExpression le)
                {
                    original = le;
                    return true;
                }
                if (onLambda.Parameters.Count == 2)
                {
                    original = onLambda;
                    return true;
                }
                return false;
            }

            private static Type UnwrapGroupingIfNeeded(Type type)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IGrouping<,>))
                {
                    return type.GetGenericArguments()[1];
                }
                return type;
            }

            private Type ResolveSourceType()
            {
                if (model.Joins != null && model.Joins.Count > 0)
                {
                    var first = model.Joins[0];
                    if (first.On is LambdaExpression onLe && TryGetOriginalJoinLambda(onLe, out var orig) && orig != null && orig.Parameters.Count >= 2)
                    {
                        return UnwrapGroupingIfNeeded(orig.Parameters[0].Type);
                    }
                }

                if (model.Projection != null && model.Projection.Selector is LambdaExpression p && p.Parameters.Count > 0)
                {
                    return UnwrapGroupingIfNeeded(p.Parameters[0].Type);
                }

                return typeof(T);
            }

            public (string SelectSql, IReadOnlyList<(string Column, string Alias)> Selected, bool SelectAll, Type SourceType) BuildSelectClause()
            {
                Type sourceType = ResolveSourceType();

                string tableName = EntityHelper.GetTableName(sourceType);

                List<(string Column, string Alias)> selected = [];
                bool selectAll = false;
                if (model.Projection?.Selector is not LambdaExpression projectionLambda)
                {
                    foreach (PropertyInfo prop in sourceType.GetProperties())
                    {
                        string col = EntityHelper.GetColumnName(prop);
                        selected.Add((col, prop.Name));
                    }
                    selectAll = true;
                }
                else
                {
                    bool isGrouping = projectionLambda.Parameters.Count > 0 && projectionLambda.Parameters[0].Type.IsGenericType && projectionLambda.Parameters[0].Type.GetGenericTypeDefinition() == typeof(IGrouping<,>);
                    Expression body = projectionLambda.Body;
                    if (isGrouping && body is MemberInitExpression mi)
                    {
                        List<string> gbCols = [];
                        List<string> fragmentParts = [];
                        foreach (MemberAssignment assign in mi.Bindings.Cast<MemberAssignment>())
                        {
                            string alias = assign.Member.Name;
                            if (assign.Expression is MethodCallExpression mce && mce.Method.Name == nameof(Enumerable.Count))
                            {
                                fragmentParts.Add($"COUNT(1) AS [{alias}]");
                                selected.Add((alias, alias));
                                continue;
                            }

                            if (assign.Expression is MemberExpression mex && mex.Member is PropertyInfo pinfo)
                            {
                                string col = EntityHelper.GetColumnName(pinfo);
                                string qualified = SqlCommandHelper.FormatQualifiedTableName(tableName);
                                fragmentParts.Add($"{qualified}.[{col}] AS [{alias}]");
                                selected.Add((col, alias));
                                gbCols.Add(col);
                                continue;
                            }

                            selectAll = true; // fallback if unknown pattern
                            break;
                        }

                        if (!selectAll)
                        {
                            _groupByExplicit = [.. gbCols.Distinct()];
                            System.Text.StringBuilder sbSel = new();
                            _ = sbSel.Append("SELECT ");
                            if (model.IsDistinct) _ = sbSel.Append("DISTINCT ");
                            _ = sbSel.Append(string.Join(", ", fragmentParts));
                            return (sbSel.ToString(), selected, false, sourceType);
                        }
                    }
                    if (body is MemberInitExpression mi2)
                    {
                        foreach (MemberAssignment assign in mi2.Bindings.Cast<MemberAssignment>())
                        {
                            if (assign.Expression is MemberExpression mex && mex.Member is PropertyInfo pinfo)
                            {
                                string col = EntityHelper.GetColumnName(pinfo);
                                selected.Add((col, assign.Member.Name));
                            }
                            else { selectAll = true; break; }
                        }
                    }
                    else if (body is NewExpression ne)
                    {
                        for (int i = 0; i < ne.Arguments.Count; i++)
                        {
                            Expression arg = ne.Arguments[i];
                            MemberInfo? member = ne.Members != null && i < ne.Members.Count ? ne.Members[i] : null;
                            if (arg is MemberExpression mex && mex.Member is PropertyInfo pinfo && member != null)
                            {
                                string col = EntityHelper.GetColumnName(pinfo);
                                selected.Add((col, member.Name));
                            }
                            else { selectAll = true; break; }
                        }
                    }
                    else if (body is MemberExpression mb && mb.Member is PropertyInfo pinfo)
                    {
                        string col = EntityHelper.GetColumnName(pinfo);
                        selected.Add((col, pinfo.Name));
                    }
                    else if (body is ParameterExpression)
                    {
                        foreach (PropertyInfo prop in sourceType.GetProperties())
                        {
                            string col = EntityHelper.GetColumnName(prop);
                            selected.Add((col, prop.Name));
                        }
                        selectAll = true;
                    }
                    else
                    {
                        foreach (PropertyInfo prop in sourceType.GetProperties())
                        {
                            string col = EntityHelper.GetColumnName(prop);
                            selected.Add((col, prop.Name));
                        }
                        selectAll = true;
                    }
                }

                System.Text.StringBuilder sb = new();
                _ = sb.Append("SELECT ");
                if (model.IsDistinct)
                {
                    _ = sb.Append("DISTINCT ");
                }

                _ = selected.Count == 0 ? sb.Append('*') : sb.Append(SqlCommandHelper.BuildColumnListWithAliases(selected, tableName));

                return (sb.ToString(), selected, selectAll, sourceType);
            }

            public (string Sql, IReadOnlyList<KeyValuePair<string, object?>> Parameters, IReadOnlyList<(string Column, string Alias)> SelectedColumns, Type SourceType, bool SelectAll) Build()
            {
                (string selectSql, IReadOnlyList<(string Column, string Alias)> selected, bool selectAll, Type sourceType) = BuildSelectClause();
                string tableName = EntityHelper.GetTableName(sourceType);
                string qualifiedTable = SqlCommandHelper.FormatQualifiedTableName(tableName);

                System.Text.StringBuilder sb = new();
                _ = sb.Append(selectSql);
                _ = sb.Append(" FROM ");
                _ = sb.Append(qualifiedTable);

                string joins = BuildJoins(tableName);
                if (!string.IsNullOrEmpty(joins)) { _ = sb.Append(' ').Append(joins); }

                (string whereSql, _) = BuildWhereClause(tableName);
                if (!string.IsNullOrEmpty(whereSql)) { _ = sb.Append(' ').Append(whereSql); }

                string group = BuildGroupBy(tableName);
                if (!string.IsNullOrEmpty(group)) { _ = sb.Append(' ').Append(group); }

                string order = BuildOrderBy(tableName);
                if (!string.IsNullOrEmpty(order))
                {
                    _ = sb.Append(' ').Append(order);
                }
                else if (model.Pagination != null)
                {
                    PropertyInfo? firstProp = sourceType.GetProperties().FirstOrDefault();
                    if (firstProp != null)
                    {
                        string col = EntityHelper.GetColumnName(firstProp);
                        string qualified = SqlCommandHelper.FormatQualifiedTableName(tableName);
                        _ = sb.Append(" ORDER BY ").Append($"{qualified}.[{col}]");
                    }
                }

                string pagination = BuildPagination();
                if (!string.IsNullOrEmpty(pagination)) { _ = sb.Append(' ').Append(pagination); }

                return (sb.ToString(), _parameters, selected, sourceType, selectAll);
            }

            public (string Sql, IReadOnlyList<KeyValuePair<string, object?>> Parameters) BuildWhereClause(string tableName)
            {
                if (model.Filters == null || model.Filters.Count == 0)
                {
                    return (string.Empty, []);
                }

                List<string> parts = [];
                foreach (FilterDescriptor<T> f in model.Filters)
                {
                    if (f.Predicate is LambdaExpression le)
                    {
                        Func<ParameterExpression, string> resolver = new(p => tableName);
                        parts.Add(SqlExpressionBuilder.TranslateExpression(le.Body, resolver, _parameters, ref _paramCounter));
                    }
                }
                if (parts.Count == 0)
                {
                    return (string.Empty, []);
                }

                return ("WHERE " + string.Join(" AND ", parts), _parameters.ToList());
            }

            public string BuildOrderBy(string tableName)
            {
                List<OrderDescriptor<T, object>>? orders = model.Orders?.OrderBy(o => o.Priority).ToList();
                if (orders == null || orders.Count == 0)
                {
                    return string.Empty;
                }

                List<string> parts = [];
                foreach (OrderDescriptor<T, object>? o in orders)
                {
                    if (o.KeySelector is LambdaExpression lle)
                    {
                        MemberExpression? member = SqlExpressionBuilder.ExtractMember(lle.Body);
                        if (member != null && member.Member is PropertyInfo pi)
                        {
                            string col = EntityHelper.GetColumnName(pi);
                            string qualified = SqlCommandHelper.FormatQualifiedTableName(tableName);
                            parts.Add($"{qualified}.[{col}] {(o.Ascending ? "ASC" : "DESC")}");
                        }
                    }
                }
                return parts.Count == 0 ? string.Empty : "ORDER BY " + string.Join(", ", parts);
            }

            public string BuildPagination()
            {
                if (model.Pagination == null)
                {
                    return string.Empty;
                }

                PaginationDescriptor pagination = model.Pagination;
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

                return fetch > 0 ? $"OFFSET {offset} ROWS FETCH NEXT {fetch} ROWS ONLY" : string.Empty;
            }

            public string BuildJoins(string outerTableName)
            {
                if (model.Joins == null || model.Joins.Count == 0)
                {
                    return string.Empty;
                }

                List<string> parts = [];
                foreach (JoinDescriptor<T, object, object> join in model.Joins)
                {
                    if (join.On is LambdaExpression onLambda)
                    {
                        Expression body = onLambda.Body;
                        LambdaExpression? origOn = null;
                        if (body is InvocationExpression inv)
                        {
                            origOn = inv.Expression as LambdaExpression;
                        }
                        if (origOn == null && onLambda.Parameters.Count == 2)
                        {
                            origOn = onLambda;
                        }

                        if (origOn != null && origOn.Parameters.Count >= 2)
                        {
                            ParameterExpression innerParam = origOn.Parameters[1];
                            Type innerType = innerParam.Type;
                            string innerTableName = EntityHelper.GetTableName(innerType);
                            string qualifiedInnerTable = SqlCommandHelper.FormatQualifiedTableName(innerTableName);

                            string joinKeyword = join.JoinType switch
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
                                Func<ParameterExpression, string> resolver = new(p =>
                                {
                                    return p == origOn.Parameters[0] ? outerTableName : p == origOn.Parameters[1] ? innerTableName : outerTableName;
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
                if (model.Group == null)
                {
                    return string.Empty;
                }

                if (_groupByExplicit != null && _groupByExplicit.Count > 0)
                {
                    string qualified = SqlCommandHelper.FormatQualifiedTableName(tableName);
                    return "GROUP BY " + string.Join(", ", _groupByExplicit.Select(c => $"{qualified}.[{c}]"));
                }

                if (model.Group.Selector is LambdaExpression gLE && gLE.Body is MemberExpression gm && gm.Member is PropertyInfo gpi)
                {
                    string gcol = EntityHelper.GetColumnName(gpi);
                    string qualified = SqlCommandHelper.FormatQualifiedTableName(tableName);
                    return "GROUP BY " + $"{qualified}.[{gcol}]";
                }
                return string.Empty;
            }

            public (string SqlFragment, IReadOnlyList<string> Names) BuildAggregates(IEnumerable<AggregateDescriptor<T, object>> aggregates)
            {
                ArgumentNullException.ThrowIfNull(aggregates);
                List<AggregateDescriptor<T, object>> aggs = [.. aggregates];
                if (aggs.Count == 0)
                {
                    return (string.Empty, Array.Empty<string>());
                }

                Type sourceType = typeof(T);
                if (model.Projection != null)
                {
                    if (model.Projection.Selector is LambdaExpression projectionLambda && projectionLambda.Parameters.Count > 0)
                    {
                        sourceType = projectionLambda.Parameters[0].Type;
                    }
                }

                string tableName = EntityHelper.GetTableName(sourceType);
                string qualifiedTable = SqlCommandHelper.FormatQualifiedTableName(tableName);

                List<string> parts = [];
                List<string> names = [];
                foreach (AggregateDescriptor<T, object>? agg in aggs)
                {
                    string alias = agg.Name ?? agg.Function.ToString();
                    if (agg.Function == AggregateFunction.Count)
                    {
                        if (agg.Selector is LambdaExpression sel && SqlExpressionBuilder.ExtractMember(sel.Body) is MemberExpression m && m.Member is PropertyInfo pi)
                        {
                            string col = EntityHelper.GetColumnName(pi);
                            parts.Add($"COUNT({qualifiedTable}.[{col}]) AS [{alias}]");
                        }
                        else
                        {
                            parts.Add($"COUNT(1) AS [{alias}]");
                        }
                    }
                    else
                    {
                        if (agg.Selector is LambdaExpression sel && SqlExpressionBuilder.ExtractMember(sel.Body) is MemberExpression m && m.Member is PropertyInfo pi)
                        {
                            string col = EntityHelper.GetColumnName(pi);
                            string func = agg.Function switch
                            {
                                AggregateFunction.Sum => "SUM",
                                AggregateFunction.Min => "MIN",
                                AggregateFunction.Max => "MAX",
                                AggregateFunction.Average => "AVG",
                                _ => throw new NotSupportedException($"Aggregate function {agg.Function} is not supported")
                            };
                            parts.Add($"{func}({qualifiedTable}.[{col}]) AS [{alias}]");
                        }
                        else
                        {
                            throw new NotSupportedException("Aggregate selector must be a simple member access for SUM/MIN/MAX/AVG");
                        }
                    }
                    names.Add(alias);
                }
                return (string.Join(", ", parts), names);
            }

            public (string Sql, IReadOnlyList<KeyValuePair<string, object?>> Parameters, IReadOnlyList<string> AggregateNames) BuildAggregate(IEnumerable<AggregateDescriptor<T, object>> aggregates)
            {
                ArgumentNullException.ThrowIfNull(aggregates);
                Type sourceType = ResolveSourceType();
                string tableName = EntityHelper.GetTableName(sourceType);
                string qualifiedTable = SqlCommandHelper.FormatQualifiedTableName(tableName);

                (string fragment, IReadOnlyList<string> names) = BuildAggregates(aggregates);
                System.Text.StringBuilder sb = new();
                _ = sb.Append("SELECT ");
                _ = sb.Append(fragment);
                _ = sb.Append(" FROM ");
                _ = sb.Append(qualifiedTable);

                string joins = BuildJoins(tableName);
                if (!string.IsNullOrEmpty(joins))
                {
                    _ = sb.Append(' ').Append(joins);
                }

                (string whereSql, _) = BuildWhereClause(tableName);
                if (!string.IsNullOrEmpty(whereSql))
                {
                    _ = sb.Append(' ').Append(whereSql);
                }

                string group = BuildGroupBy(tableName);
                if (!string.IsNullOrEmpty(group))
                {
                    _ = sb.Append(' ').Append(group);
                }

                return (sb.ToString(), _parameters, names);
            }
        }

        public AggregateResult Aggregate(params AggregateDescriptor<T, object>[] aggregates)
        {
            ArgumentNullException.ThrowIfNull(aggregates);
            SqlQueryBuilder builder = new(Model);
            (string sql, IReadOnlyList<KeyValuePair<string, object?>> parameters, IReadOnlyList<string> names) = builder.BuildAggregate(aggregates);

            Dictionary<string, object?> values = [];
            using DbConnection conn = ConnectionFactory();
            if (conn.State != System.Data.ConnectionState.Open)
            {
                conn.Open();
            }

            using DbCommand cmd = SqlCommandHelper.CreateCommand(conn, Transaction);
            cmd.CommandText = sql;
            SqlCommandHelper.AddParameters(cmd, parameters);
            using DbDataReader reader = SqlCommandHelper.ExecuteReader(cmd);
            if (reader.Read())
            {
                for (int i = 0; i < names.Count; i++)
                {
                    object? val = reader.IsDBNull(i) ? null : reader.GetValue(i);
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
            SqlQueryBuilder builder = new(Model);
            (string sql, IReadOnlyList<KeyValuePair<string, object?>> parameters, IReadOnlyList<string> names) = builder.BuildAggregate(aggregates);

            await using DbConnection conn = await SqlCommandHelper.CreateAndOpenConnectionAsync(ConnectionFactory, cancellationToken);
            await using DbCommand cmd = SqlCommandHelper.CreateCommand(conn, Transaction);
            cmd.CommandText = sql;
            SqlCommandHelper.AddParameters(cmd, parameters);
            await using DbDataReader reader = await SqlCommandHelper.ExecuteReaderAsync(cmd, cancellationToken);
            Dictionary<string, object?> values = [];
            if (await reader.ReadAsync(cancellationToken))
            {
                for (int i = 0; i < names.Count; i++)
                {
                    object? val = await reader.IsDBNullAsync(i, cancellationToken) ? null : reader.GetValue(i);
                    values[names[i]] = val;
                }
            }
            return CreateAggregateResult(values);
        }

        private static AggregateResult CreateAggregateResult(Dictionary<string, object?> values)
        {
            ConstructorInfo ctor = typeof(AggregateResult).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .FirstOrDefault(c => c.GetParameters().Length == 1 && c.GetParameters()[0].ParameterType == typeof(Dictionary<string, object?>)) ?? throw new InvalidOperationException("Could not locate AggregateResult constructor via reflection.");
            return (AggregateResult)ctor.Invoke([values ?? []]);
        }

        private static Expression<Func<T, object>> ConvertSelectorToObject<TResult>(Expression<Func<T, TResult>> selector)
        {
            ArgumentNullException.ThrowIfNull(selector);
            ParameterExpression param = selector.Parameters.Single();
            UnaryExpression body = Expression.Convert(selector.Body, typeof(object));
            return Expression.Lambda<Func<T, object>>(body, param);
        }

        public long Count()
        {
            AggregateDescriptor<T, object> agg = new(AggregateFunction.Count, t => 1, "Count");
            SqlQueryBuilder builder = new(Model);
            (string sql, IReadOnlyList<KeyValuePair<string, object?>> parameters, IReadOnlyList<string> _) = builder.BuildAggregate([agg]);
            object? obj = SqlCommandHelper.ExecuteScalar(ConnectionFactory, Transaction, sql, parameters);
            return Convert.ToInt64(obj ?? 0L);
        }

        public long Count(Expression<Func<T, bool>> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            List<FilterDescriptor<T>> filters = Model.Filters?.ToList() ?? [];
            filters.Add(new FilterDescriptor<T>(predicate));
            QueryModel<T> tempModel = new(filters, Model.Projection, Model.Group, Model.Aggregates, Model.Joins, Model.Orders, Model.Pagination, Model.IsDistinct);
            AggregateDescriptor<T, object> agg = new(AggregateFunction.Count, t => 1, "Count");
            SqlQueryBuilder builder = new(tempModel);
            (string sql, IReadOnlyList<KeyValuePair<string, object?>> parameters, IReadOnlyList<string> _) = builder.BuildAggregate([agg]);
            object? obj = SqlCommandHelper.ExecuteScalar(ConnectionFactory, Transaction, sql, parameters);
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
                List<FilterDescriptor<T>> filters = Model.Filters?.ToList() ?? [];
                filters.Add(new FilterDescriptor<T>(predicate));
                model = new QueryModel<T>(filters, Model.Projection, Model.Group, Model.Aggregates, Model.Joins, Model.Orders, Model.Pagination, Model.IsDistinct);
            }
            AggregateDescriptor<T, object> agg = new(AggregateFunction.Count, t => 1, "Count");
            SqlQueryBuilder builder = new(model);
            (string sql, IReadOnlyList<KeyValuePair<string, object?>> parameters, IReadOnlyList<string> _) = builder.BuildAggregate([agg]);
            object? obj = await SqlCommandHelper.ExecuteScalarAsync(ConnectionFactory, Transaction, sql, parameters, cancellationToken);
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
            long count = await CountAsyncInternal(predicate, cancellationToken);
            return count > 0;
        }

        public TResult Sum<TResult>(Expression<Func<T, TResult>> selector)
        {
            ArgumentNullException.ThrowIfNull(selector);
            Expression<Func<T, object>> objSel = ConvertSelectorToObject(selector);
            AggregateDescriptor<T, object> agg = new(AggregateFunction.Sum, objSel, "Sum");
            SqlQueryBuilder builder = new(Model);
            (string sql, IReadOnlyList<KeyValuePair<string, object?>> parameters, IReadOnlyList<string> _) = builder.BuildAggregate([agg]);
            object? obj = SqlCommandHelper.ExecuteScalar(ConnectionFactory, Transaction, sql, parameters);
            return obj == null || obj == DBNull.Value ? default! : (TResult)Convert.ChangeType(obj, typeof(TResult));
        }

        public Task<TResult> SumAsync<TResult>(Expression<Func<T, TResult>> selector, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(selector);
            return SumAsyncInternal(selector, cancellationToken);
        }

        private async Task<TResult> SumAsyncInternal<TResult>(Expression<Func<T, TResult>> selector, CancellationToken cancellationToken)
        {
            Expression<Func<T, object>> objSel = ConvertSelectorToObject(selector);
            AggregateDescriptor<T, object> agg = new(AggregateFunction.Sum, objSel, "Sum");
            SqlQueryBuilder builder = new(Model);
            (string sql, IReadOnlyList<KeyValuePair<string, object?>> parameters, IReadOnlyList<string> _) = builder.BuildAggregate([agg]);
            object? obj = await SqlCommandHelper.ExecuteScalarAsync(ConnectionFactory, Transaction, sql, parameters, cancellationToken);
            return obj == null || obj == DBNull.Value ? default! : (TResult)Convert.ChangeType(obj, typeof(TResult));
        }

        public TResult Min<TResult>(Expression<Func<T, TResult>> selector)
        {
            ArgumentNullException.ThrowIfNull(selector);
            Expression<Func<T, object>> objSel = ConvertSelectorToObject(selector);
            AggregateDescriptor<T, object> agg = new(AggregateFunction.Min, objSel, "Min");
            SqlQueryBuilder builder = new(Model);
            (string sql, IReadOnlyList<KeyValuePair<string, object?>> parameters, IReadOnlyList<string> _) = builder.BuildAggregate([agg]);
            object? obj = SqlCommandHelper.ExecuteScalar(ConnectionFactory, Transaction, sql, parameters);
            return obj == null || obj == DBNull.Value ? default! : (TResult)Convert.ChangeType(obj, typeof(TResult));
        }

        public Task<TResult> MinAsync<TResult>(Expression<Func<T, TResult>> selector, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(selector);
            return MinAsyncInternal(selector, cancellationToken);
        }

        private async Task<TResult> MinAsyncInternal<TResult>(Expression<Func<T, TResult>> selector, CancellationToken cancellationToken)
        {
            Expression<Func<T, object>> objSel = ConvertSelectorToObject(selector);
            AggregateDescriptor<T, object> agg = new(AggregateFunction.Min, objSel, "Min");
            SqlQueryBuilder builder = new(Model);
            (string sql, IReadOnlyList<KeyValuePair<string, object?>> parameters, IReadOnlyList<string> _) = builder.BuildAggregate([agg]);
            object? obj = await SqlCommandHelper.ExecuteScalarAsync(ConnectionFactory, Transaction, sql, parameters, cancellationToken);
            return obj == null || obj == DBNull.Value ? default! : (TResult)Convert.ChangeType(obj, typeof(TResult));
        }

        public TResult Max<TResult>(Expression<Func<T, TResult>> selector)
        {
            ArgumentNullException.ThrowIfNull(selector);
            Expression<Func<T, object>> objSel = ConvertSelectorToObject(selector);
            AggregateDescriptor<T, object> agg = new(AggregateFunction.Max, objSel, "Max");
            SqlQueryBuilder builder = new(Model);
            (string sql, IReadOnlyList<KeyValuePair<string, object?>> parameters, IReadOnlyList<string> _) = builder.BuildAggregate([agg]);
            object? obj = SqlCommandHelper.ExecuteScalar(ConnectionFactory, Transaction, sql, parameters);
            return obj == null || obj == DBNull.Value ? default! : (TResult)Convert.ChangeType(obj, typeof(TResult));
        }

        public Task<TResult> MaxAsync<TResult>(Expression<Func<T, TResult>> selector, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(selector);
            return MaxAsyncInternal(selector, cancellationToken);
        }

        private async Task<TResult> MaxAsyncInternal<TResult>(Expression<Func<T, TResult>> selector, CancellationToken cancellationToken)
        {
            Expression<Func<T, object>> objSel = ConvertSelectorToObject(selector);
            AggregateDescriptor<T, object> agg = new(AggregateFunction.Max, objSel, "Max");
            SqlQueryBuilder builder = new(Model);
            (string sql, IReadOnlyList<KeyValuePair<string, object?>> parameters, IReadOnlyList<string> _) = builder.BuildAggregate([agg]);
            object? obj = await SqlCommandHelper.ExecuteScalarAsync(ConnectionFactory, Transaction, sql, parameters, cancellationToken);
            return obj == null || obj == DBNull.Value ? default! : (TResult)Convert.ChangeType(obj, typeof(TResult));
        }

        public double Average<TResult>(Expression<Func<T, TResult>> selector)
        {
            ArgumentNullException.ThrowIfNull(selector);
            Expression<Func<T, object>> objSel = ConvertSelectorToObject(selector);
            AggregateDescriptor<T, object> agg = new(AggregateFunction.Average, objSel, "Average");
            SqlQueryBuilder builder = new(Model);
            (string sql, IReadOnlyList<KeyValuePair<string, object?>> parameters, IReadOnlyList<string> _) = builder.BuildAggregate([agg]);
            object? obj = SqlCommandHelper.ExecuteScalar(ConnectionFactory, Transaction, sql, parameters);
            return obj == null || obj == DBNull.Value ? 0.0 : Convert.ToDouble(obj);
        }

        public Task<double> AverageAsync<TResult>(Expression<Func<T, TResult>> selector, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(selector);
            return AverageAsyncInternal(selector, cancellationToken);
        }

        private async Task<double> AverageAsyncInternal<TResult>(Expression<Func<T, TResult>> selector, CancellationToken cancellationToken)
        {
            Expression<Func<T, object>> objSel = ConvertSelectorToObject(selector);
            AggregateDescriptor<T, object> agg = new(AggregateFunction.Average, objSel, "Average");
            SqlQueryBuilder builder = new(Model);
            (string sql, IReadOnlyList<KeyValuePair<string, object?>> parameters, IReadOnlyList<string> _) = builder.BuildAggregate([agg]);
            object? obj = await SqlCommandHelper.ExecuteScalarAsync(ConnectionFactory, Transaction, sql, parameters, cancellationToken);
            return obj == null || obj == DBNull.Value ? 0.0 : Convert.ToDouble(obj);
        }

        public List<T> ToList()
        {
            (string sql, IReadOnlyList<KeyValuePair<string, object?>> parameters, IReadOnlyList<(string Column, string Alias)> selected, Type sourceType, bool selectAll) = new SqlQueryBuilder(Model).Build();

            using DbConnection conn = ConnectionFactory();
            if (conn.State != System.Data.ConnectionState.Open)
            {
                conn.Open();
            }

            using DbCommand cmd = SqlCommandHelper.CreateCommand(conn, Transaction);
            cmd.CommandText = sql;
            SqlCommandHelper.AddParameters(cmd, parameters);
            using DbDataReader reader = SqlCommandHelper.ExecuteReader(cmd);
            List<T> result = [];

            while (reader.Read())
            {
                result.Add(DataReaderMapper.MapReaderToEntityWithAliases<T>(reader, selected, selectAll));
            }
            return result;
        }

        public async Task<List<T>> ToListAsync(CancellationToken cancellationToken = default)
        {
            (string sql, IReadOnlyList<KeyValuePair<string, object?>> parameters, IReadOnlyList<(string Column, string Alias)> selected, Type sourceType, bool selectAll) = new SqlQueryBuilder(Model).Build();
            await using DbConnection conn = await SqlCommandHelper.CreateAndOpenConnectionAsync(ConnectionFactory, cancellationToken);
            await using DbCommand cmd = SqlCommandHelper.CreateCommand(conn, Transaction);
            cmd.CommandText = sql;
            SqlCommandHelper.AddParameters(cmd, parameters);
            List<T> result = [];
            await using DbDataReader reader = await SqlCommandHelper.ExecuteReaderAsync(cmd, cancellationToken);
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
            List<T> list = await ToListAsync(cancellationToken);
            return [.. list];
        }

        public T First()
        {
            List<T> list = Page(1, 1).ToList();
            return list.Count == 0 ? throw new InvalidOperationException("Sequence contains no elements") : list[0];
        }

        public async Task<T> FirstAsync(CancellationToken cancellationToken = default)
        {
            IQuery<T> q = Page(1, 1);
            List<T> list = await q.ToListAsync(cancellationToken);
            return list.Count == 0 ? throw new InvalidOperationException("Sequence contains no elements") : list[0];
        }

        public T? FirstOrDefault()
        {
            List<T> list = Page(1, 1).ToList();
            return list.Count == 0 ? default : list[0];
        }

        public async Task<T?> FirstOrDefaultAsync(CancellationToken cancellationToken = default)
        {
            IQuery<T> q = Page(1, 1);
            List<T> list = await q.ToListAsync(cancellationToken);
            return list.Count == 0 ? default : list[0];
        }

        public T Single()
        {
            List<T> list = ToList();
            return list.Count == 0
                ? throw new InvalidOperationException("Sequence contains no elements")
                : list.Count > 1 ? throw new InvalidOperationException("Sequence contains more than one element") : list[0];
        }

        public async Task<T> SingleAsync(CancellationToken cancellationToken = default)
        {
            List<T> list = await ToListAsync(cancellationToken);
            return list.Count == 0
                ? throw new InvalidOperationException("Sequence contains no elements")
                : list.Count > 1 ? throw new InvalidOperationException("Sequence contains more than one element") : list[0];
        }

        public T? SingleOrDefault()
        {
            List<T> list = ToList();
            return list.Count > 1
                ? throw new InvalidOperationException("Sequence contains more than one element")
                : list.Count == 0 ? default : list[0];
        }

        public async Task<T?> SingleOrDefaultAsync(CancellationToken cancellationToken = default)
        {
            List<T> list = await ToListAsync(cancellationToken);
            return list.Count > 1
                ? throw new InvalidOperationException("Sequence contains more than one element")
                : list.Count == 0 ? default : list[0];
        }

        public PagedResult<T> ToPagedResult()
        {
            PaginationDescriptor pagination = Model.Pagination ?? new PaginationDescriptor();
            int page = pagination.Page ?? (pagination.Skip.HasValue && pagination.PageSize.HasValue ? (pagination.Skip.Value / pagination.PageSize.Value) + 1 : 1);
            int pageSize = pagination.PageSize ?? pagination.Take ?? 0;

            List<T> items = Page(page, pageSize).ToList();

            QueryModel<T> countModel = new(Model.Filters, Model.Projection, Model.Group, Model.Aggregates, Model.Joins, [], null, Model.IsDistinct);
            SqlQueryBuilder countQuery = new(countModel);
            (string countSql, IReadOnlyList<KeyValuePair<string, object?>> countParams, IReadOnlyList<(string Column, string Alias)> _, Type _, bool _) = countQuery.Build();

            using DbConnection conn = ConnectionFactory();
            if (conn.State != System.Data.ConnectionState.Open)
            {
                conn.Open();
            }

            using DbCommand cmd = SqlCommandHelper.CreateCommand(conn, Transaction);
            string countCommand = "SELECT COUNT(1) FROM (" + countSql + ") AS [__countSub]";
            cmd.CommandText = countCommand;
            SqlCommandHelper.AddParameters(cmd, countParams);
            int total = Convert.ToInt32(SqlCommandHelper.ExecuteScalar(ConnectionFactory, Transaction, countCommand, countParams));

            return new PagedResult<T> { Items = items, TotalCount = total, PageNumber = page, PageSize = pageSize };
        }

        public async Task<PagedResult<T>> ToPagedResultAsync(CancellationToken cancellationToken = default)
        {
            PaginationDescriptor pagination = Model.Pagination ?? new PaginationDescriptor();
            int page = pagination.Page ?? (pagination.Skip.HasValue && pagination.PageSize.HasValue ? (pagination.Skip.Value / pagination.PageSize.Value) + 1 : 1);
            int pageSize = pagination.PageSize ?? pagination.Take ?? 0;

            List<T> items = await Page(page, pageSize).ToListAsync(cancellationToken);

            QueryModel<T> countModel = new(Model.Filters, Model.Projection, Model.Group, Model.Aggregates, Model.Joins, [], null, Model.IsDistinct);
            SqlQueryBuilder countQuery = new(countModel);
            (string countSql, IReadOnlyList<KeyValuePair<string, object?>> countParams, IReadOnlyList<(string Column, string Alias)> _, Type _, bool _) = countQuery.Build();

            string countCommand2 = "SELECT COUNT(1) FROM (" + countSql + ") AS [__countSub]";
            object? totalObj = await SqlCommandHelper.ExecuteScalarAsync(ConnectionFactory, Transaction, countCommand2, countParams, cancellationToken);
            int total = Convert.ToInt32(totalObj);

            return new PagedResult<T> { Items = items, TotalCount = total, PageNumber = page, PageSize = pageSize };
        }
    }
}