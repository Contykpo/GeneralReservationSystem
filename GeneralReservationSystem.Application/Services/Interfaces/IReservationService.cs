using GeneralReservationSystem.Application.Repositories.Interfaces;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities.Authentication;
using System.Threading.Tasks;

namespace GeneralReservationSystem.Application.Services.Interfaces
{
    public interface IReservationService
    {
        Task<PagedResult<AvailableSeatDto>> GetAvailableSeatsAsync(TripAvailableSeatSearchRequestDto tripAvailableSeatSearchRequestDto, CancellationToken cancellationToken = default);
        Task<PagedResult<ReservedSeatDto>> GetReservedSeatsForTripAsync(TripReservedSeatSearchRequestDto tripReservedSeatSearchRequestDto, CancellationToken cancellationToken = default);
        Task<PagedResult<ReservedSeatDto>> GetReservedSeatsForUserAsync(UserReservedSeatSearchRequestDto userReservedSeatSearchRequestDto, CancellationToken cancellationToken = default);
        Task AddReservationAsync(CreateReservationDto reservationDto, CancellationToken cancellationToken = default);
        Task DeleteReservationAsync(Reservation reservation, CancellationToken cancellationToken = default);
    }
}
