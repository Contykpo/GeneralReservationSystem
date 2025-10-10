using GeneralReservationSystem.Application.Repositories.Interfaces;
using GeneralReservationSystem.Application.Repositories.Interfaces.Authentication;
using GeneralReservationSystem.Application.Repositories.Util.Interfaces;
using GeneralReservationSystem.Infrastructure.Helpers;
using GeneralReservationSystem.Infrastructure.Repositories.Sql;
using GeneralReservationSystem.Infrastructure.Repositories.Sql.Authentication;
using System.Data.Common;

namespace GeneralReservationSystem.Infrastructure.Repositories.Util.Sql
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DbConnection _connection;
        private readonly DbTransaction _transaction;

        private IUserRepository? _users;

        private IReservationRepository? _reservations;
        private IStationRepository? _stations;
        private ITripRepository? _trips;

        public IUserRepository UserRepository => _users ??= new UserRepository(() => _connection, null!);
        public IStationRepository StationRepository => _stations ??= new StationRepository(() => _connection, null!);
        public IReservationRepository ReservationRepository => _reservations ??= new ReservationRepository(() => _connection, null!);
        public ITripRepository TripRepository => _trips ??= new TripRepository(() => _connection, null!);

        public UnitOfWork(Func<DbConnection> connectionFactory)
        {
            _connection = SqlCommandHelper.CreateAndOpenConnectionAsync(connectionFactory).GetAwaiter().GetResult();
            _transaction = SqlCommandHelper.CreateTransactionAsync(_connection).GetAwaiter().GetResult();
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
        }
    }
}
