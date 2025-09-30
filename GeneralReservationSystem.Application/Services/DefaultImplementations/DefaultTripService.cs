using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using GeneralReservationSystem.Application.Services.Interfaces;
using static GeneralReservationSystem.Application.Common.OperationResult;

namespace GeneralReservationSystem.Application.Services.DefaultImplementations
{
    public class DefaultTripService : ITripService
    {
        private readonly ITripRepository _tripRepository;
        private readonly IVehicleRepository _vehicleRepository;
        private readonly IDriverRepository _driverRepository;

        public DefaultTripService(ITripRepository tripRepository, IVehicleRepository vehicleRepository, IDriverRepository driverRepository)
        {
            _tripRepository = tripRepository;
            _vehicleRepository = vehicleRepository;
            _driverRepository = driverRepository;
        }

        public Task<OptionalResult<IList<TripDetailsDto>>> SearchPagedAsync(int pageIndex, int pageSize,
            string? departureName = null, string? departureCity = null, string? destinationName = null, string? destinationCity = null,
            DateTime? startDate = null, DateTime? endDate = null, bool onlyWithAvailableSeats = true,
            TripSearchSortBy? sortBy = null, bool descending = false)
            => _tripRepository.SearchPagedAsync(pageIndex, pageSize, departureName, departureCity, destinationName, destinationCity, startDate, endDate, onlyWithAvailableSeats, sortBy, descending);

        public Task<OptionalResult<Trip>> GetByIdAsync(int id)
            => _tripRepository.GetByIdAsync(id);

        public async Task<OperationResult> AddAsync(CreateTripDto tripDto)
        {
            // Business rule: DepartureId and DestinationId must be valid and not equal
            if (tripDto.DepartureId <= 0 || tripDto.DestinationId <= 0 || tripDto.DepartureId == tripDto.DestinationId)
            {
                return Failure("El origen y el destino deben ser válidos y diferentes.");
            }

            // Business rule: DepartureTime must be before ArrivalTime
            if (tripDto.DepartureTime >= tripDto.ArrivalTime)
            {
                return Failure("La hora de salida debe ser anterior a la hora de llegada.");
            }

            // Business rule: Driver must exist and license must not be expired at DepartureTime and ArrivalTime
            var driverCheck = (await _tripRepository.GetDriverByIdAsync(tripDto.DriverId)).Match(
                onValue: driver =>
                {
                    if (driver.LicenseExpiryDate < tripDto.DepartureTime)
                        return Failure("La licencia del conductor estará vencida en la hora de salida.");
                    if (driver.LicenseExpiryDate < tripDto.ArrivalTime)
                        return Failure("La licencia del conductor estará vencida antes de la llegada.");
                    return null;
                },
                onEmpty: () => Failure("No se encontró el conductor."),
                onError: error => Failure(error)
            );

            if (driverCheck is not null)
            {
                return driverCheck;
            }

            // Business rule: Driver must be available during the trip
            var driverAvailabilityCheck = (await _driverRepository.GetTripsByDriverIdAsync(tripDto.DriverId)).Match(
                onValue: trips =>
                {
                    foreach (var trip in trips)
                    {
                        if (tripDto.DepartureTime <= trip.ArrivalTime && trip.DepartureTime <= tripDto.ArrivalTime)
                        {
                            return Failure("El conductor no está disponible durante el horario seleccionado.");
                        }
                    }
                    return null;
                },
                onEmpty: () => null,
                onError: error => Failure(error)
            );

            if (driverAvailabilityCheck is not null)
            {
                return driverAvailabilityCheck;
            }

            // Business rule: Vehicle must exist and status must be "Available"
            var vehicleCheck = (await _tripRepository.GetVehicleByIdAsync(tripDto.VehicleId)).Match(
                onValue: vehicle =>
                {
                    if (vehicle.Status != "Available")
                        return Failure("El vehículo no está disponible.");
                    return null;
                },
                onEmpty: () => Failure("No se encontró el vehículo."),
                onError: error => Failure(error)
            );

            if (vehicleCheck is not null)
            {
                return vehicleCheck;
            }

            // Business rule: Vehicle must be available during the trip
            var vehicleAvailabilityCheck = (await _vehicleRepository.GetTripsByVehicleIdAsync(tripDto.VehicleId)).Match(
                onValue: trips =>
                {
                    foreach (var trip in trips)
                    {
                        if (tripDto.DepartureTime <= trip.ArrivalTime && trip.DepartureTime <= tripDto.ArrivalTime)
                        {
                            return Failure("El vehículo no está disponible durante el horario seleccionado.");
                        }
                    }
                    return null;
                },
                onEmpty: () => null,
                onError: error => Failure(error)
            );

            if (vehicleAvailabilityCheck is not null)
            {
                return vehicleAvailabilityCheck;
            }

            var trip = new Trip
            {
                VehicleId = tripDto.VehicleId,
                DepartureId = tripDto.DepartureId,
                DestinationId = tripDto.DestinationId,
                DriverId = tripDto.DriverId,
                DepartureTime = tripDto.DepartureTime,
                ArrivalTime = tripDto.ArrivalTime
            };

            return await _tripRepository.AddAsync(trip);
        }

        public Task<OperationResult> UpdateAsync(Trip trip)
            => _tripRepository.UpdateAsync(trip);

        public Task<OperationResult> DeleteAsync(int id)
            => _tripRepository.DeleteAsync(id);
    }
}
