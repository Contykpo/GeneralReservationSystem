using System.Collections.Generic;
using System.Threading.Tasks;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Services.Interfaces;

namespace GeneralReservationSystem.Application.Services.DefaultImplementations
{
    public class DefaultVehicleService : IVehicleService
    {
        private readonly IVehicleRepository _vehicleRepository;

        public DefaultVehicleService(IVehicleRepository vehicleRepository)
        {
            _vehicleRepository = vehicleRepository;
        }

        public Task<OptionalResult<Vehicle>> GetVehicleByIdAsync(int id)
            => _vehicleRepository.GetByIdAsync(id);

        public Task<OptionalResult<VehicleModel>> GetModelByIdAsync(int id)
            => _vehicleRepository.GetModelByIdAsync(id);

        public async Task<OptionalResult<IList<VehicleDetailsDto>>> SearchVehiclesAsync(int pageIndex, int pageSize, string? modelName = null,
            string? manufacturer = null, string? licensePlate = null, GeneralReservationSystem.Application.Repositories.Interfaces.VehicleSearchSortBy? sortBy = null,
            bool descending = false)
        {
            return await _vehicleRepository.SearchPagedAsync(pageIndex, pageSize, modelName, manufacturer, licensePlate, sortBy, descending);
        }

        public Task<OperationResult> AddVehicleAsync(CreateVehicleDto vehicleDto)
        {
            var vehicle = new Vehicle
            {
                VehicleModelId = vehicleDto.VehicleModelId,
                LicensePlate = vehicleDto.LicensePlate,
                Status = vehicleDto.Status
            };
            return _vehicleRepository.AddAsync(vehicle);
        }

        public Task<OperationResult> UpdateVehicleAsync(Vehicle vehicle)
            => _vehicleRepository.UpdateAsync(vehicle);

        public Task<OperationResult> DeleteVehicleAsync(int id)
            => _vehicleRepository.DeleteAsync(id);
    }
}
