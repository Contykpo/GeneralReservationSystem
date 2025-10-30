using GeneralReservationSystem.Application.Helpers;
using GeneralReservationSystem.Application.Repositories.Util.Interfaces;
using GeneralReservationSystem.Infrastructure.Helpers;
using System.Collections;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Linq.Expressions;

namespace GeneralReservationSystem.Infrastructure.Repositories.Util.Sql.Query
{
    public class SqlQueryProvider(Func<DbConnection> connectionFactory, DbTransaction? transaction = null) : RepositoryQueryProvider
    {
        public override string GetQueryText(Expression expression)
        {
            ProjectionExpression projection = Translate(expression);
            return QueryFormatter.Format(projection.Source);
        }

        public override object ExecuteExpression(Expression expression)
        {
            LambdaExpression? lambda = expression as LambdaExpression;
            if (lambda != null)
            {
                expression = lambda.Body;
            }

            ProjectionExpression projection = Translate(expression);

            string commandText = QueryFormatter.Format(projection.Source);
            ReadOnlyCollection<NamedValueExpression> namedValues = NamedValueGatherer.Gather(projection.Source);
            string[] names = [.. namedValues.Select(v => v.Name)];

            Expression rootQueryable = RootQueryableFinder.Find(expression)!;
            Expression providerAccess = Expression.Convert(
                Expression.Property(rootQueryable, typeof(IQueryable).GetProperty("Provider")!),
                typeof(SqlQueryProvider)
                );

            LambdaExpression projector = ProjectionBuilder.Build(this, projection, providerAccess);
            LambdaExpression eRead = GetReader(projector, projection.Aggregator!, true);

            if (lambda != null)
            {
                Expression body = Expression.Call(
                    providerAccess, "Execute", null,
                    Expression.Constant(commandText),
                    Expression.Constant(names),
                    Expression.NewArrayInit(typeof(object), [.. namedValues.Select(v => Expression.Convert(v.Value, typeof(object)))]),
                    eRead
                    );
                body = Expression.Convert(body, expression.Type);
                LambdaExpression fn = Expression.Lambda(lambda.Type, body, lambda.Parameters);
                return fn.Compile();
            }
            else
            {
                object[] values = namedValues.Select(v => v.Value as ConstantExpression).Select(c => c?.Value).ToArray()!;
                Func<DbDataReader, object> fnRead = (Func<DbDataReader, object>)eRead.Compile();
                return Execute(commandText, names, values, fnRead);
            }
        }

        public override async Task<object> ExecuteExpressionAsync(Expression expression, CancellationToken cancellationToken = default)
        {
            LambdaExpression? lambda = expression as LambdaExpression;
            if (lambda != null)
            {
                expression = lambda.Body;
            }

            ProjectionExpression projection = Translate(expression);

            string commandText = QueryFormatter.Format(projection.Source);
            ReadOnlyCollection<NamedValueExpression> namedValues = NamedValueGatherer.Gather(projection.Source);
            string[] names = [.. namedValues.Select(v => v.Name)];

            Expression rootQueryable = RootQueryableFinder.Find(expression)!;
            Expression providerAccess = Expression.Convert(
                Expression.Property(rootQueryable, typeof(IQueryable).GetProperty("Provider")!),
                typeof(SqlQueryProvider)
                );

            LambdaExpression projector = ProjectionBuilder.Build(this, projection, providerAccess);
            LambdaExpression eRead = GetReader(projector, projection.Aggregator!, true);

            if (lambda != null)
            {
                Expression body = Expression.Call(
                    providerAccess, "Execute", null,
                    Expression.Constant(commandText),
                    Expression.Constant(names),
                    Expression.NewArrayInit(typeof(object), [.. namedValues.Select(v => Expression.Convert(v.Value, typeof(object)))]),
                    eRead
                    );
                body = Expression.Convert(body, expression.Type);
                LambdaExpression fn = Expression.Lambda(lambda.Type, body, lambda.Parameters);
                return fn.Compile();
            }
            else
            {
                object[] values = namedValues.Select(v => v.Value as ConstantExpression).Select(c => c?.Value).ToArray()!;
                Func<DbDataReader, object> fnRead = (Func<DbDataReader, object>)eRead.Compile();
                return await ExecuteAsync(commandText, names, values, fnRead, cancellationToken);
            }
        }

        public object Execute(string commandText, string[] paramNames, object[] paramValues, Func<DbDataReader, object> fnRead)
        {
            // TODO: connection and command aren't being disposed, fix it.
            DbConnection connection = SqlCommandHelper.CreateAndOpenConnection(connectionFactory);
            DbCommand command = SqlCommandHelper.CreateCommand(connection, transaction);
            command.CommandText = commandText;

            for (int i = 0, n = paramNames.Length; i < n; i++)
            {
                SqlCommandHelper.AddParameter(command, paramNames[i], paramValues[i], paramValues[i].GetType());
            }

            DbDataReader reader = SqlCommandHelper.ExecuteReader(command);

            return fnRead(reader);
        }

        public async Task<object> ExecuteAsync(string commandText, string[] paramNames, object[] paramValues, Func<DbDataReader, object> fnRead, CancellationToken cancellationToken = default)
        {
            // TODO: connection and command aren't being disposed, fix it.
            DbConnection connection = await SqlCommandHelper.CreateAndOpenConnectionAsync(connectionFactory, cancellationToken);
            DbCommand command = SqlCommandHelper.CreateCommand(connection, transaction);
            command.CommandText = commandText;

            for (int i = 0, n = paramNames.Length; i < n; i++)
            {
                SqlCommandHelper.AddParameter(command, paramNames[i], paramValues[i], paramValues[i].GetType());
            }

            DbDataReader reader = await SqlCommandHelper.ExecuteReaderAsync(command, cancellationToken);

            return fnRead(reader);
        }

        private ProjectionExpression Translate(Expression? expression)
        {
            expression = Evaluator.PartialEval(expression, CanBeEvaluatedLocally);
            expression = QueryBinder.Bind(this, expression);
            expression = AggregateRewriter.Rewrite(expression);
            expression = UnusedColumnRemover.Remove(expression);
            expression = RedundantSubqueryRemover.Remove(expression);
            expression = OrderByRewriter.Rewrite(expression);
            expression = SkipRewriter.Rewrite(expression);
            expression = OrderByRewriter.Rewrite(expression);
            expression = RedundantSubqueryRemover.Remove(expression);
            expression = Parameterizer.Parameterize(expression);
            return (ProjectionExpression)expression!;
        }

        private bool CanBeEvaluatedLocally(Expression expression)
        {
            if (expression is ConstantExpression cex)
            {
                if (cex.Value is IQueryable query && query.Provider == this)
                {
                    return false;
                }
            }
            return (expression is not MethodCallExpression mc ||
                (mc.Method.DeclaringType != typeof(Enumerable) &&
                 mc.Method.DeclaringType != typeof(Queryable))) && ((expression.NodeType == ExpressionType.Convert &&
                expression.Type == typeof(object)) || (expression.NodeType != ExpressionType.Parameter &&
                   expression.NodeType != ExpressionType.Lambda));
        }
        private static LambdaExpression GetReader(LambdaExpression fnProjector, LambdaExpression fnAggregator, bool boxReturn)
        {
            ParameterExpression reader = Expression.Parameter(typeof(DbDataReader), "reader");
            Expression body = Expression.New(typeof(SqlProjectionReader<>).MakeGenericType(fnProjector.Body.Type).GetConstructors()[0], reader, fnProjector);
            if (fnAggregator != null)
            {
                body = Expression.Invoke(fnAggregator, body);
            }
            if (boxReturn && body.Type != typeof(object))
            {
                body = Expression.Convert(body, typeof(object));
            }
            return Expression.Lambda(body, reader);
        }

        private class ProjectionBuilder : DbExpressionVisitor
        {
            private readonly SqlQueryProvider provider;
            private readonly ProjectionExpression projection;
            private readonly Expression providerAccess;
            private readonly ParameterExpression dbDataReaderParam;
            private readonly Dictionary<string, int> nameMap;

            private ProjectionBuilder(SqlQueryProvider provider, ProjectionExpression projection, Expression providerAccess)
            {
                this.provider = provider;
                this.projection = projection;
                this.providerAccess = providerAccess;
                dbDataReaderParam = Expression.Parameter(typeof(DbDataReader), "reader");
                nameMap = projection.Source.Columns.Select((c, i) => new { c, i }).ToDictionary(x => x.c.Name, x => x.i);
            }

            internal static LambdaExpression Build(SqlQueryProvider provider, ProjectionExpression projection, Expression providerAccess)
            {
                ProjectionBuilder m = new(provider, projection, providerAccess);
                Expression body = m.Visit(projection.Projector)!;
                return Expression.Lambda(body, m.dbDataReaderParam);
            }

            protected override Expression VisitColumn(ColumnExpression column)
            {
                if (column.Alias == projection.Source.Alias)
                {
                    int iOrdinal = nameMap[column.Name];

                    Expression defvalue = !column.Type.IsValueType || TypeHelpers.IsNullableType(column.Type)
                        ? Expression.Constant(null, column.Type)
                        : Expression.Constant(Activator.CreateInstance(column.Type), column.Type);
                    Expression value = Expression.Convert(
                        Expression.Call(typeof(System.Convert), "ChangeType", null,
                            Expression.Call(dbDataReaderParam, "GetValue", null, Expression.Constant(iOrdinal)),
                            Expression.Constant(TypeHelpers.GetNonNullableType(column.Type))
                            ),
                            column.Type
                        );

                    return Expression.Condition(
                        Expression.Call(dbDataReaderParam, "IsDbNull", null, Expression.Constant(iOrdinal)),
                        defvalue, value
                        );
                }
                return column;
            }

            protected override Expression VisitProjection(ProjectionExpression projection)
            {
                projection = (ProjectionExpression)Parameterizer.Parameterize(projection)!;
                projection = (ProjectionExpression)OuterParameterizer.Parameterize(this.projection.Source.Alias, projection)!;

                string commandText = QueryFormatter.Format(projection.Source);
                ReadOnlyCollection<NamedValueExpression> namedValues = NamedValueGatherer.Gather(projection.Source);
                string[] names = [.. namedValues.Select(v => v.Name)];
                Expression[] values = [.. namedValues.Select(v => Expression.Convert(Visit(v.Value)!, typeof(object)))];

                LambdaExpression projector = ProjectionBuilder.Build(provider, projection, providerAccess);
                LambdaExpression eRead = GetReader(projector, projection.Aggregator!, true);

                Type resultType = projection.Aggregator != null ? projection.Aggregator.Body.Type : typeof(IEnumerable<>).MakeGenericType(projection.Projector.Type);

                return Expression.Convert(
                    Expression.Call(providerAccess, "Execute", null,
                        Expression.Constant(commandText),
                        Expression.Constant(names),
                        Expression.NewArrayInit(typeof(object), values),
                        eRead
                        ),
                    resultType
                    );
            }

            private class OuterParameterizer : DbExpressionVisitor
            {
                private int iParam;
                private string outerAlias = null!;

                internal static Expression? Parameterize(string outerAlias, Expression? expr)
                {
                    OuterParameterizer op = new()
                    {
                        outerAlias = outerAlias
                    };
                    return op.Visit(expr);
                }

                protected override Expression VisitProjection(ProjectionExpression proj)
                {
                    SelectExpression select = (SelectExpression)Visit(proj.Source)!;
                    return select != proj.Source ? new ProjectionExpression(select, proj.Projector, proj.Aggregator) : proj;
                }

                protected override Expression VisitColumn(ColumnExpression column)
                {
                    return column.Alias == outerAlias ? new NamedValueExpression("n" + iParam++, column) : column;
                }
            }
        }

        private class RootQueryableFinder : DbExpressionVisitor
        {
            private Expression? root;

            internal static Expression? Find(Expression? expression)
            {
                RootQueryableFinder finder = new();
                _ = finder.Visit(expression);
                return finder.root;
            }

            public override Expression? Visit(Expression? exp)
            {
                Expression? result = base.Visit(exp);

                if (root == null && result != null && typeof(IQueryable).IsAssignableFrom(result.Type))
                {
                    root = result;
                }

                return result;
            }
        }
    }

    public class Grouping<TKey, TElement>(TKey key, IEnumerable<TElement> group) : IGrouping<TKey, TElement>
    {
        public TKey Key { get; } = key;

        public IEnumerator<TElement> GetEnumerator()
        {
            return group.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return group.GetEnumerator();
        }
    }
}
