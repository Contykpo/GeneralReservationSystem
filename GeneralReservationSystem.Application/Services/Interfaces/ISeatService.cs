using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.DTOs;

namespace GeneralReservationSystem.Application.Services.Interfaces
{
    public interface ISeatService
    {
        Task<OptionalResult<Seat>> GetSeatByIdAsync(int id);
        Task<OperationResult> AddSeatAsync(CreateSeatDto seatDto);
        Task<OperationResult> AddSeatsAsync(IEnumerable<CreateSeatDto> seatDtos);
        Task<OperationResult> UpdateSeatAsync(Seat seat);
        Task<OperationResult> UpdateSeatsAsync(IEnumerable<Seat> seats);
        Task<OperationResult> DeleteSeatAsync(int id);
        Task<OperationResult> DeleteSeatsAsync(IEnumerable<int> ids);
    }
}
