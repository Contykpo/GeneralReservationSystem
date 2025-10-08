using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.DTOs;

namespace GeneralReservationSystem.Application.Services.Interfaces
{
    public interface ISeatService
    {
        Task<Seat?> GetSeatByIdAsync(int id, CancellationToken cancellationToken = default);
        Task AddSeatAsync(CreateSeatDto seatDto, CancellationToken cancellationToken = default);
        Task<int> AddSeatsAsync(IEnumerable<CreateSeatDto> seatDtos, CancellationToken cancellationToken = default); // Returns number of seats added
        Task UpdateSeatAsync(UpdateSeatDto seatDto, CancellationToken cancellationToken = default);
        Task<int> UpdateSeatsAsync(IEnumerable<UpdateSeatDto> seatDtos, CancellationToken cancellationToken = default); // Returns number of seats updated
        Task DeleteSeatAsync(int id, CancellationToken cancellationToken = default);
        Task<int> DeleteSeatsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default); // Returns number of seats deleted
    }
}
