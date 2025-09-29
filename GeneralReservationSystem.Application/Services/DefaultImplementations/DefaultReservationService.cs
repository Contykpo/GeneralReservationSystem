using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using GeneralReservationSystem.Application.Services.Interfaces;
using static GeneralReservationSystem.Application.Common.OperationResult;

namespace GeneralReservationSystem.Application.Services
{
    public class DefaultReservationService : IReservationService
    {
        private readonly IReservationRepository _reservationRepository;
        private readonly IVehicleRepository _vehicleRepository;
        private readonly ISeatRepository _seatRepository;
        private readonly ITripRepository _tripRepository;

        public DefaultReservationService(IReservationRepository reservationRepository, IVehicleRepository vehicleRepository, ISeatRepository seatRepository, ITripRepository tripRepository)
        {
            _reservationRepository = reservationRepository;
            _vehicleRepository = vehicleRepository;
            _seatRepository = seatRepository;
            _tripRepository = tripRepository;
        }

        public async Task<OptionalResult<IList<AvailableSeatDto>>> GetAvailableSeatsAsync(int pageIndex, int pageSize, int tripId)
        {
            return (await _reservationRepository.GetAvailablePagedAsync(pageIndex, pageSize, tripId)).Match<OptionalResult<IList<AvailableSeatDto>>>(
                onValue: value => OptionalResult<IList<AvailableSeatDto>>.Value(value),
                onEmpty: () => OptionalResult<IList<AvailableSeatDto>>.NoValue<IList<AvailableSeatDto>>(),
                onError: error => OptionalResult<IList<AvailableSeatDto>>.Error<IList<AvailableSeatDto>>(error)
            );
        }

        public async Task<OptionalResult<IList<SeatReservationDto>>> GetReservedSeatsForTripAsync(int pageIndex, int pageSize, int tripId)
        {
            return (await _reservationRepository.GetReservedSeatsForTripPagedAsync(pageIndex, pageSize, tripId)).Match<OptionalResult<IList<SeatReservationDto>>>(
                onValue: value => OptionalResult<IList<SeatReservationDto>>.Value(value),
                onEmpty: () => OptionalResult<IList<SeatReservationDto>>.NoValue<IList<SeatReservationDto>>(),
                onError: error => OptionalResult<IList<SeatReservationDto>>.Error<IList<SeatReservationDto>>(error)
            );
        }

        public async Task<OptionalResult<IList<SeatReservationDto>>> GetReservedSeatsForUserAsync(int pageIndex, int pageSize, int userId, int? tripId)
        {
            return (await _reservationRepository.GetReservedSeatsForUserPagedAsync(pageIndex, pageSize, userId, tripId)).Match<OptionalResult<IList<SeatReservationDto>>>(
                onValue: value => OptionalResult<IList<SeatReservationDto>>.Value(value),
                onEmpty: () => OptionalResult<IList<SeatReservationDto>>.NoValue<IList<SeatReservationDto>>(),
                onError: error => OptionalResult<IList<SeatReservationDto>>.Error<IList<SeatReservationDto>>(error)
            );
        }

        public async Task<OperationResult> AddReservationAsync(CreateReservationDto reservationDto)
        {
            // Business rule: The reserved seat must be part of the vehicle model of the vehicle assigned to the trip
            var seatModelCheck = (await _tripRepository.GetVehicleByIdAsync(reservationDto.TripId)).Match(
                onValue: vehicle => (_seatRepository.GetByIdAsync(reservationDto.SeatId).Result).Match(
                    onValue: seat =>
                    {
                        if (seat.VehicleModelId != vehicle.VehicleModelId)
                        {
                            return Failure("The selected seat does not belong to the vehicle model assigned to this trip.");
                        }
                        return null;
                    },
                    onEmpty: () => Failure("Seat for the reservation not found."),
                    onError: error => Failure(error)
                ),
                onEmpty: () => Failure("Vehicle for the trip not found."),
                onError: error => Failure(error)
            );

            if (seatModelCheck is not null)
            {
                return seatModelCheck;
            }

            // NOTE: The rest of rules are handled by the database constraints (foreign keys, unique constraints, etc.)

            var reservation = new Reservation
            {
                TripId = reservationDto.TripId,
                SeatId = reservationDto.SeatId,
                UserId = reservationDto.UserId
            };
            return await _reservationRepository.AddAsync(reservation);
        }

        public Task<OperationResult> DeleteReservationAsync(Reservation reservation)
            => _reservationRepository.DeleteAsync(reservation);
    }
}
