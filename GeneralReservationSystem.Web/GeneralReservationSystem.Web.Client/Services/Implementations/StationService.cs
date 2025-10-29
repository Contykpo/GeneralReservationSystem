using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Services.Interfaces;
using GeneralReservationSystem.Web.Client.Helpers;

namespace GeneralReservationSystem.Web.Client.Services.Implementations
{
    public class StationService(HttpClient httpClient) : ApiServiceBase(httpClient), IStationService
    {
        public async Task<Station> GetStationAsync(StationKeyDto keyDto, CancellationToken cancellationToken = default)
        {
            return await GetAsync<Station>($"/api/stations/{keyDto.StationId}", cancellationToken);
        }

        public async Task<IEnumerable<Station>> GetAllStationsAsync(CancellationToken cancellationToken = default)
        {
            return await GetAsync<IEnumerable<Station>>("/api/stations", cancellationToken);
        }

        public async Task<PagedResult<Station>> SearchStationsAsync(PagedSearchRequestDto searchDto, CancellationToken cancellationToken = default)
        {
            string query = searchDto.ToQueryString();
            return await GetAsync<PagedResult<Station>>($"/api/stations/search?{query}", cancellationToken);
        }

        public async Task<Station> CreateStationAsync(CreateStationDto dto, CancellationToken cancellationToken = default)
        {
            return await PostAsync<Station>("/api/stations", dto, cancellationToken);
        }

        public async Task<Station> UpdateStationAsync(UpdateStationDto dto, CancellationToken cancellationToken = default)
        {
            return await PutAsync<Station>($"/api/stations/{dto.StationId}", dto, cancellationToken);
        }

        public async Task DeleteStationAsync(StationKeyDto keyDto, CancellationToken cancellationToken = default)
        {
            await DeleteAsync($"/api/stations/{keyDto.StationId}", cancellationToken);
        }
    }
}
