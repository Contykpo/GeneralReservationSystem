using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;

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
        Task<OptionalResult<IList<Trip>>> GetTripsByVehicleIdAsync(int id);
        Task<OptionalResult<IList<VehicleDetailsDto>>> SearchPagedAsync(int pageIndex, int pageSize, string? modelName = null,
            string? manufacturer = null, string? licensePlate = null, VehicleSearchSortBy? sortBy = null,
            bool descending = false);
        Task<OperationResult> AddAsync(Vehicle vehicle);
        Task<OperationResult> UpdateAsync(Vehicle vehicle);
        Task<OperationResult> DeleteAsync(int id);
    }
}
