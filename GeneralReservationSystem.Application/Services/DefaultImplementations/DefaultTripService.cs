using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using GeneralReservationSystem.Application.Services.Interfaces;
using static GeneralReservationSystem.Application.Common.OperationResult;

namespace GeneralReservationSystem.Application.Services
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
            // Business rule: Departure and Destination must be different
            if (tripDto.DepartureId == tripDto.DestinationId)
            {
                return Failure("Departure and destination must be different.");
            }

            // Business rule: DepartureTime must be before ArrivalTime
            if (tripDto.DepartureTime >= tripDto.ArrivalTime)
            {
                return Failure("Departure time must be before arrival time.");
            }

            // Business rule: Driver must exist and license must not be expired at DepartureTime and ArrivalTime
            var driverCheck = (await _tripRepository.GetDriverByIdAsync(tripDto.DriverId)).Match(
                onValue: driver =>
                {
                    if (driver.LicenseExpiryDate < tripDto.DepartureTime)
                        return Failure("Driver's license will be expired at the time of departure.");
                    if (driver.LicenseExpiryDate < tripDto.ArrivalTime)
                        return Failure("Driver's license will be expired before arrival time.");
                    return null;
                },
                onEmpty: () => Failure("Driver not found."),
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
                        // Check for overlap:
                        // (StartDate1 <= EndDate2) and (StartDate2 <= EndDate1)
                        // https://stackoverflow.com/questions/325933/determine-whether-two-date-ranges-overlap
                        if (tripDto.DepartureTime <= trip.ArrivalTime && trip.DepartureTime <= tripDto.ArrivalTime)
                        {
                            return Failure("Driver is not available during the selected trip time.");
                        }
                    }
                    return null;
                },
                onEmpty: () => null, // No trips, so driver is available
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
                        return Failure("Vehicle is not available.");
                    return null;
                },
                onEmpty: () => Failure("Vehicle not found."),
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
                        // Check for overlap:
                        // (StartDate1 <= EndDate2) and (StartDate2 <= EndDate1)
                        // https://stackoverflow.com/questions/325933/determine-whether-two-date-ranges-overlap
                        if (tripDto.DepartureTime <= trip.ArrivalTime && trip.DepartureTime <= tripDto.ArrivalTime)
                        {
                            return Failure("Vehicle is not available during the selected trip time.");
                        }
                    }
                    return null;
                },
                onEmpty: () => null, // No trips, so vehicle is available
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
