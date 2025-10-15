using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;

namespace GeneralReservationSystem.Application.Services.Interfaces
{
    public interface IStationService
    {
        Task<Station> GetStationAsync(int key, CancellationToken cancellationToken = default);
        Task<IEnumerable<Station>> GetAllStationsAsync(CancellationToken cancellationToken = default);
        Task<PagedResult<Station>> SearchStationsAsync(PagedSearchRequestDto searchDto, CancellationToken cancellationToken = default);
        Task<Station> CreateStationAsync(Station station, CancellationToken cancellationToken = default);
        Task<Station> UpdateStationAsync(Station station, CancellationToken cancellationToken = default);
        Task DeleteStationAsync(int key, CancellationToken cancellationToken = default);
    }
}
