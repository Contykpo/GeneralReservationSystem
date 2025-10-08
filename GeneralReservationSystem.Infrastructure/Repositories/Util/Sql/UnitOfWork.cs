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
        private DbTransaction _transaction;

        private IUserRepository? _users;
        private IRoleRepository? _roles;
        private ISessionRepository? _sessions;

        private IReservationRepository? _reservations;
        private IDestinationRepository? _destinations;
        private IDriverRepository? _drivers;
        private ISeatRepository? _seats;
        private ITripRepository? _trips;
        private IVehicleModelRepository? _vehicleModels;
        private IVehicleRepository? _vehicles;

        public IRoleRepository RoleRepository => _roles ??= new RoleRepository(() => _connection, null!);
        public IUserRepository UserRepository => _users ??= new UserRepository(() => _connection, null!);
        public ISessionRepository SessionRepository => _sessions ??= new SessionRepository(() => _connection, null!);
        public IDestinationRepository DestinationRepository => _destinations ??= new DestinationRepository(() => _connection, null!);
        public IDriverRepository DriverRepository => _drivers ??= new DriverRepository(() => _connection, null!);
        public IReservationRepository ReservationRepository => _reservations ??= new ReservationRepository(() => _connection, null!);
        public ISeatRepository SeatRepository => _seats ??= new SeatRepository(() => _connection, null!);
        public ITripRepository TripRepository => _trips ??= new TripRepository(() => _connection, null!);
        public IVehicleModelRepository VehicleModelRepository => _vehicleModels ??= new VehicleModelRepository(() => _connection, null!);
        public IVehicleRepository VehicleRepository => _vehicles ??= new VehicleRepository(() => _connection, null!);

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
