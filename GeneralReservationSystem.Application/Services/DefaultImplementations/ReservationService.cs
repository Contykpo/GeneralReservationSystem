using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using GeneralReservationSystem.Application.Services.Interfaces;

namespace GeneralReservationSystem.Application.Services.DefaultImplementations
{
    public class ReservationService : IReservationService
    {
        private readonly IReservationRepository _reservationRepository;
        private readonly ISeatRepository _seatRepository;
        private readonly ITripRepository _tripRepository;

        public ReservationService(IReservationRepository reservationRepository, ISeatRepository seatRepository, ITripRepository tripRepository)
        {
            _reservationRepository = reservationRepository;
            _seatRepository = seatRepository;
            _tripRepository = tripRepository;
        }

        public Task AddReservationAsync(CreateReservationDto reservationDto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task DeleteReservationAsync(Reservation reservation, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<PagedResult<AvailableSeatDto>> GetAvailableSeatsAsync(TripAvailableSeatSearchRequestDto tripAvailableSeatSearchRequestDto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<PagedResult<ReservedSeatDto>> GetReservedSeatsForTripAsync(TripReservedSeatSearchRequestDto tripReservedSeatSearchRequestDto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<PagedResult<ReservedSeatDto>> GetReservedSeatsForUserAsync(UserReservedSeatSearchRequestDto userReservedSeatSearchRequestDto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
