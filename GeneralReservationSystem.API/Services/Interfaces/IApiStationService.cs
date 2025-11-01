using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Services.Interfaces;

namespace GeneralReservationSystem.API.Services.Interfaces
{
    public interface IApiStationService : IStationService
    {
        Task<int> CreateStationsBulkAsync(IEnumerable<ImportStationDto> stations, CancellationToken cancellationToken = default);
    }
}
