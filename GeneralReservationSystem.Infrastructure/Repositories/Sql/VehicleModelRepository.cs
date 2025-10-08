using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using System.Data.Common;

namespace GeneralReservationSystem.Infrastructure.Repositories.Sql
{
    public class VehicleModelRepository : Repository<VehicleModel>, IVehicleModelRepository
    {
        public VehicleModelRepository(Func<DbConnection> connectionFactory, DbTransaction? transaction = null)
            : base(connectionFactory, transaction) { }

        public Task<VehicleModel?> GetByNameAndManufacturerAsync(string name, string manufacturer, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Seat>> GetSeatsByVehicleModelIdAsync(int vehicleModelId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
