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
        Task<OperationResult> UpdateSeatAsync(UpdateSeatDto seatDto);
        Task<OperationResult> UpdateSeatsAsync(IEnumerable<UpdateSeatDto> seatDtos);
        Task<OperationResult> DeleteSeatAsync(int id);
        Task<OperationResult> DeleteSeatsAsync(IEnumerable<int> ids);
    }
}
