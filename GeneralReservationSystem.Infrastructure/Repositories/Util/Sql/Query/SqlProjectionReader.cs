using System.Collections;
using System.Data.Common;

namespace GeneralReservationSystem.Infrastructure.Repositories.Util.Sql.Query
{
    public class SqlProjectionReader<T>(DbDataReader reader, Func<DbDataReader, T> projector) : IEnumerable<T>, IEnumerable, IDisposable
    {
        private Enumerator? enumerator = new(reader, projector);

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

        void IDisposable.Dispose()
        {
            if (enumerator != null)
            {
                enumerator.Dispose();
                enumerator = null;
            }
            GC.SuppressFinalize(this);
        }

        private class Enumerator : IEnumerator<T>, IEnumerator, IDisposable
        {
            private readonly DbDataReader reader;
            private readonly Func<DbDataReader, T> projector;

            internal Enumerator(DbDataReader reader, Func<DbDataReader, T> projector)
            {
                this.reader = reader;
                this.projector = projector;

                Current = default!;
            }

            public T Current { get; private set; }

            object IEnumerator.Current => Current!;

            public bool MoveNext()
            {
                if (reader.Read())
                {
                    Current = projector(reader);
                    return true;
                }
                Dispose();
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
