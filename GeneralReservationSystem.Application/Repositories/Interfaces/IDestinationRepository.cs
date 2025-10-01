using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GeneralReservationSystem.Application.Repositories.Interfaces
{
    public enum DestinationSearchSortBy
    {
        Name,
        Code,
        City,
        Region,
        Country
    }

    public interface IDestinationRepository
    {
        Task<OptionalResult<PagedResult<Destination>>> SearchPagedAsync(int pageIndex, int pageSize, string? name = null, string? code = null,
            string? city = null, string? region = null, string? country = null, DestinationSearchSortBy? sortBy = null, bool descending = false);
        Task<OptionalResult<Destination>> GetByIdAsync(int id);
        Task<OperationResult> AddAsync(Destination destination);
        Task<OperationResult> UpdateAsync(Destination destination);
        Task<OperationResult> DeleteAsync(int id);
    }
}
