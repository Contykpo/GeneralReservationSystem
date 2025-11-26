using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Web.Client.Helpers;
using GeneralReservationSystem.Web.Client.Services.Interfaces;

namespace GeneralReservationSystem.Web.Client.Services.Implementations
{
    public class ClientTripService(HttpClient httpClient) : ClientServiceBase(httpClient), IClientTripService
    {
        public async Task<Trip> GetTripAsync(TripKeyDto keyDto, CancellationToken cancellationToken = default)
        {
            return await GetAsync<Trip>($"/api/trips/{keyDto.TripId}", cancellationToken);
        }

        public async Task<TripWithDetailsDto> GetTripWithDetailsAsync(TripKeyDto keyDto, CancellationToken cancellationToken = default)
        {
            return await GetAsync<TripWithDetailsDto>($"/api/trips/{keyDto.TripId}/details", cancellationToken);
        }

        public async Task<IEnumerable<Trip>> GetAllTripsAsync(CancellationToken cancellationToken = default)
        {
            return await GetAsync<IEnumerable<Trip>>("/api/trips", cancellationToken);
        }

        public async Task<PagedResult<TripWithDetailsDto>> SearchTripsAsync(PagedSearchRequestDto searchDto, CancellationToken cancellationToken = default)
        {
            string query = searchDto.ToQueryString();
            return await GetAsync<PagedResult<TripWithDetailsDto>>($"/api/trips/search?{query}", cancellationToken);
        }

        public async Task<Trip> CreateTripAsync(CreateTripDto dto, CancellationToken cancellationToken = default)
        {
            return await PostAsync<Trip>("/api/trips", dto, cancellationToken);
        }

        public async Task DeleteTripAsync(TripKeyDto keyDto, CancellationToken cancellationToken = default)
        {
            await DeleteAsync($"/api/trips/{keyDto.TripId}", cancellationToken);
        }

        public async Task<IEnumerable<int>> GetFreeSeatsAsync(TripKeyDto keyDto, CancellationToken cancellationToken = default)
        {
            return await GetAsync<IEnumerable<int>>($"/api/trips/{keyDto.TripId}/free-seats", cancellationToken);
        }
    }
}
