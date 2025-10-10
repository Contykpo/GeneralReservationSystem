using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Services.Interfaces;

namespace GeneralReservationSystem.Web.Client.Services.Implementations
{
    public class TripService(HttpClient httpClient) : ApiServiceBase(httpClient), ITripService
    {
        public async Task<Trip> GetTripAsync(TripKeyDto keyDto, CancellationToken cancellationToken = default)
        {
            return await GetAsync<Trip>($"/api/trips/{keyDto.TripId}", cancellationToken);
        }

        public async Task<IEnumerable<Trip>> GetAllTripsAsync(CancellationToken cancellationToken = default)
        {
            return await GetAsync<IEnumerable<Trip>>("/api/trips", cancellationToken);
        }

        public async Task<PagedResult<Trip>> SearchTripsAsync(PagedSearchRequestDto searchDto, CancellationToken cancellationToken = default)
        {
            return await PostAsync<PagedResult<Trip>>("/api/trips/search", searchDto, cancellationToken);
        }

        public async Task<Trip> CreateTripAsync(CreateTripDto dto, CancellationToken cancellationToken = default)
        {
            return await PostAsync<Trip>("/api/trips", dto, cancellationToken);
        }

        public async Task<Trip> UpdateTripAsync(UpdateTripDto dto, CancellationToken cancellationToken = default)
        {
            return await PutAsync<Trip>($"/api/trips/{dto.TripId}", dto, cancellationToken);
        }

        public async Task DeleteTripAsync(TripKeyDto keyDto, CancellationToken cancellationToken = default)
        {
            await DeleteAsync($"/api/trips/{keyDto.TripId}", cancellationToken);
        }
    }
}
