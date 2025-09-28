using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Entities.Authentication;

namespace GeneralReservationSystem.Application.Repositories.Interfaces
{
    public interface IReservationRepository
    {
        Task<OptionalResult<IList<AvailableSeatDto>>> GetAvailablePagedAsync(int pageIndex, int pageSize,
            int tripId); // NOTE: Ideally, search should be in a service layer, but 
                         // we don't use an ORM. This is simpler.

        Task<OptionalResult<IList<SeatReservationDto>>> GetReservedSeatsForTripPagedAsync(int pageIndex, int pageSize, int tripId); 
        // NOTE: Ideally, search should be in a service layer, but we don't use an ORM. This is simpler.
        Task<OptionalResult<IList<SeatReservationDto>>> GetReservedSeatsForUserPagedAsync(int pageIndex, int pageSize, int userId, int? tripId);
        // NOTE: Ideally, search should be in a service layer, but we don't use an ORM. This is simpler.
        Task<OptionalResult<Seat>> GetSeatByIdAsync(Reservation reservation);
        Task<OptionalResult<Trip>> GetTripByIdAsync(Reservation reservation);
        Task<OptionalResult<ApplicationUser>> GetUserByIdAsync(Reservation reservation);
        Task<OperationResult> AddAsync(Reservation reservation);
        Task<OperationResult> DeleteAsync(Reservation reservation);
    }
}
