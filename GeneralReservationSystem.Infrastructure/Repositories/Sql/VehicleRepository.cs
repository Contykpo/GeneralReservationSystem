using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using System.Data.Common;

namespace GeneralReservationSystem.Infrastructure.Repositories.Sql
{
	public class VehicleRepository : Repository<Vehicle>, IVehicleRepository
	{
        public VehicleRepository(Func<DbConnection> connectionFactory, DbTransaction? transaction = null)
			: base(connectionFactory, transaction) { }

		public Task<VehicleModel?> GetModelByIdAsync(int vehicleId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Trip>> GetTripsByVehicleIdAsync(int vehicleId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
