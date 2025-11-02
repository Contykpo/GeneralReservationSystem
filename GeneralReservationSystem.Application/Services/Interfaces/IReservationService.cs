using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.DTOs.Authentication;
using GeneralReservationSystem.Application.Entities;

namespace GeneralReservationSystem.Application.Services.Interfaces
{
    public interface IReservationService
    {
        Task<Reservation> GetReservationAsync(ReservationKeyDto keyDto, CancellationToken cancellationToken = default);
        Task<IEnumerable<UserReservationDetailsDto>> GetUserReservationsAsync(UserKeyDto keyDto, CancellationToken cancellationToken = default);
        Task<PagedResult<ReservationDetailsDto>> SearchReservationsAsync(PagedSearchRequestDto searchDto, CancellationToken cancellationToken = default);
        Task<PagedResult<UserReservationDetailsDto>> SearchUserReservationsAsync(UserKeyDto keyDto, PagedSearchRequestDto searchDto, CancellationToken cancellationToken = default);
        Task<Reservation> CreateReservationAsync(CreateReservationDto dto, CancellationToken cancellationToken = default);
        Task DeleteReservationAsync(ReservationKeyDto keyDto, CancellationToken cancellationToken = default);
    }
}