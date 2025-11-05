using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Services.Interfaces;

namespace GeneralReservationSystem.Web.Client.Services.Interfaces
{
    public interface IClientReservationService : IReservationService
    {
        Task<IEnumerable<UserReservationDetailsDto>> GetCurrentUserReservationsAsync(CancellationToken cancellationToken = default);
        Task<PagedResult<UserReservationDetailsDto>> SearchCurrentUserReservationsAsync(PagedSearchRequestDto searchDto, CancellationToken cancellationToken = default);
        Task<Reservation> CreateCurrentUserReservationAsync(ReservationKeyDto keyDto, CancellationToken cancellationToken = default);
    }
}
