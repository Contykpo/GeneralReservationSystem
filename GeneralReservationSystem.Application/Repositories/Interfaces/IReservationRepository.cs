using GeneralReservationSystem.Application.Entities;

namespace GeneralReservationSystem.Application.Repositories.Interfaces
{
    public interface IReservationRepository : IRepository<Reservation> { }

    /*public interface IReservationRepository
    {
        Task<OptionalResult<PagedResult<AvailableSeatDto>>> GetAvailablePagedAsync(int pageIndex, int pageSize, int tripId);
        Task<OptionalResult<PagedResult<SeatReservationDto>>> GetReservedSeatsForTripPagedAsync(int pageIndex, int pageSize, int tripId);
        Task<OptionalResult<PagedResult<SeatReservationDto>>> GetReservedSeatsForUserPagedAsync(int pageIndex, int pageSize, Guid userId, int? tripId);
        Task<OperationResult> AddAsync(Reservation reservation);
        Task<OperationResult> DeleteAsync(Reservation reservation);
    }*/
}
