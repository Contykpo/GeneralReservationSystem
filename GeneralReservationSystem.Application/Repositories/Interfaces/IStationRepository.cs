using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;

namespace GeneralReservationSystem.Application.Repositories.Interfaces
{
    public interface IStationRepository : IRepository<Station>
    {
        Task<Station?> GetByIdAsync(int stationId, CancellationToken cancellationToken = default);

        Task<PagedResult<Station>> SearchAsync(PagedSearchRequestDto searchDto, CancellationToken cancellationToken = default);
    }
}
