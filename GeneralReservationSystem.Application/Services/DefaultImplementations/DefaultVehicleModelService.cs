using System.Collections.Generic;
using System.Threading.Tasks;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.Services.Interfaces;
using GeneralReservationSystem.Application.DTOs;
using static GeneralReservationSystem.Application.Common.OperationResult;

namespace GeneralReservationSystem.Application.Services
{
    public class DefaultVehicleModelService : IVehicleModelService
    {
        private readonly IVehicleModelRepository _vehicleModelRepository;

        public DefaultVehicleModelService(IVehicleModelRepository vehicleModelRepository)
        {
            _vehicleModelRepository = vehicleModelRepository;
        }

        public Task<OptionalResult<IList<VehicleModel>>> SearchVehicleModelsAsync(int pageIndex, int pageSize, string? name = null, string? manufacturer = null,
            GeneralReservationSystem.Application.Repositories.Interfaces.VehicleModelSearchSortBy? sortBy = null, bool descending = false)
            => _vehicleModelRepository.SearchPagedAsync(pageIndex, pageSize, name, manufacturer, sortBy, descending);

        public Task<OptionalResult<VehicleModel>> GetVehicleModelByIdAsync(int id)
            => _vehicleModelRepository.GetByIdAsync(id);

        public async Task<OperationResult> AddVehicleModelAsync(CreateVehicleModelDto vehicleModelDto)
        {
            // Business rule: No duplicate seat locations (Row, Column) allowed
            var seatLocations = new HashSet<(int Row, int Column)>();
            foreach (var seat in vehicleModelDto.Seats)
            {
                var location = (seat.Row, seat.Column);
                if (!seatLocations.Add(location))
                {
                    return Failure($"Duplicate seat location detected at Row {seat.Row}, Column {seat.Column}.");
                }
            }

            var vehicleModel = new VehicleModel
            {
                Name = vehicleModelDto.Name,
                Manufacturer = vehicleModelDto.Manufacturer
            };
            var seats = vehicleModelDto.Seats.Select(s => new Seat
            {
                VehicleModelId = 0, // Will be set after VehicleModel is created
                Row = s.Row,
                Column = s.Column,
                IsAtWindow = s.IsAtWindow,
                IsAtAisle = s.IsAtAisle,
                IsInFront = s.IsInFront,
                IsInBack = s.IsInBack,
                IsAccessible = s.IsAccessible
            }).ToList();
            return await _vehicleModelRepository.AddAsync(vehicleModel, seats);
        }

        public Task<OperationResult> UpdateVehicleModelAsync(VehicleModel vehicleModel)
            => _vehicleModelRepository.UpdateAsync(vehicleModel);

        public Task<OperationResult> DeleteVehicleModelAsync(int id)
            => _vehicleModelRepository.DeleteAsync(id);
    }
}
