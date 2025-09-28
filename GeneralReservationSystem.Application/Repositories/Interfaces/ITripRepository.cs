using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;

namespace GeneralReservationSystem.Application.Repositories.Interfaces
{
    public enum TripSearchSortBy
    {
        DepartureName,
        DepartureCity,
        DestinationName,
        DestinationCity,
        StartDate,
        EndDate
    }

    public interface ITripRepository
    {
        Task<OptionalResult<Trip>> GetByIdAsync(int id);
        Task<SearchResult<IList<TripDetailsDto>>> SearchPagedAsync(int pageIndex, int pageSize,
            string? DepartureName = null, string? DepartureCity = null, string? destinationName = null, string? destinationCity = null,
            DateTime? startDate = null, DateTime? endDate = null, bool onlyWithAvailableSeats = true, TripSearchSortBy? sortBy = null,
            bool descending = false); // NOTE: Ideally, search should be in a service layer, but 
                                      // we don't use an ORM. This is simpler.
        Task<OptionalResult<Driver>> GetDriverByIdAsync(int id);
        Task<OptionalResult<Vehicle>> GetVehicleByIdAsync(int id);
        Task<OptionalResult<Destination>> GetDepartureByIdAsync(int id);
        Task<OptionalResult<Destination>> GetDestinationByIdAsync(int id);
        Task<OperationResult> AddAsync(Trip trip);
        Task<OperationResult> UpdateAsync(Trip trip);
        Task<OperationResult> DeleteAsync(int id);
    }
}
