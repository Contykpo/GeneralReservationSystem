using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Web.Client.Helpers;
using GeneralReservationSystem.Web.Client.Services.Interfaces;
using System.Net.Http.Headers;

namespace GeneralReservationSystem.Web.Client.Services.Implementations
{
    public class ClientStationService(HttpClient httpClient) : ClientServiceBase(httpClient), IClientStationService
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
            return await PatchAsync<Station>($"/api/stations/{dto.StationId}", dto, cancellationToken);
        }

        public async Task DeleteStationAsync(StationKeyDto keyDto, CancellationToken cancellationToken = default)
        {
            await DeleteAsync($"/api/stations/{keyDto.StationId}", cancellationToken);
        }
        public async Task<ImportResult> ImportStationsFromCsvAsync(Stream csvStream, string fileName, CancellationToken cancellationToken = default)
        {
            MultipartFormDataContent content = [];
            StreamContent fileContent = new(csvStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
            content.Add(fileContent, "file", fileName);
            return await PostMultipartAsync<ImportResult>("/api/stations/import", content, cancellationToken);
        }

        public async Task<(byte[] FileContent, string FileName)> ExportStationsToCsvAsync(CancellationToken cancellationToken = default)
        {
            return await GetFileAsync("/api/stations/export", cancellationToken);
        }
    }
}
