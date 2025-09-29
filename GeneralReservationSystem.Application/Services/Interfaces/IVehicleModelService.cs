using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;

namespace GeneralReservationSystem.Application.Services.Interfaces
{
    public interface IVehicleModelService
    {
        Task<OptionalResult<IList<VehicleModel>>> SearchVehicleModelsAsync(int pageIndex, int pageSize, string? name = null, string? manufacturer = null,
            Repositories.Interfaces.VehicleModelSearchSortBy? sortBy = null, bool descending = false);
        Task<OptionalResult<VehicleModel>> GetVehicleModelByIdAsync(int id);
        Task<OperationResult> AddVehicleModelAsync(CreateVehicleModelDto vehicleModelDto);
        Task<OperationResult> UpdateVehicleModelAsync(VehicleModel vehicleModel);
        Task<OperationResult> DeleteVehicleModelAsync(int id);
    }
}
