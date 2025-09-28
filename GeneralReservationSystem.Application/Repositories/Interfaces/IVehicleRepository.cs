using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;

namespace GeneralReservationSystem.Application.Repositories.Interfaces
{
    public enum VehicleSearchSortBy
    {
        ModelName,
        Manufacturer,
        LicensePlate,
        Status
    }

    public interface IVehicleRepository
    {
        Task<OptionalResult<Vehicle>> GetByIdAsync(int id);
        Task<OptionalResult<VehicleModel>> GetModelByIdAsync(int id);
        Task<OptionalResult<IList<Vehicle>>> GetAllAsync();
        Task<OptionalResult<IList<VehicleSearchResult>>> SearchPaginatedAsync(int pageIndex, int pageSize, string? modelName = null,
            string? manufacturer = null, string? licensePlate = null, VehicleSearchSortBy? sortBy = null,
            bool descending = false); // NOTE: Ideally, search should be in a service layer, but 
                                      // we don't use an ORM. This is simpler.
        Task<OperationResult> AddAsync(Vehicle vehicle);
        Task<OperationResult> UpdateAsync(Vehicle vehicle);
        Task<OperationResult> DeleteAsync(int id);
    }
}
