using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;

namespace GeneralReservationSystem.Application.Services.Interfaces
{
    public interface ITripService
    {
        Task<Trip> GetTripAsync(TripKeyDto keyDto, CancellationToken cancellationToken = default);
        Task<TripWithDetailsDto> GetTripWithDetailsAsync(TripKeyDto keyDto, CancellationToken cancellationToken = default);
        Task<IEnumerable<Trip>> GetAllTripsAsync(CancellationToken cancellationToken = default);
        Task<PagedResult<TripWithDetailsDto>> SearchTripsAsync(PagedSearchRequestDto searchDto, CancellationToken cancellationToken = default);
        Task<Trip> CreateTripAsync(CreateTripDto dto, CancellationToken cancellationToken = default);
        Task<Trip> UpdateTripAsync(UpdateTripDto dto, CancellationToken cancellationToken = default);
        Task DeleteTripAsync(TripKeyDto keyDto, CancellationToken cancellationToken = default);
        Task<IEnumerable<int>> GetFreeSeatsAsync(TripKeyDto keyDto, CancellationToken cancellationToken = default);
    }
}
