using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Web.Client.Services.Interfaces;

namespace GeneralReservationSystem.Web.Client.Services.Implementations
{
    public class ClientReservationService(HttpClient httpClient) : ApiServiceBase(httpClient), IClientReservationService
    {
        public async Task<Reservation> GetReservationAsync(ReservationKeyDto keyDto, CancellationToken cancellationToken = default)
        {
            return await GetAsync<Reservation>($"/api/reservations/{keyDto.TripId}/{keyDto.Seat}", cancellationToken);
        }

        public async Task<IEnumerable<Reservation>> GetUserReservationsAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await GetAsync<IEnumerable<Reservation>>($"/api/reservations/user/{userId}", cancellationToken);
        }

        public async Task<PagedResult<Reservation>> SearchReservationsAsync(PagedSearchRequestDto searchDto, CancellationToken cancellationToken = default)
        {
            return await PostAsync<PagedResult<Reservation>>("/api/reservations/search", searchDto, cancellationToken);
        }

        public async Task<Reservation> CreateReservationAsync(CreateReservationDto dto, CancellationToken cancellationToken = default)
        {
            return await PostAsync<Reservation>("/api/reservations", dto, cancellationToken);
        }

        public async Task DeleteReservationAsync(ReservationKeyDto keyDto, CancellationToken cancellationToken = default)
        {
            await DeleteAsync($"/api/reservations/{keyDto.TripId}/{keyDto.Seat}", cancellationToken);
        }

        public async Task<IEnumerable<Reservation>> GetTripUserReservationsAsync(TripUserReservationsKeyDto keyDto, CancellationToken cancellationToken = default)
        {
            return await GetAsync<IEnumerable<Reservation>>($"/api/reservations/{keyDto.TripId}/user/{keyDto.UserId}", cancellationToken);
        }

        public async Task<IEnumerable<Reservation>> GetCurrentUserReservationsAsync(CancellationToken cancellationToken = default)
        {
            return await GetAsync<IEnumerable<Reservation>>("/api/reservations/me", cancellationToken);
        }

        public async Task<IEnumerable<Reservation>> GetCurrentUserReservationsForTripAsync(int tripId, CancellationToken cancellationToken = default)
        {
            return await GetAsync<IEnumerable<Reservation>>($"/api/reservations/me/trip/{tripId}", cancellationToken);
        }

        public async Task<Reservation> CreateCurrentUserReservationAsync(ReservationKeyDto keyDto, CancellationToken cancellationToken = default)
        {
            return await PostAsync<Reservation>("/api/reservations/me", keyDto, cancellationToken);
        }
    }
}
