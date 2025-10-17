using GeneralReservationSystem.Application.Helpers;
using GeneralReservationSystem.Application.Repositories.Util.Interfaces;
using System.Linq.Expressions;
using System.Reflection;

namespace GeneralReservationSystem.Infrastructure.Repositories.Util.Sql.Query
{
    internal class TranslateResult
    {
        internal required string CommandText;
        internal required LambdaExpression Projector;
    }

    internal class QueryBinder : ExpressionVisitor
    {
        private readonly ColumnProjector columnProjector;
        private List<OrderExpression>? thenBys;
        private Dictionary<ParameterExpression, Expression> map = [];
        private int aliasCount;
        private readonly RepositoryQueryProvider queryProvider;

        internal QueryBinder(RepositoryQueryProvider queryProvider)
        {
            columnProjector = new ColumnProjector(CanBeColumn);
            this.queryProvider = queryProvider;
        }

        private static bool CanBeColumn(Expression expression)
        {
            return expression.NodeType == (ExpressionType)DbExpressionType.Column;
        }

        internal Expression? Bind(Expression? expression)
        {
            map = [];
            return Visit(expression);
        }

        private static Expression StripQuotes(Expression e)
        {
            while (e.NodeType == ExpressionType.Quote)
            {
                e = ((UnaryExpression)e).Operand;
            }

            return e;
        }

        private string GetNextAlias()
        {
            return "t" + aliasCount++;
        }

        private ProjectedColumns ProjectColumns(Expression expression, string newAlias, params string[] existingAliases)
        {
            return columnProjector.ProjectColumns(expression, newAlias, existingAliases);
        }

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            return m.Method.DeclaringType == typeof(Queryable) ||
                m.Method.DeclaringType == typeof(Enumerable)
                ? m.Method.Name switch
                {
                    "Where" => BindWhere(m.Type, m.Arguments[0], (LambdaExpression)StripQuotes(m.Arguments[1])),
                    "Select" => BindSelect(m.Type, m.Arguments[0], (LambdaExpression)StripQuotes(m.Arguments[1])),
                    "OrderBy" => BindOrderBy(m.Type, m.Arguments[0], (LambdaExpression)StripQuotes(m.Arguments[1]), OrderType.Ascending),
                    "OrderByDescending" => BindOrderBy(m.Type, m.Arguments[0], (LambdaExpression)StripQuotes(m.Arguments[1]), OrderType.Descending),
                    "ThenBy" => BindThenBy(m.Arguments[0], (LambdaExpression)StripQuotes(m.Arguments[1]), OrderType.Ascending),
                    "ThenByDescending" => BindThenBy(m.Arguments[0], (LambdaExpression)StripQuotes(m.Arguments[1]), OrderType.Descending),
                    "Join" => BindJoin(
                        m.Type, m.Arguments[0], m.Arguments[1],
                        (LambdaExpression)StripQuotes(m.Arguments[2]),
                        (LambdaExpression)StripQuotes(m.Arguments[3]),
                        (LambdaExpression)StripQuotes(m.Arguments[4])
                        ),
                    "SelectMany" when m.Arguments.Count == 2 => BindSelectMany(
                        m.Type, m.Arguments[0],
                        (LambdaExpression)StripQuotes(m.Arguments[1]),
                        null
                        ),
                    "SelectMany" when m.Arguments.Count == 3 => BindSelectMany(
                        m.Type, m.Arguments[0],
                        (LambdaExpression)StripQuotes(m.Arguments[1]),
                        (LambdaExpression)StripQuotes(m.Arguments[2])
                        ),
                    _ => throw new NotSupportedException($"The method '{m.Method.Name}' is not supported"),
                }
                : base.VisitMethodCall(m);
        }

        private ProjectionExpression BindWhere(Type resultType, Expression source, LambdaExpression predicate)
        {
            ProjectionExpression projection = (ProjectionExpression)Visit(source);

            map[predicate.Parameters[0]] = projection.Projector;

            Expression where = Visit(predicate.Body);

            string alias = GetNextAlias();

            ProjectedColumns pc = ProjectColumns(projection.Projector, alias, GetExistingAlias(projection.Source));

            return new ProjectionExpression
            (
                new SelectExpression(resultType, alias, pc.Columns, projection.Source, where),
                pc.Projector
            );
        }

        private ProjectionExpression BindSelect(Type resultType, Expression source, LambdaExpression selector)
        {
            ProjectionExpression projection = (ProjectionExpression)Visit(source);

            map[selector.Parameters[0]] = projection.Projector;

            Expression expression = Visit(selector.Body);

            string alias = GetNextAlias();

            ProjectedColumns pc = ProjectColumns(expression, alias, GetExistingAlias(projection.Source));

            return new ProjectionExpression
            (
                new SelectExpression(resultType, alias, pc.Columns, projection.Source, null),
                pc.Projector
            );
        }

        private static string GetExistingAlias(Expression source)
        {
            return (DbExpressionType)source.NodeType switch
            {
                DbExpressionType.Select => ((SelectExpression)source).Alias,
                DbExpressionType.Table => ((TableExpression)source).Alias,
                _ => throw new InvalidOperationException(string.Format("Invalid source node type '{0}'", source.NodeType)),
            };
        }

        private bool IsTable(object? value)
        {
            return value is IQueryable q && q.Provider == this.queryProvider && q.Expression.NodeType == ExpressionType.Constant;
        }

        private bool IsTable(Expression expression)
        {
            return expression is ConstantExpression c && IsTable(c.Value);
        }

        private static string GetTableName(object table)
        {
            IQueryable tableQuery = (IQueryable)table;
            Type rowType = tableQuery.ElementType;

            //return rowType.Name;

            return EntityHelper.GetTableName(rowType);
        }

        private static string GetColumnName(MemberInfo mi)
        {
            //return pi.Name;

            return mi is not PropertyInfo and not FieldInfo
                ? throw new ArgumentException("MemberInfo must be either FieldInfo or PropertyInfo", nameof(mi))
                : EntityHelper.GetColumnName(mi);
        }

        private static Type GetColumnType(MemberInfo mi)
        {
            return mi switch
            {
                FieldInfo fi => fi.FieldType,
                PropertyInfo pi => pi.PropertyType,
                _ => throw new ArgumentException("MemberInfo must be either FieldInfo or PropertyInfo", nameof(mi)),
            };
        }

        private static IEnumerable<MemberInfo> GetMappedMembers(Type rowType)
        {
            return rowType
                .GetFields()
                .Cast<MemberInfo>()
                .Concat(
                    rowType.GetProperties()
                );
        }

        private ProjectionExpression GetTableProjection(object value)
        {
            IQueryable table = (IQueryable)value;
            string tableAlias = GetNextAlias();
            string selectAlias = GetNextAlias();

            List<MemberBinding> bindings = [];
            List<ColumnDeclaration> columns = [];

            foreach (MemberInfo mi in GetMappedMembers(table.ElementType))
            {
                string columnName = GetColumnName(mi);
                Type columnType = GetColumnType(mi);
                int ordinal = columns.Count;

                bindings.Add(Expression.Bind(mi, new ColumnExpression(columnType, selectAlias, columnName, ordinal)));

                columns.Add(new ColumnDeclaration(columnName, new ColumnExpression(columnType, tableAlias, columnName, ordinal)));
            }

            Expression projector = Expression.MemberInit(Expression.New(table.ElementType), bindings);

            Type resultType = typeof(IEnumerable<>).MakeGenericType(table.ElementType);

            return new ProjectionExpression
            (
                new SelectExpression(
                    resultType,
                    selectAlias,
                    columns,
                    new TableExpression(resultType, tableAlias, GetTableName(table)),
                    null
                ),
                projector
            );
        }

        protected virtual Expression BindOrderBy(Type resultType, Expression source, LambdaExpression orderSelector, OrderType orderType)
        {
            List<OrderExpression>? myThenBys = thenBys;
            thenBys = null;
            ProjectionExpression projection = (ProjectionExpression)Visit(source)!;

            map[orderSelector.Parameters[0]] = projection.Projector;
            List<OrderExpression> orderings = [new OrderExpression(orderType, Visit(orderSelector.Body))];

            if (myThenBys != null)
            {
                for (int i = myThenBys.Count - 1; i >= 0; i--)
                {
                    OrderExpression tb = myThenBys[i];
                    LambdaExpression lambda = (LambdaExpression)tb.Expression;
                    map[lambda.Parameters[0]] = projection.Projector;
                    orderings.Add(new OrderExpression(tb.OrderType, Visit(lambda.Body)));
                }
            }

            string alias = GetNextAlias();
            ProjectedColumns pc = ProjectColumns(projection.Projector, alias, projection.Source.Alias);
            return new ProjectionExpression(
                new SelectExpression(resultType, alias, pc.Columns, projection.Source, null, orderings.AsReadOnly()),
                pc.Projector
            );
        }

        protected virtual Expression BindThenBy(Expression source, LambdaExpression orderSelector, OrderType orderType)
        {
            thenBys ??= [];

            thenBys.Add(new OrderExpression(orderType, orderSelector));
            return Visit(source);
        }

        protected virtual Expression BindJoin(Type resultType, Expression outerSource, Expression innerSource, LambdaExpression outerKey, LambdaExpression innerKey, LambdaExpression resultSelector)
        {
            ProjectionExpression outerProjection = (ProjectionExpression)Visit(outerSource);
            ProjectionExpression innerProjection = (ProjectionExpression)Visit(innerSource);
            map[outerKey.Parameters[0]] = outerProjection.Projector;
            Expression outerKeyExpr = Visit(outerKey.Body);
            map[innerKey.Parameters[0]] = innerProjection.Projector;
            Expression innerKeyExpr = Visit(innerKey.Body);
            map[resultSelector.Parameters[0]] = outerProjection.Projector;
            map[resultSelector.Parameters[1]] = innerProjection.Projector;
            Expression resultExpr = Visit(resultSelector.Body);
            JoinExpression join = new(resultType, JoinType.InnerJoin, outerProjection.Source, innerProjection.Source, Expression.Equal(outerKeyExpr, innerKeyExpr));
            string alias = GetNextAlias();
            ProjectedColumns pc = ProjectColumns(resultExpr, alias, outerProjection.Source.Alias, innerProjection.Source.Alias);
            return new ProjectionExpression(
                new SelectExpression(resultType, alias, pc.Columns, join, null),
                pc.Projector
                );
        }

        protected virtual Expression BindSelectMany(Type resultType, Expression source, LambdaExpression collectionSelector, LambdaExpression? resultSelector)
        {
            ProjectionExpression projection = (ProjectionExpression)Visit(source);
            map[collectionSelector.Parameters[0]] = projection.Projector;
            ProjectionExpression collectionProjection = (ProjectionExpression)Visit(collectionSelector.Body);
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
                Expression result = Visit(resultSelector.Body);
                pc = ProjectColumns(result, alias, projection.Source.Alias, collectionProjection.Source.Alias);
            }
            return new ProjectionExpression(
                new SelectExpression(resultType, alias, pc.Columns, join, null),
                pc.Projector
                );
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            return IsTable(c.Value) ? GetTableProjection(c.Value!) : c;
        }

        protected override Expression VisitParameter(ParameterExpression p)
        {
            return map.TryGetValue(p, out Expression? e) ? e : p;
        }

        protected override Expression VisitMember(MemberExpression m)
        {
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

            if (a is MethodInfo && b is PropertyInfo info1)
            {
                return a == info1.GetGetMethod();
            }
            else if (a is PropertyInfo info && b is MethodInfo)
            {
                return info.GetGetMethod() == b;
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

            PropertyInfo pi = (mi as PropertyInfo)!;

            return Expression.Property(source, pi);
        }
    }
}
