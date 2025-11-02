using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.DTOs.Authentication;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Web.Client.Helpers;
using GeneralReservationSystem.Web.Client.Services.Interfaces;

namespace GeneralReservationSystem.Web.Client.Services.Implementations
{
    public class ClientReservationService(HttpClient httpClient) : ApiServiceBase(httpClient), IClientReservationService
    {
        public async Task<Reservation> GetReservationAsync(ReservationKeyDto keyDto, CancellationToken cancellationToken = default)
        {
            return await GetAsync<Reservation>($"/api/reservations/{keyDto.TripId}/{keyDto.Seat}", cancellationToken);
        }

        public async Task<IEnumerable<UserReservationDetailsDto>> GetUserReservationsAsync(UserKeyDto keyDto, CancellationToken cancellationToken = default)
        {
            return await GetAsync<IEnumerable<UserReservationDetailsDto>>($"/api/reservations/user/{keyDto.UserId}", cancellationToken);
        }

        public async Task<PagedResult<ReservationDetailsDto>> SearchReservationsAsync(PagedSearchRequestDto searchDto, CancellationToken cancellationToken = default)
        {
            string query = searchDto.ToQueryString();
            return await GetAsync<PagedResult<ReservationDetailsDto>>($"/api/reservations/search?{query}", cancellationToken);
        }

        public async Task<PagedResult<UserReservationDetailsDto>> SearchUserReservationsAsync(UserKeyDto keyDto, PagedSearchRequestDto searchDto, CancellationToken cancellationToken = default)
        {
            string query = searchDto.ToQueryString();
            return await GetAsync<PagedResult<UserReservationDetailsDto>>($"/api/reservations/search/{keyDto.UserId}?{query}", cancellationToken);
        }

        public async Task<PagedResult<UserReservationDetailsDto>> SearchCurrentUserReservationsAsync(PagedSearchRequestDto searchDto, CancellationToken cancellationToken = default)
        {
            string query = searchDto.ToQueryString();
            return await GetAsync<PagedResult<UserReservationDetailsDto>>($"/api/reservations/search/me?{query}", cancellationToken);
        }

        public async Task<Reservation> CreateReservationAsync(CreateReservationDto dto, CancellationToken cancellationToken = default)
        {
            return await PostAsync<Reservation>("/api/reservations", dto, cancellationToken);
        }

        public async Task DeleteReservationAsync(ReservationKeyDto keyDto, CancellationToken cancellationToken = default)
        {
            await DeleteAsync($"/api/reservations/{keyDto.TripId}/{keyDto.Seat}", cancellationToken);
        }

        public async Task<IEnumerable<UserReservationDetailsDto>> GetCurrentUserReservationsAsync(CancellationToken cancellationToken = default)
        {
            return await GetAsync<IEnumerable<UserReservationDetailsDto>>("/api/reservations/me", cancellationToken);
        }

        public async Task<Reservation> CreateCurrentUserReservationAsync(ReservationKeyDto keyDto, CancellationToken cancellationToken = default)
        {
            return await PostAsync<Reservation>("/api/reservations/me", keyDto, cancellationToken);
        }
    }
}
