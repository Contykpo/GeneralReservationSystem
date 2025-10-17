using GeneralReservationSystem.Application.Repositories.Interfaces;
using GeneralReservationSystem.Application.Repositories.Interfaces.Authentication;
using GeneralReservationSystem.Application.Repositories.Util.Interfaces;
using GeneralReservationSystem.Infrastructure.Helpers;
using GeneralReservationSystem.Infrastructure.Repositories.Sql;
using GeneralReservationSystem.Infrastructure.Repositories.Sql.Authentication;
using GeneralReservationSystem.Infrastructure.Repositories.Util.Sql.Query;
using System.Data.Common;

namespace GeneralReservationSystem.Infrastructure.Repositories.Util.Sql
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DbConnection _connection;
        private readonly DbTransaction _transaction;
        private readonly RepositoryQueryProvider _provider;

        private IUserRepository? _users;
        private IReservationRepository? _reservations;
        private IStationRepository? _stations;
        private ITripRepository? _trips;

        public IUserRepository UserRepository => _users ??= new UserRepository(_provider, () => _connection, _transaction);
        public IStationRepository StationRepository => _stations ??= new StationRepository(_provider, () => _connection, _transaction);
        public IReservationRepository ReservationRepository => _reservations ??= new ReservationRepository(_provider, () => _connection, _transaction);
        public ITripRepository TripRepository => _trips ??= new TripRepository(_provider, () => _connection, _transaction);

        public UnitOfWork(Func<DbConnection> connectionFactory)
        {
            _connection = SqlCommandHelper.CreateAndOpenConnectionAsync(connectionFactory).GetAwaiter().GetResult();
            _transaction = SqlCommandHelper.CreateTransactionAsync(_connection).GetAwaiter().GetResult();
            _provider = new SqlQueryProvider(() => _connection, _transaction);
        }

        public void Commit()
        {
            _transaction.Commit();
            _connection.Close();
        }

        public void Rollback()
        {
            _transaction.Rollback();
            _connection.Close();
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _connection?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
