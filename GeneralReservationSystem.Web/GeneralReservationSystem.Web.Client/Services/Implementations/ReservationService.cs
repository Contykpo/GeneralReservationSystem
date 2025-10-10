using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Services.Interfaces;

namespace GeneralReservationSystem.Web.Client.Services.Implementations
{
    public class ReservationService(HttpClient httpClient) : ApiServiceBase(httpClient), IReservationService
    {
        public async Task<Reservation> GetReservationAsync(ReservationKeyDto keyDto, CancellationToken cancellationToken = default)
        {
            return await GetAsync<Reservation>($"/api/reservations/me/trip/{keyDto.TripId}", cancellationToken);
        }

        public async Task<IEnumerable<Reservation>> GetUserReservationsAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await GetAsync<IEnumerable<Reservation>>("/api/reservations/me", cancellationToken);
        }

        public async Task<PagedResult<Reservation>> SearchReservationsAsync(PagedSearchRequestDto searchDto, CancellationToken cancellationToken = default)
        {
            return await PostAsync<PagedResult<Reservation>>("/api/reservations/search", searchDto, cancellationToken);
        }

        public async Task CreateReservationAsync(CreateReservationDto dto, int userId, CancellationToken cancellationToken = default)
        {
            await PostAsync("/api/reservations", dto, cancellationToken);
        }

        public async Task DeleteReservationAsync(ReservationKeyDto keyDto, CancellationToken cancellationToken = default)
        {
            await DeleteAsync($"/api/reservations/me/trip/{keyDto.TripId}", cancellationToken);
        }
    }
}
