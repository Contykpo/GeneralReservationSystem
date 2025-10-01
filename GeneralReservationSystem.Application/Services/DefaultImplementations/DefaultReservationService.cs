using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using GeneralReservationSystem.Application.Services.Interfaces;
using static GeneralReservationSystem.Application.Common.OperationResult;

namespace GeneralReservationSystem.Application.Services.DefaultImplementations
{
    public class DefaultReservationService : IReservationService
    {
        private readonly IReservationRepository _reservationRepository;
        private readonly ISeatRepository _seatRepository;
        private readonly ITripRepository _tripRepository;

        public DefaultReservationService(IReservationRepository reservationRepository, ISeatRepository seatRepository, ITripRepository tripRepository)
        {
            _reservationRepository = reservationRepository;
            _seatRepository = seatRepository;
            _tripRepository = tripRepository;
        }

        public async Task<OptionalResult<PagedResult<AvailableSeatDto>>> GetAvailableSeatsAsync(int pageIndex, int pageSize, int tripId)
        {
            return await _reservationRepository.GetAvailablePagedAsync(pageIndex, pageSize, tripId);
        }

        public async Task<OptionalResult<PagedResult<SeatReservationDto>>> GetReservedSeatsForTripAsync(int pageIndex, int pageSize, int tripId)
        {
            return await _reservationRepository.GetReservedSeatsForTripPagedAsync(pageIndex, pageSize, tripId);
        }

        public async Task<OptionalResult<PagedResult<SeatReservationDto>>> GetReservedSeatsForUserAsync(int pageIndex, int pageSize, Guid userId, int? tripId)
        {
            return await _reservationRepository.GetReservedSeatsForUserPagedAsync(pageIndex, pageSize, userId, tripId);
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
                            return Failure("El asiento seleccionado no pertenece al modelo de vehículo asignado a este viaje.");
                        }
                        return null;
                    },
                    onEmpty: () => Failure("No se encontró el asiento para la reserva."),
                    onError: error => Failure(error)
                ),
                onEmpty: () => Failure("No se encontró el vehículo para el viaje."),
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
