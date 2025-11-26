using FluentValidation;
using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.DTOs.Authentication;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Services.Interfaces;
using GeneralReservationSystem.Web.Client.Services.Interfaces;

namespace GeneralReservationSystem.Server.Services.Implementations
{
    public class WebReservationService(
        IReservationService reservationService,
        IHttpContextAccessor httpContextAccessor,
        IValidator<PagedSearchRequestDto> pagedSearchValidator,
        IValidator<CreateReservationDto> createReservationValidator,
        IValidator<ReservationKeyDto> reservationKeyValidator,
        IValidator<UserKeyDto> userKeyValidator) : WebServiceBase(httpContextAccessor), IClientReservationService
    {
        public async Task<Reservation> GetReservationAsync(ReservationKeyDto keyDto, CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();
            await ValidateAsync(reservationKeyValidator, keyDto, cancellationToken);

            Reservation reservation = await reservationService.GetReservationAsync(keyDto, cancellationToken);
            EnsureOwnerOrAdmin(reservation.UserId);

            return reservation;
        }

        public async Task<IEnumerable<UserReservationDetailsDto>> GetUserReservationsAsync(UserKeyDto keyDto, CancellationToken cancellationToken = default)
        {
            EnsureAuthorized();
            await ValidateAsync(userKeyValidator, keyDto, cancellationToken);
            return await reservationService.GetUserReservationsAsync(keyDto, cancellationToken);
        }

        public async Task<PagedResult<ReservationDetailsDto>> SearchReservationsAsync(PagedSearchRequestDto searchDto, CancellationToken cancellationToken = default)
        {
            EnsureAuthorized();
            await ValidateAsync(pagedSearchValidator, searchDto, cancellationToken);
            return await reservationService.SearchReservationsAsync(searchDto, cancellationToken);
        }

        public async Task<PagedResult<UserReservationDetailsDto>> SearchUserReservationsAsync(UserKeyDto keyDto, PagedSearchRequestDto searchDto, CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();
            await ValidateAsync(pagedSearchValidator, searchDto, cancellationToken);
            await ValidateAsync(userKeyValidator, keyDto, cancellationToken);
            EnsureOwnerOrAdmin(keyDto.UserId);
            return await reservationService.SearchUserReservationsAsync(keyDto, searchDto, cancellationToken);
        }

        public async Task<PagedResult<UserReservationDetailsDto>> SearchCurrentUserReservationsAsync(PagedSearchRequestDto searchDto, CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();
            await ValidateAsync(pagedSearchValidator, searchDto, cancellationToken);
            UserKeyDto keyDto = new() { UserId = CurrentUserId!.Value };
            return await reservationService.SearchUserReservationsAsync(keyDto, searchDto, cancellationToken);
        }

        public async Task<Reservation> CreateReservationAsync(CreateReservationDto dto, CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();
            await ValidateAsync(createReservationValidator, dto, cancellationToken);
            EnsureOwnerOrAdmin(dto.UserId);
            return await reservationService.CreateReservationAsync(dto, cancellationToken);
        }

        public async Task DeleteReservationAsync(ReservationKeyDto keyDto, CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();
            await ValidateAsync(reservationKeyValidator, keyDto, cancellationToken);

            Reservation reservation = await reservationService.GetReservationAsync(keyDto, cancellationToken);
            EnsureOwnerOrAdmin(reservation.UserId);

            await reservationService.DeleteReservationAsync(keyDto, cancellationToken);
        }

        public async Task<IEnumerable<UserReservationDetailsDto>> GetCurrentUserReservationsAsync(CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();
            UserKeyDto keyDto = new() { UserId = CurrentUserId!.Value };
            return await reservationService.GetUserReservationsAsync(keyDto, cancellationToken);
        }

        public async Task<Reservation> CreateCurrentUserReservationAsync(ReservationKeyDto keyDto, CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();
            await ValidateAsync(reservationKeyValidator, keyDto, cancellationToken);

            CreateReservationDto dto = new()
            {
                TripId = keyDto.TripId,
                Seat = keyDto.Seat,
                UserId = CurrentUserId!.Value
            };

            return await reservationService.CreateReservationAsync(dto, cancellationToken);
        }
    }
}
