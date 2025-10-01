using GeneralReservationSystem.Application.Repositories.Interfaces;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities.Authentication;
using System.Threading.Tasks;

namespace GeneralReservationSystem.Application.Services.Interfaces
{
    public interface IReservationService
    {
        Task<OptionalResult<PagedResult<AvailableSeatDto>>> GetAvailableSeatsAsync(int pageIndex, int pageSize, int tripId);
        Task<OptionalResult<PagedResult<SeatReservationDto>>> GetReservedSeatsForTripAsync(int pageIndex, int pageSize, int tripId);
        Task<OptionalResult<PagedResult<SeatReservationDto>>> GetReservedSeatsForUserAsync(int pageIndex, int pageSize, Guid userId, int? tripId);
        Task<OperationResult> AddReservationAsync(CreateReservationDto reservationDto);
        Task<OperationResult> DeleteReservationAsync(Reservation reservation);
    }
}
