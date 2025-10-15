using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;

namespace GeneralReservationSystem.Application.Services.Interfaces
{
    public interface IReservationService
    {
        Task<Reservation> GetReservationAsync(Reservation reservation, CancellationToken cancellationToken = default);
        Task<IEnumerable<Reservation>> GetUserReservationsAsync(int userId, CancellationToken cancellationToken = default);
        Task<PagedResult<Reservation>> SearchReservationsAsync(PagedSearchRequestDto searchDto, CancellationToken cancellationToken = default);
        Task CreateReservationAsync(Reservation reservation, CancellationToken cancellationToken = default);
        Task DeleteReservationAsync(Reservation reservation, CancellationToken cancellationToken = default);
    }
}