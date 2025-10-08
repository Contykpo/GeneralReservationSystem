using GeneralReservationSystem.Application.Entities;

namespace GeneralReservationSystem.Application.Repositories.Interfaces
{
    public interface IVehicleRepository : IRepository<Vehicle> 
    {
        Task<VehicleModel?> GetModelByIdAsync(int vehicleId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Trip>> GetTripsByVehicleIdAsync(int vehicleId, CancellationToken cancellationToken = default);
    }

    /*public interface IVehicleRepository
    {
        Task<OptionalResult<Vehicle>> GetByIdAsync(int id);
        Task<OptionalResult<VehicleModel>> GetModelByIdAsync(int id);
        Task<OptionalResult<IList<Trip>>> GetTripsByVehicleIdAsync(int id);
        Task<OptionalResult<PagedResult<VehicleDetailsDto>>> SearchPagedAsync(int pageIndex, int pageSize, string? modelName = null,
            string? manufacturer = null, string? licensePlate = null, VehicleSearchOrderBy? sortBy = null,
            bool descending = false);
        Task<OperationResult> AddAsync(Vehicle vehicle);
        Task<OperationResult> UpdateAsync(Vehicle vehicle);
        Task<OperationResult> DeleteAsync(int id);
    }*/
}
