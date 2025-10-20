using GeneralReservationSystem.Application.Helpers;
using GeneralReservationSystem.Application.Repositories.Util.Interfaces;
using System.Collections;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace GeneralReservationSystem.Infrastructure.Repositories.Util.Sql.Query
{
    internal class QueryBinder : ExpressionVisitor
    {
        private int aliasCount;
        private readonly RepositoryQueryProvider provider = null!;
        private readonly Dictionary<ParameterExpression, Expression> map = null!;
        private readonly Dictionary<Expression, GroupByInfo> groupByMap = null!;
        private readonly Expression? root = null;

        private QueryBinder(RepositoryQueryProvider provider, Expression? root)
        {
            this.provider = provider;
            map = [];
            groupByMap = [];
            this.root = root;
        }

        internal static Expression? Bind(RepositoryQueryProvider provider, Expression? expression)
        {
            return new QueryBinder(provider, expression).Visit(expression);
        }

        private static bool CanBeColumn(Expression expression)
        {
            return expression.NodeType switch
            {
                (ExpressionType)DbExpressionType.Column or (ExpressionType)DbExpressionType.Scalar or (ExpressionType)DbExpressionType.Exists or (ExpressionType)DbExpressionType.AggregateSubquery or (ExpressionType)DbExpressionType.Aggregate => true,
                _ => false,
            };
        }

        private static Expression StripQuotes(Expression e)
        {
            while (e.NodeType == ExpressionType.Quote)
            {
                e = ((UnaryExpression)e).Operand;
            }
            return e;
        }

        internal string GetNextAlias()
        {
            return "t" + aliasCount++;
        }

        private static ProjectedColumns ProjectColumns(Expression expression, string newAlias, params string[] existingAliases)
        {
            return ColumnProjector.ProjectColumns(CanBeColumn, expression, newAlias, existingAliases);
        }

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            if (m.Method.DeclaringType == typeof(Queryable) || m.Method.DeclaringType == typeof(Enumerable))
            {
                switch (m.Method.Name)
                {
                    case "Where":
                        return BindWhere(m.Type, m.Arguments[0], (LambdaExpression)StripQuotes(m.Arguments[1]));
                    case "Select":
                        return BindSelect(m.Type, m.Arguments[0], (LambdaExpression)StripQuotes(m.Arguments[1]));
                    case "SelectMany":
                        if (m.Arguments.Count == 2)
                        {
                            return BindSelectMany(
                                m.Type, m.Arguments[0],
                                (LambdaExpression)StripQuotes(m.Arguments[1]),
                                null
                                );
                        }
                        else if (m.Arguments.Count == 3)
                        {
                            return BindSelectMany(
                                m.Type, m.Arguments[0],
                                (LambdaExpression)StripQuotes(m.Arguments[1]),
                                (LambdaExpression)StripQuotes(m.Arguments[2])
                                );
                        }
                        break;
                    case "Join":
                        return BindJoin(
                            m.Type, m.Arguments[0], m.Arguments[1],
                            (LambdaExpression)StripQuotes(m.Arguments[2]),
                            (LambdaExpression)StripQuotes(m.Arguments[3]),
                            (LambdaExpression)StripQuotes(m.Arguments[4])
                            );
                    case "OrderBy":
                        return BindOrderBy(m.Type, m.Arguments[0], (LambdaExpression)StripQuotes(m.Arguments[1]), OrderType.Ascending);
                    case "OrderByDescending":
                        return BindOrderBy(m.Type, m.Arguments[0], (LambdaExpression)StripQuotes(m.Arguments[1]), OrderType.Descending);
                    case "ThenBy":
                        return BindThenBy(m.Arguments[0], (LambdaExpression)StripQuotes(m.Arguments[1]), OrderType.Ascending);
                    case "ThenByDescending":
                        return BindThenBy(m.Arguments[0], (LambdaExpression)StripQuotes(m.Arguments[1]), OrderType.Descending);
                    case "GroupBy":
                        if (m.Arguments.Count == 2)
                        {
                            return BindGroupBy(
                                m.Arguments[0],
                                (LambdaExpression)StripQuotes(m.Arguments[1]),
                                null,
                                null
                                );
                        }
                        else if (m.Arguments.Count == 3)
                        {
                            LambdaExpression lambda1 = (LambdaExpression)StripQuotes(m.Arguments[1]);
                            LambdaExpression lambda2 = (LambdaExpression)StripQuotes(m.Arguments[2]);
                            if (lambda2.Parameters.Count == 1)
                            {
                                return BindGroupBy(m.Arguments[0], lambda1, lambda2, null);
                            }
                            else if (lambda2.Parameters.Count == 2)
                            {
                                return BindGroupBy(m.Arguments[0], lambda1, null, lambda2);
                            }
                        }
                        else if (m.Arguments.Count == 4)
                        {
                            return BindGroupBy(
                                m.Arguments[0],
                                (LambdaExpression)StripQuotes(m.Arguments[1]),
                                (LambdaExpression)StripQuotes(m.Arguments[2]),
                                (LambdaExpression)StripQuotes(m.Arguments[3])
                                );
                        }
                        break;
                    case "Count":
                    case "Min":
                    case "Max":
                    case "Sum":
                    case "Average":
                        if (m.Arguments.Count == 1)
                        {
                            return BindAggregate(m.Arguments[0], m.Method, null, m == root);
                        }
                        else if (m.Arguments.Count == 2)
                        {
                            LambdaExpression selector = (LambdaExpression)StripQuotes(m.Arguments[1]);
                            return BindAggregate(m.Arguments[0], m.Method, selector, m == root);
                        }
                        break;
                    case "Distinct":
                        if (m.Arguments.Count == 1)
                        {
                            return BindDistinct(m.Arguments[0]);
                        }
                        break;
                    case "Skip":
                        if (m.Arguments.Count == 2)
                        {
                            return BindSkip(m.Arguments[0], m.Arguments[1]);
                        }
                        break;
                    case "Take":
                        if (m.Arguments.Count == 2)
                        {
                            return BindTake(m.Arguments[0], m.Arguments[1]);
                        }
                        break;
                    case "First":
                    case "FirstOrDefault":
                    case "Single":
                    case "SingleOrDefault":
                        if (m.Arguments.Count == 1)
                        {
                            return BindFirst(m.Arguments[0], null, m.Method.Name, m == root);
                        }
                        else if (m.Arguments.Count == 2)
                        {
                            LambdaExpression predicate = (LambdaExpression)StripQuotes(m.Arguments[1]);
                            return BindFirst(m.Arguments[0], predicate, m.Method.Name, m == root);
                        }
                        break;
                    case "Any":
                        if (m.Arguments.Count == 1)
                        {
                            return BindAnyAll(m.Arguments[0], m.Method, null, m == root);
                        }
                        else if (m.Arguments.Count == 2)
                        {
                            LambdaExpression predicate = (LambdaExpression)StripQuotes(m.Arguments[1]);
                            return BindAnyAll(m.Arguments[0], m.Method, predicate, m == root);
                        }
                        break;
                    case "All":
                        if (m.Arguments.Count == 2)
                        {
                            LambdaExpression predicate = (LambdaExpression)StripQuotes(m.Arguments[1]);
                            return BindAnyAll(m.Arguments[0], m.Method, predicate, m == root);
                        }
                        break;
                    case "Contains":
                        if (m.Arguments.Count == 2)
                        {
                            return BindContains(m.Arguments[0], m.Arguments[1], m == root);
                        }
                        break;
                }
            }
            return base.VisitMethodCall(m);
        }

        private ProjectionExpression VisitSequence(Expression source)
        {
            return ConvertToSequence(base.Visit(source));
        }

        private static ProjectionExpression ConvertToSequence(Expression expr)
        {
            switch (expr.NodeType)
            {
                case (ExpressionType)DbExpressionType.Projection:
                    return (ProjectionExpression)expr;
                case ExpressionType.New:
                    NewExpression nex = (NewExpression)expr;
                    if (expr.Type.IsGenericType && expr.Type.GetGenericTypeDefinition() == typeof(Grouping<,>))
                    {
                        return (ProjectionExpression)nex.Arguments[1];
                    }
                    goto default;
                default:
                    throw new Exception(string.Format("The expression of type '{0}' is not a sequence", expr.Type));
            }
        }

        public override Expression? Visit(Expression? exp)
        {
            Expression? result = base.Visit(exp);

            if (result != null)
            {
                Type expectedType = exp!.Type;
                if (result is ProjectionExpression projection && projection.Aggregator == null && !expectedType.IsAssignableFrom(projection.Type))
                {
                    LambdaExpression? aggregator = GetAggregator(expectedType, projection.Projector);
                    if (aggregator != null)
                    {
                        return new ProjectionExpression(projection.Source, projection.Projector, aggregator);
                    }
                }
            }

            return result;
        }

        private static LambdaExpression? GetAggregator(Type expectedType, Expression projector)
        {
            Type elementType = projector.Type;
            Type actualType = typeof(IEnumerable<>).MakeGenericType(elementType);
            if (!expectedType.IsAssignableFrom(actualType))
            {
                ParameterExpression p = Expression.Parameter(actualType, "p");
                Expression? body = null;
                if (expectedType.IsGenericType && expectedType.GetGenericTypeDefinition() == typeof(IQueryable<>))
                {
                    body = Expression.Call(typeof(Queryable), "AsQueryable", [elementType], p);
                }
                else if (expectedType.IsArray && expectedType.GetArrayRank() == 1)
                {
                    body = Expression.Call(typeof(Enumerable), "ToArray", [elementType], p);
                }
                else if (typeof(IList).IsAssignableFrom(expectedType))
                {
                    body = Expression.Call(typeof(Enumerable), "ToList", [elementType], p);
                }
                if (body != null)
                {
                    return Expression.Lambda(body, p);
                }
            }
            return null;
        }

        private ProjectionExpression BindWhere(Type resultType, Expression source, LambdaExpression predicate)
        {
            ProjectionExpression projection = VisitSequence(source);
            map[predicate.Parameters[0]] = projection.Projector;
            Expression where = Visit(predicate.Body)!;
            string alias = GetNextAlias();
            ProjectedColumns pc = ProjectColumns(projection.Projector, alias, projection.Source.Alias);
            return new ProjectionExpression(
                new SelectExpression(resultType, alias, pc.Columns, projection.Source, where),
                pc.Projector
                );
        }

        private ProjectionExpression BindSelect(Type resultType, Expression source, LambdaExpression selector)
        {
            ProjectionExpression projection = VisitSequence(source);
            map[selector.Parameters[0]] = projection.Projector;
            Expression expression = Visit(selector.Body)!;
            string alias = GetNextAlias();
            ProjectedColumns pc = ProjectColumns(expression, alias, projection.Source.Alias);
            return new ProjectionExpression(
                new SelectExpression(resultType, alias, pc.Columns, projection.Source, null),
                pc.Projector
                );
        }

        protected virtual Expression BindSelectMany(Type resultType, Expression source, LambdaExpression collectionSelector, LambdaExpression? resultSelector)
        {
            ProjectionExpression projection = VisitSequence(source);
            map[collectionSelector.Parameters[0]] = projection.Projector;
            ProjectionExpression collectionProjection = VisitSequence(collectionSelector.Body);
            JoinType joinType = IsTable(collectionSelector.Body) ? JoinType.CrossJoin : JoinType.CrossApply;
            JoinExpression join = new(resultType, joinType, projection.Source, collectionProjection.Source, null);
            string alias = GetNextAlias();
            ProjectedColumns pc;
            if (resultSelector == null)
            {
                pc = ProjectColumns(collectionProjection.Projector, alias, projection.Source.Alias, collectionProjection.Source.Alias);
            }
            else
            {
                map[resultSelector.Parameters[0]] = projection.Projector;
                map[resultSelector.Parameters[1]] = collectionProjection.Projector;
                Expression result = Visit(resultSelector.Body)!;
                pc = ProjectColumns(result, alias, projection.Source.Alias, collectionProjection.Source.Alias);
            }
            return new ProjectionExpression(
                new SelectExpression(resultType, alias, pc.Columns, join, null),
                pc.Projector
                );
        }

        protected virtual Expression BindJoin(Type resultType, Expression outerSource, Expression innerSource, LambdaExpression outerKey, LambdaExpression innerKey, LambdaExpression resultSelector)
        {
            ProjectionExpression outerProjection = VisitSequence(outerSource);
            ProjectionExpression innerProjection = VisitSequence(innerSource);
            map[outerKey.Parameters[0]] = outerProjection.Projector;
            Expression outerKeyExpr = Visit(outerKey.Body)!;
            map[innerKey.Parameters[0]] = innerProjection.Projector;
            Expression innerKeyExpr = Visit(innerKey.Body)!;
            map[resultSelector.Parameters[0]] = outerProjection.Projector;
            map[resultSelector.Parameters[1]] = innerProjection.Projector;
            Expression resultExpr = Visit(resultSelector.Body)!;
            JoinExpression join = new(resultType, JoinType.InnerJoin, outerProjection.Source, innerProjection.Source, Expression.Equal(outerKeyExpr, innerKeyExpr));
            string alias = GetNextAlias();
            ProjectedColumns pc = ProjectColumns(resultExpr, alias, outerProjection.Source.Alias, innerProjection.Source.Alias);
            return new ProjectionExpression(
                new SelectExpression(resultType, alias, pc.Columns, join, null),
                pc.Projector
                );
        }

        private List<OrderExpression>? thenBys;

        protected virtual Expression BindOrderBy(Type resultType, Expression source, LambdaExpression orderSelector, OrderType orderType)
        {
            List<OrderExpression>? myThenBys = thenBys;
            thenBys = null;
            ProjectionExpression projection = VisitSequence(source);

            map[orderSelector.Parameters[0]] = projection.Projector;
            List<OrderExpression> orderings = [new OrderExpression(orderType, Visit(orderSelector.Body)!)];

            if (myThenBys != null)
            {
                for (int i = myThenBys.Count - 1; i >= 0; i--)
                {
                    OrderExpression tb = myThenBys[i];
                    LambdaExpression lambda = (LambdaExpression)tb.Expression;
                    map[lambda.Parameters[0]] = projection.Projector;
                    orderings.Add(new OrderExpression(tb.OrderType, Visit(lambda.Body)!));
                }
            }

            string alias = GetNextAlias();
            ProjectedColumns pc = ProjectColumns(projection.Projector, alias, projection.Source.Alias);
            return new ProjectionExpression(
                new SelectExpression(resultType, alias, pc.Columns, projection.Source, null, orderings.AsReadOnly(), null),
                pc.Projector
                );
        }

        protected virtual Expression BindThenBy(Expression source, LambdaExpression orderSelector, OrderType orderType)
        {
            thenBys ??= [];
            thenBys.Add(new OrderExpression(orderType, orderSelector));
            return Visit(source)!;
        }

        protected virtual Expression BindGroupBy(Expression source, LambdaExpression keySelector, LambdaExpression? elementSelector, LambdaExpression? resultSelector)
        {
            ProjectionExpression projection = VisitSequence(source);

            map[keySelector.Parameters[0]] = projection.Projector;
            Expression keyExpr = Visit(keySelector.Body)!;

            Expression? elemExpr = projection.Projector;
            if (elementSelector != null)
            {
                map[elementSelector.Parameters[0]] = projection.Projector;
                elemExpr = Visit(elementSelector.Body);
            }

            ProjectedColumns keyProjection = ProjectColumns(keyExpr, projection.Source.Alias, projection.Source.Alias);
            IEnumerable<Expression> groupExprs = keyProjection.Columns.Select(c => c.Expression);

            ProjectionExpression subqueryBasis = VisitSequence(source);

            map[keySelector.Parameters[0]] = subqueryBasis.Projector;
            Expression subqueryKey = Visit(keySelector.Body)!;

            ProjectedColumns subqueryKeyPC = ProjectColumns(subqueryKey, subqueryBasis.Source.Alias, subqueryBasis.Source.Alias);
            IEnumerable<Expression> subqueryGroupExprs = subqueryKeyPC.Columns.Select(c => c.Expression);
            Expression? subqueryCorrelation = BuildPredicateWithNullsEqual(subqueryGroupExprs, groupExprs);

            Expression? subqueryElemExpr = subqueryBasis.Projector;
            if (elementSelector != null)
            {
                map[elementSelector.Parameters[0]] = subqueryBasis.Projector;
                subqueryElemExpr = Visit(elementSelector.Body);
            }

            string elementAlias = GetNextAlias();
            ProjectedColumns elementPC = ProjectColumns(subqueryElemExpr!, elementAlias, subqueryBasis.Source.Alias);
            ProjectionExpression elementSubquery =
                new(
                    new SelectExpression(TypeHelpers.GetSequenceType(subqueryElemExpr!.Type), elementAlias, elementPC.Columns, subqueryBasis.Source, subqueryCorrelation),
                    elementPC.Projector
                    );

            string alias = GetNextAlias();

            GroupByInfo info = new(alias, elemExpr!);
            groupByMap.Add(elementSubquery, info);

            Expression resultExpr;
            if (resultSelector != null)
            {
                Expression saveGroupElement = currentGroupElement;
                currentGroupElement = elementSubquery;
                map[resultSelector.Parameters[0]] = keyProjection.Projector;
                map[resultSelector.Parameters[1]] = elementSubquery;
                resultExpr = Visit(resultSelector.Body)!;
                currentGroupElement = saveGroupElement;
            }
            else
            {
                resultExpr = Expression.New(
                    typeof(Grouping<,>).MakeGenericType(keyExpr.Type, subqueryElemExpr.Type).GetConstructors()[0],
                    [keyExpr, elementSubquery]
                    );
            }

            ProjectedColumns pc = ProjectColumns(resultExpr, alias, projection.Source.Alias);

            Expression projectedElementSubquery = ((NewExpression)pc.Projector).Arguments[1];
            groupByMap.Add(projectedElementSubquery, info);

            return new ProjectionExpression(
                new SelectExpression(TypeHelpers.GetSequenceType(resultExpr.Type), alias, pc.Columns, projection.Source, null, null, groupExprs),
                pc.Projector
                );
        }

        private static Expression? BuildPredicateWithNullsEqual(IEnumerable<Expression> source1, IEnumerable<Expression> source2)
        {
            IEnumerator<Expression> en1 = source1.GetEnumerator();
            IEnumerator<Expression> en2 = source2.GetEnumerator();
            Expression? result = null;
            while (en1.MoveNext() && en2.MoveNext())
            {
                Expression compare =
                    Expression.Or(
                        Expression.And(new IsNullExpression(en1.Current), new IsNullExpression(en2.Current)),
                        Expression.Equal(en1.Current, en2.Current)
                        );
                result = (result == null) ? compare : Expression.And(result, compare);
            }
            return result;
        }

        private Expression currentGroupElement = null!;

        private class GroupByInfo
        {
            internal string Alias { get; private set; }
            internal Expression Element { get; private set; }
            internal GroupByInfo(string alias, Expression element)
            {
                Alias = alias;
                Element = element;
            }
        }

        private static AggregateType GetAggregateType(string methodName)
        {
            return methodName switch
            {
                "Count" => AggregateType.Count,
                "Min" => AggregateType.Min,
                "Max" => AggregateType.Max,
                "Sum" => AggregateType.Sum,
                "Average" => AggregateType.Average,
                _ => throw new Exception(string.Format("Unknown aggregate type: {0}", methodName)),
            };
        }

        private static bool HasPredicateArg(AggregateType aggregateType)
        {
            return aggregateType == AggregateType.Count;
        }

        private Expression BindAggregate(Expression source, MethodInfo method, LambdaExpression? argument, bool isRoot)
        {
            Type returnType = method.ReturnType;
            AggregateType aggType = GetAggregateType(method.Name);
            bool hasPredicateArg = HasPredicateArg(aggType);
            bool isDistinct = false;
            bool argumentWasPredicate = false;
            bool useAlternateArg = false;

            if (source is MethodCallExpression mcs && !hasPredicateArg && argument == null)
            {
                if (mcs.Method.Name == "Distinct" && mcs.Arguments.Count == 1 &&
                    (mcs.Method.DeclaringType == typeof(Queryable) || mcs.Method.DeclaringType == typeof(Enumerable)))
                {
                    source = mcs.Arguments[0];
                    isDistinct = true;
                }
            }

            if (argument != null && hasPredicateArg)
            {
                source = Expression.Call(typeof(Queryable), "Where", method.GetGenericArguments(), source, argument);
                argument = null;
                argumentWasPredicate = true;
            }

            ProjectionExpression projection = VisitSequence(source);

            Expression? argExpr = null;
            if (argument != null)
            {
                map[argument.Parameters[0]] = projection.Projector;
                argExpr = Visit(argument.Body);
            }
            else if (!hasPredicateArg || useAlternateArg)
            {
                argExpr = projection.Projector;
            }

            string alias = GetNextAlias();
            _ = ProjectColumns(projection.Projector, alias, projection.Source.Alias);
            Expression aggExpr = new AggregateExpression(returnType, aggType, argExpr!, isDistinct);
            Type selectType = typeof(IEnumerable<>).MakeGenericType(returnType);
            SelectExpression select = new(selectType, alias, [new("", aggExpr)], projection.Source, null);

            if (isRoot)
            {
                ParameterExpression p = Expression.Parameter(selectType, "p");
                LambdaExpression gator = Expression.Lambda(Expression.Call(typeof(Enumerable), "Single", [returnType], p), p);
                return new ProjectionExpression(select, new ColumnExpression(returnType, alias, ""), gator);
            }

            ScalarExpression subquery = new(returnType, select);

            if (!argumentWasPredicate && groupByMap.TryGetValue(projection, out GroupByInfo? info))
            {
                if (argument != null)
                {
                    map[argument.Parameters[0]] = info.Element;
                    argExpr = Visit(argument.Body)!;
                }
                else if (!hasPredicateArg || useAlternateArg)
                {
                    argExpr = info.Element;
                }
                aggExpr = new AggregateExpression(returnType, aggType, argExpr!, isDistinct);

                return projection == currentGroupElement ? aggExpr : new AggregateSubqueryExpression(info.Alias, aggExpr, subquery);
            }

            return subquery;
        }

        private ProjectionExpression BindDistinct(Expression source)
        {
            ProjectionExpression projection = VisitSequence(source);
            SelectExpression select = projection.Source;
            string alias = GetNextAlias();
            ProjectedColumns pc = ProjectColumns(projection.Projector, alias, projection.Source.Alias);
            return new ProjectionExpression(
                new SelectExpression(select.Type, alias, pc.Columns, projection.Source, null, null, null, true, null, null),
                pc.Projector
                );
        }

        private ProjectionExpression BindTake(Expression source, Expression take)
        {
            ProjectionExpression projection = VisitSequence(source);
            take = Visit(take)!;
            SelectExpression select = projection.Source;
            string alias = GetNextAlias();
            ProjectedColumns pc = ProjectColumns(projection.Projector, alias, projection.Source.Alias);
            return new ProjectionExpression(
                new SelectExpression(select.Type, alias, pc.Columns, projection.Source, null, null, null, false, null, take),
                pc.Projector
                );
        }

        private ProjectionExpression BindSkip(Expression source, Expression skip)
        {
            ProjectionExpression projection = VisitSequence(source);
            skip = Visit(skip)!;
            SelectExpression select = projection.Source;
            string alias = GetNextAlias();
            ProjectedColumns pc = ProjectColumns(projection.Projector, alias, projection.Source.Alias);
            return new ProjectionExpression(
                new SelectExpression(select.Type, alias, pc.Columns, projection.Source, null, null, null, false, skip, null),
                pc.Projector
                );
        }

        private ProjectionExpression BindFirst(Expression source, LambdaExpression? predicate, string kind, bool isRoot)
        {
            ProjectionExpression projection = VisitSequence(source);
            Expression? where = null;
            if (predicate != null)
            {
                map[predicate.Parameters[0]] = projection.Projector;
                where = Visit(predicate.Body);
            }
            Expression? take = kind.StartsWith("First") ? Expression.Constant(1) : null;
            if (take != null || where != null)
            {
                string alias = GetNextAlias();
                ProjectedColumns pc = ProjectColumns(projection.Projector, alias, projection.Source.Alias);
                projection = new ProjectionExpression(
                    new SelectExpression(source.Type, alias, pc.Columns, projection.Source, where, null, null, false, null, take),
                    pc.Projector
                    );
            }
            if (isRoot)
            {
                Type elementType = projection.Projector.Type;
                ParameterExpression p = Expression.Parameter(typeof(IEnumerable<>).MakeGenericType(elementType), "p");
                LambdaExpression gator = Expression.Lambda(Expression.Call(typeof(Enumerable), kind, [elementType], p), p);
                return new ProjectionExpression(projection.Source, projection.Projector, gator);
            }
            return projection;
        }

        private Expression BindAnyAll(Expression source, MethodInfo method, LambdaExpression? predicate, bool isRoot)
        {
            bool isAll = method.Name == "All";
            if (source is ConstantExpression constSource && !IsTable(constSource))
            {
                Debug.Assert(!isRoot);
                Expression? where = null;
                foreach (object value in (IEnumerable)constSource.Value!)
                {
                    Expression expr = Expression.Invoke(predicate!, Expression.Constant(value, predicate!.Parameters[0].Type));
                    where = where == null ? expr : isAll ? Expression.And(where, expr) : Expression.Or(where, expr);
                }
                return Visit(where)!;
            }
            else
            {
                if (isAll)
                {
                    predicate = Expression.Lambda(Expression.Not(predicate!.Body), [.. predicate.Parameters]);
                }
                if (predicate != null)
                {
                    source = Expression.Call(typeof(Queryable), "Where", method.GetGenericArguments(), source, predicate);
                }
                ProjectionExpression projection = VisitSequence(source);
                Expression result = new ExistsExpression(projection.Source);
                if (isAll)
                {
                    result = Expression.Not(result);
                }
                return isRoot ? GetSingletonSequence(result, "SingleOrDefault") : result;
            }
        }

        private Expression BindContains(Expression source, Expression match, bool isRoot)
        {
            if (source is ConstantExpression constSource && !IsTable(constSource))
            {
                Debug.Assert(!isRoot);
                List<Expression> values = [];
                foreach (object value in (IEnumerable)constSource.Value!)
                {
                    values.Add(Expression.Constant(Convert.ChangeType(value, match.Type), match.Type));
                }
                match = Visit(match)!;
                return new InExpression(match, values);
            }
            else
            {
                ProjectionExpression projection = VisitSequence(source);
                match = Visit(match)!;
                Expression result = new InExpression(match, projection.Source);
                return isRoot ? GetSingletonSequence(result, "SingleOrDefault") : result;
            }
        }

        private ProjectionExpression GetSingletonSequence(Expression expr, string aggregator)
        {
            ParameterExpression p = Expression.Parameter(typeof(IEnumerable<>).MakeGenericType(expr.Type), "p");
            LambdaExpression? gator = null;
            if (aggregator != null)
            {
                gator = Expression.Lambda(Expression.Call(typeof(Enumerable), aggregator, [expr.Type], p), p);
            }
            string alias = GetNextAlias();
            SelectExpression select = new(p.Type, alias, [new ColumnDeclaration("value", expr)], null, null);
            return new ProjectionExpression(select, new ColumnExpression(expr.Type, alias, "value"), gator);
        }

        private static bool IsTable(Expression expression)
        {
            return expression.Type.IsGenericType && expression.Type.GetGenericTypeDefinition() == typeof(RepositoryQuery<>);
        }

        private static string GetTableName(Type rowType)
        {
            return EntityHelper.GetTableName(rowType);
        }

        private static string GetColumnName(MemberInfo member)
        {
            return EntityHelper.GetColumnName(member);
        }

        private static Type GetColumnType(MemberInfo member)
        {
            FieldInfo? fi = member as FieldInfo;
            if (fi != null)
            {
                return fi.FieldType;
            }
            PropertyInfo pi = (PropertyInfo)member;
            return pi.PropertyType;
        }

        private static IEnumerable<MemberInfo> GetMappedMembers(Type rowType)
        {
            return rowType.GetFields().Cast<MemberInfo>().Concat(rowType.GetProperties()).OrderBy(m => m.Name);
        }

        private ProjectionExpression GetTableProjection(Type rowType)
        {
            string tableAlias = GetNextAlias();
            string selectAlias = GetNextAlias();
            List<MemberBinding> bindings = [];
            List<ColumnDeclaration> columns = [];
            foreach (MemberInfo mi in GetMappedMembers(rowType))
            {
                string columnName = GetColumnName(mi);
                Type columnType = GetColumnType(mi);
                bindings.Add(Expression.Bind(mi, new ColumnExpression(columnType, selectAlias, columnName)));
                columns.Add(new ColumnDeclaration(columnName, new ColumnExpression(columnType, tableAlias, columnName)));
            }
            Expression projector = Expression.MemberInit(Expression.New(rowType), bindings);
            Type resultType = typeof(IEnumerable<>).MakeGenericType(rowType);
            return new ProjectionExpression(
                new SelectExpression(resultType, selectAlias, columns, new TableExpression(resultType, tableAlias, GetTableName(rowType)), null),
                projector
                );
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            return IsTable(c) ? GetTableProjection(TypeHelpers.GetElementType(c.Type)) : c;
        }

        protected override Expression VisitParameter(ParameterExpression p)
        {
            return map.TryGetValue(p, out Expression? e) ? e : p;
        }

        protected override Expression VisitInvocation(InvocationExpression iv)
        {
            if (iv.Expression is LambdaExpression lambda)
            {
                for (int i = 0, n = lambda.Parameters.Count; i < n; i++)
                {
                    map[lambda.Parameters[i]] = iv.Arguments[i];
                }
                return Visit(lambda.Body)!;
            }
            return base.VisitInvocation(iv);
        }

        protected override Expression VisitMember(MemberExpression m)
        {
            if (IsTable(m))
            {
                return GetTableProjection(TypeHelpers.GetElementType(m.Type));
            }
            Expression source = Visit(m.Expression)!;
            switch (source.NodeType)
            {
                case ExpressionType.MemberInit:
                    MemberInitExpression min = (MemberInitExpression)source;
                    for (int i = 0, n = min.Bindings.Count; i < n; i++)
                    {
                        if (min.Bindings[i] is MemberAssignment assign && MembersMatch(assign.Member, m.Member))
                        {
                            return assign.Expression;
                        }
                    }
                    break;
                case ExpressionType.New:
                    NewExpression nex = (NewExpression)source;
                    if (nex.Members != null)
                    {
                        for (int i = 0, n = nex.Members.Count; i < n; i++)
                        {
                            if (MembersMatch(nex.Members[i], m.Member))
                            {
                                return nex.Arguments[i];
                            }
                        }
                    }
                    else if (nex.Type.IsGenericType && nex.Type.GetGenericTypeDefinition() == typeof(Grouping<,>))
                    {
                        if (m.Member.Name == "Key")
                        {
                            return nex.Arguments[0];
                        }
                    }
                    break;
            }
            return source == m.Expression ? m : MakeMember(source, m.Member);
        }

        private static bool MembersMatch(MemberInfo a, MemberInfo b)
        {
            if (a == b)
            {
                return true;
            }
            if (a is MethodInfo && b is PropertyInfo infoB)
            {
                return a == infoB.GetGetMethod();
            }
            else if (a is PropertyInfo infoA && b is MethodInfo)
            {
                return infoA.GetGetMethod() == b;
            }
            return false;
        }

        private static MemberExpression MakeMember(Expression source, MemberInfo mi)
        {
            FieldInfo? fi = mi as FieldInfo;
            if (fi != null)
            {
                return Expression.Field(source, fi);
            }
            PropertyInfo pi = (PropertyInfo)mi;
            return Expression.Property(source, pi);
        }
    }
}
