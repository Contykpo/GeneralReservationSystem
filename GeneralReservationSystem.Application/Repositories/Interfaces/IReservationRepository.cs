using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;

namespace GeneralReservationSystem.Application.Repositories.Interfaces
{
    public interface IReservationRepository : IRepository<Reservation>
    {
        Task<Reservation?> GetByKeyAsync(int tripId, int seat, CancellationToken cancellationToken);
        Task<IEnumerable<UserReservationDetailsDto>> GetByUserIdWithDetailsAsync(int userId, CancellationToken cancellationToken);
        Task<PagedResult<UserReservationDetailsDto>> SearchForUserIdWithDetailsAsync(int userId, PagedSearchRequestDto searchDto, CancellationToken cancellationToken);
        Task<PagedResult<ReservationDetailsDto>> SearchWithDetailsAsync(PagedSearchRequestDto searchDto, CancellationToken cancellationToken);
    }
}
