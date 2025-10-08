using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.DTOs;

namespace GeneralReservationSystem.Application.Services.Interfaces
{
    public interface IDestinationService
    {
        Task<PagedResult<Destination>> SearchDestinationsAsync(DestinationSearchRequestDto destinationSearchRequestDto, CancellationToken cancellationToken = default);
        Task<Destination?> GetDestinationByIdAsync(int id, CancellationToken cancellationToken = default);
        Task AddDestinationAsync(CreateDestinationDto destinationDto, CancellationToken cancellationToken = default);
        Task UpdateDestinationAsync(UpdateDestinationDto destinationDto, CancellationToken cancellationToken = default);
        Task DeleteDestinationAsync(int id, CancellationToken cancellationToken = default);
    }
}
