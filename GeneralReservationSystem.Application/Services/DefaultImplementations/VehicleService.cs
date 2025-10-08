using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using GeneralReservationSystem.Application.Services.Interfaces;

namespace GeneralReservationSystem.Application.Services.DefaultImplementations
{
    public class VehicleService : IVehicleService
    {
        private readonly IVehicleRepository _vehicleRepository;

        public VehicleService(IVehicleRepository vehicleRepository)
        {
            _vehicleRepository = vehicleRepository;
        }

        public Task AddVehicleAsync(CreateVehicleDto vehicleDto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task DeleteVehicleAsync(int id, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<VehicleModel?> GetModelOfVehicleByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Vehicle?> GetVehicleByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<PagedResult<VehicleDetailsDto>> SearchVehiclesAsync(VehicleDetailsSearchRequestDto vehicleDetailsSearchRequestDto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task UpdateVehicleAsync(UpdateVehicleDto vehicleDto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
