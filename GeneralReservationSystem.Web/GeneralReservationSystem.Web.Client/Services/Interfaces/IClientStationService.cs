using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.Services.Interfaces;

namespace GeneralReservationSystem.Web.Client.Services.Interfaces
{
    public interface IClientStationService : IStationService
    {
        Task<ImportResult> ImportStationsFromCsvAsync(Stream csvStream, string fileName, CancellationToken cancellationToken = default);
    }
}
