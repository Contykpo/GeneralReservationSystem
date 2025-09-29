using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.DTOs;

namespace GeneralReservationSystem.Application.Services.Interfaces
{
    public interface IDestinationService
    {
        Task<OptionalResult<IList<Destination>>> SearchDestinationsAsync(int pageIndex, int pageSize, string? name = null, string? code = null,
            string? city = null, string? region = null, string? country = null, Repositories.Interfaces.DestinationSearchSortBy? sortBy = null, bool descending = false);
        Task<OptionalResult<Destination>> GetDestinationByIdAsync(int id);
        Task<OperationResult> AddDestinationAsync(CreateDestinationDto destinationDto);
        Task<OperationResult> UpdateDestinationAsync(Destination destination);
        Task<OperationResult> DeleteDestinationAsync(int id);
    }
}
