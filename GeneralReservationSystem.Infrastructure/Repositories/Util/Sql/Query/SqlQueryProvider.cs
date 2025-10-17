using GeneralReservationSystem.Application.Helpers;
using GeneralReservationSystem.Application.Repositories.Util.Interfaces;
using GeneralReservationSystem.Infrastructure.Helpers;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;

namespace GeneralReservationSystem.Infrastructure.Repositories.Util.Sql.Query
{
    public class SqlQueryProvider(Func<DbConnection> connectionFactory, DbTransaction? transaction = null) : RepositoryQueryProvider
    {
        public override object ExecuteExpression(Expression expression)
        {
            TranslateResult result = Translate(expression);
            Delegate projector = result.Projector.Compile();

            // TODO: connection and command aren't being disposed, fix it.
            DbConnection connection = SqlCommandHelper.CreateAndOpenConnection(connectionFactory);
            DbCommand command = SqlCommandHelper.CreateCommand(connection, transaction);
            command.CommandText = result.CommandText;

            Console.WriteLine($"Executing SQL Query: {command.CommandText}");

            DbDataReader reader = SqlCommandHelper.ExecuteReader(command);
            Type elementType = TypeHelpers.GetElementType(expression.Type);

            return Activator.CreateInstance(
                typeof(ProjectionReader<>).MakeGenericType(elementType),
                BindingFlags.Instance | BindingFlags.NonPublic, null,
                [reader, projector, this],
                null
            )!;
        }

        public override Task<object> ExecuteExpressionAsync(Expression expression, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Async execution not implemented yet.");
        }

        private static TranslateResult Translate(Expression expression)
        {
            if (expression is not ProjectionExpression projection)
            {
                expression = Evaluator.PartialEval(expression)!;
                projection = (ProjectionExpression)new QueryBinder().Bind(expression);
            }
            string commandText = new QueryFormatter().Format(projection.Source);
            LambdaExpression projector = new ProjectionBuilder().Build(projection.Projector, projection.Source.Alias);

            return new TranslateResult
            {
                CommandText = commandText,
                Projector = projector
            };
        }

        public override string GetQueryText(Expression expression)
        {
            return Translate(expression).CommandText;
        }
    }
}
