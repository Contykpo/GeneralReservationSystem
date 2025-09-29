using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.DTOs;

namespace GeneralReservationSystem.Application.Services.Interfaces
{
    public interface IVehicleService
    {
        Task<OptionalResult<Vehicle>> GetVehicleByIdAsync(int id);
        Task<OptionalResult<VehicleModel>> GetModelByIdAsync(int id);
        Task<OptionalResult<IList<VehicleDetailsDto>>> SearchVehiclesAsync(int pageIndex, int pageSize, string? modelName = null,
            string? manufacturer = null, string? licensePlate = null, Repositories.Interfaces.VehicleSearchSortBy? sortBy = null,
            bool descending = false);
        Task<OperationResult> AddVehicleAsync(CreateVehicleDto vehicleDto);
        Task<OperationResult> UpdateVehicleAsync(Vehicle vehicle);
        Task<OperationResult> DeleteVehicleAsync(int id);
    }
}
