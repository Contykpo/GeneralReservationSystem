using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Entities.Authentication;

namespace GeneralReservationSystem.Application.Repositories.Interfaces
{
    public interface IReservationRepository
    {
        Task<OptionalResult<IList<AvailableSeatDto>>> GetAvailablePagedAsync(int pageIndex, int pageSize, int tripId);
        Task<OptionalResult<IList<SeatReservationDto>>> GetReservedSeatsForTripPagedAsync(int pageIndex, int pageSize, int tripId);
        Task<OptionalResult<IList<SeatReservationDto>>> GetReservedSeatsForUserPagedAsync(int pageIndex, int pageSize, int userId, int? tripId);
        Task<OperationResult> AddAsync(Reservation reservation);
        Task<OperationResult> DeleteAsync(Reservation reservation);
    }
}
