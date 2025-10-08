using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using GeneralReservationSystem.Application.Services.Interfaces;

namespace GeneralReservationSystem.Application.Services.DefaultImplementations
{
    public class SeatService : ISeatService
    {
        private readonly ISeatRepository _seatRepository;

        public SeatService(ISeatRepository seatRepository)
        {
            _seatRepository = seatRepository;
        }

        public Task AddSeatAsync(CreateSeatDto seatDto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<int> AddSeatsAsync(IEnumerable<CreateSeatDto> seatDtos, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task DeleteSeatAsync(int id, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<int> DeleteSeatsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Seat?> GetSeatByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task UpdateSeatAsync(UpdateSeatDto seatDto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<int> UpdateSeatsAsync(IEnumerable<UpdateSeatDto> seatDtos, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
