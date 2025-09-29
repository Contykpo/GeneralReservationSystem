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
        Task<OptionalResult<IList<AvailableSeatDto>>> GetAvailableSeatsAsync(int pageIndex, int pageSize, int tripId);
        Task<OptionalResult<IList<SeatReservationDto>>> GetReservedSeatsForTripAsync(int pageIndex, int pageSize, int tripId);
        Task<OptionalResult<IList<SeatReservationDto>>> GetReservedSeatsForUserAsync(int pageIndex, int pageSize, int userId, int? tripId);
        Task<OperationResult> AddReservationAsync(CreateReservationDto reservationDto);
        Task<OperationResult> DeleteReservationAsync(Reservation reservation);
    }
}
