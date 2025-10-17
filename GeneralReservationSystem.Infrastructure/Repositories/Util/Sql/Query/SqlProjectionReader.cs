using GeneralReservationSystem.Application.Repositories.Util.Interfaces;
using System.Collections;
using System.Data.Common;
using System.Linq.Expressions;

namespace GeneralReservationSystem.Infrastructure.Repositories.Util.Sql.Query
{
    internal class ProjectionReader<T> : IEnumerable<T>
    {
        private Enumerator? enumerator;

        internal ProjectionReader(DbDataReader reader, Func<ProjectionRow, T> projector, RepositoryQueryProvider queryProvider)
        {
            enumerator = new Enumerator(reader, projector, queryProvider);
        }

        public IEnumerator<T> GetEnumerator()
        {
            Enumerator? e = enumerator ?? throw new InvalidOperationException("Cannot enumerate more than once");

            enumerator = null;

            return e;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private class Enumerator : ProjectionRow, IEnumerator<T>, IDisposable
        {
            private readonly DbDataReader reader;

            private readonly RepositoryQueryProvider queryProvider;

            public T Current { get; private set; } = default!;

            object IEnumerator.Current => Current!;

            private readonly Func<ProjectionRow, T> projector;

            internal Enumerator(DbDataReader reader, Func<ProjectionRow, T> projector, RepositoryQueryProvider queryProvider)
            {
                this.reader = reader;
                this.projector = projector;
                this.queryProvider = queryProvider;
            }

            public override object? GetValue(int index)
            {
                return index >= 0 ? reader.IsDBNull(index) ? null : reader.GetValue(index) : throw new IndexOutOfRangeException();
            }

            public override IEnumerable<E> ExecuteSubQuery<E>(LambdaExpression query)
            {
                ProjectionExpression projection = (ProjectionExpression)new Replacer().Replace(query.Body, query.Parameters[0], Expression.Constant(this));
                projection = (ProjectionExpression)Evaluator.PartialEval(projection, CanEvaluateLocally)!;

                IEnumerable<E> result = (IEnumerable<E>)queryProvider.ExecuteExpression(projection);
                List<E> list = [.. result];

                return typeof(IQueryable<E>).IsAssignableFrom(query.Body.Type) ? list.AsQueryable() : list;
            }

            private static bool CanEvaluateLocally(Expression expression)
            {
                return expression.NodeType != ExpressionType.Parameter &&
!expression.NodeType.IsDbExpression();
            }

            public bool MoveNext()
            {
                if (reader.Read())
                {
                    Current = projector(this);
                    return true;
                }
                return false;
            }

            public void Reset()
            {
            }

            public void Dispose()
            {
                reader.Dispose();
            }
        }
    }
}
