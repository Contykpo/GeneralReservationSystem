using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;

namespace GeneralReservationSystem.Application.Repositories.Interfaces
{
    public interface ITripRepository : IRepository<Trip>
    {
        Task<Trip?> GetByIdAsync(int tripId, CancellationToken cancellationToken);
        Task<IEnumerable<int>> GetFreeSeatsAsync(int tripId, CancellationToken cancellationToken);
        Task<TripWithDetailsDto?> GetTripWithDetailsAsync(int tripId, CancellationToken cancellationToken);
        Task<PagedResult<TripWithDetailsDto>> SearchWithDetailsAsync(PagedSearchRequestDto searchDto, CancellationToken cancellationToken);
    }
}
