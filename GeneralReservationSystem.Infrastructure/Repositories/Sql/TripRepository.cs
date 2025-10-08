using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using System.Data.Common;

namespace GeneralReservationSystem.Infrastructure.Repositories.Sql
{
    public class TripRepository : Repository<Trip>, ITripRepository
    {
        public TripRepository(Func<DbConnection> connectionFactory, DbTransaction? transaction = null)
            : base(connectionFactory, transaction) { }

        public Task<Driver?> GetDriverByTripIdAsync(int tripId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Vehicle?> GetVehicleByTripIdAsync(int tripId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
