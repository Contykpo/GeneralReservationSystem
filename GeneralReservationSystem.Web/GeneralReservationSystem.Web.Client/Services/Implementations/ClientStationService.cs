using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Web.Client.Services.Interfaces;
using System.Net.Http.Headers;

namespace GeneralReservationSystem.Web.Client.Services.Implementations
{
    public class ClientStationService(HttpClient httpClient) : StationService(httpClient), IClientStationService
    {
        public async Task<ImportResult> ImportStationsFromCsvAsync(Stream csvStream, string fileName, CancellationToken cancellationToken = default)
        {
            MultipartFormDataContent content = [];
            StreamContent fileContent = new(csvStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
            content.Add(fileContent, "file", fileName);
            return await PostMultipartAsync<ImportResult>("/api/stations/import", content, cancellationToken);
        }
    }
}