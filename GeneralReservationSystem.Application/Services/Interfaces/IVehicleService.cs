using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.DTOs;

namespace GeneralReservationSystem.Application.Services.Interfaces
{
    public interface IVehicleService
    {
        Task<Vehicle?> GetVehicleByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<VehicleModel?> GetModelOfVehicleByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<PagedResult<VehicleDetailsDto>> SearchVehiclesAsync(VehicleDetailsSearchRequestDto vehicleDetailsSearchRequestDto, CancellationToken cancellationToken = default);
        Task AddVehicleAsync(CreateVehicleDto vehicleDto, CancellationToken cancellationToken = default);
        Task UpdateVehicleAsync(UpdateVehicleDto vehicleDto, CancellationToken cancellationToken = default);
        Task DeleteVehicleAsync(int id, CancellationToken cancellationToken = default);
    }
}
