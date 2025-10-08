using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using GeneralReservationSystem.Application.Services.Interfaces;

namespace GeneralReservationSystem.Application.Services.DefaultImplementations
{
    public class VehicleModelService : IVehicleModelService
    {
        private readonly IVehicleModelRepository _vehicleModelRepository;

        public VehicleModelService(IVehicleModelRepository vehicleModelRepository)
        {
            _vehicleModelRepository = vehicleModelRepository;
        }

        public Task AddVehicleModelAsync(CreateVehicleModelDto vehicleModelDto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task DeleteVehicleModelAsync(int id, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<VehicleModel?> GetVehicleModelByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<PagedResult<VehicleModel>> SearchVehicleModelsAsync(VehicleModelSearchRequestDto vehicleModelSearchRequestDto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task UpdateVehicleModelAsync(UpdateVehicleModelDto vehicleModelDto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
