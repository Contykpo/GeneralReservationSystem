using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;

namespace GeneralReservationSystem.Application.Services.Interfaces
{
    public interface IVehicleModelService
    {
        Task<PagedResult<VehicleModel>> SearchVehicleModelsAsync(VehicleModelSearchRequestDto vehicleModelSearchRequestDto, CancellationToken cancellationToken = default);
        Task<VehicleModel?> GetVehicleModelByIdAsync(int id, CancellationToken cancellationToken = default);
        Task AddVehicleModelAsync(CreateVehicleModelDto vehicleModelDto, CancellationToken cancellationToken = default);
        Task UpdateVehicleModelAsync(UpdateVehicleModelDto vehicleModelDto, CancellationToken cancellationToken = default);
        Task DeleteVehicleModelAsync(int id, CancellationToken cancellationToken = default);
    }
}
