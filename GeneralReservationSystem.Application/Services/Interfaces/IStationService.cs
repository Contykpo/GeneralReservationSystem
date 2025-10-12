using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;

namespace GeneralReservationSystem.Application.Services.Interfaces
{
    public interface IStationService
    {
        Task<Station> GetStationAsync(StationKeyDto keyDto, CancellationToken cancellationToken = default);
        Task<IEnumerable<Station>> GetAllStationsAsync(CancellationToken cancellationToken = default);
        Task<PagedResult<Station>> SearchStationsAsync(PagedSearchRequestDto searchDto, CancellationToken cancellationToken = default);
        Task<Station> CreateStationAsync(CreateStationDto dto, CancellationToken cancellationToken = default);
        Task<Station> UpdateStationAsync(UpdateStationDto dto, CancellationToken cancellationToken = default);
        Task DeleteStationAsync(StationKeyDto keyDto, CancellationToken cancellationToken = default);
    }
}
