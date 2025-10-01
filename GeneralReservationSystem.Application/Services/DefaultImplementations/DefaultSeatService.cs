using GeneralReservationSystem.Application.Repositories.Interfaces;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.Services.Interfaces;
using GeneralReservationSystem.Application.DTOs;

namespace GeneralReservationSystem.Application.Services.DefaultImplementations
{
    public class DefaultSeatService : ISeatService
    {
        private readonly ISeatRepository _seatRepository;

        public DefaultSeatService(ISeatRepository seatRepository)
        {
            _seatRepository = seatRepository;
        }

        public Task<OptionalResult<Seat>> GetSeatByIdAsync(int id)
            => _seatRepository.GetByIdAsync(id);

        public Task<OperationResult> AddSeatAsync(CreateSeatDto seatDto)
        {
            var seat = new Seat
            {
                VehicleModelId = seatDto.VehicleModelId,
                SeatRow = seatDto.SeatRow,
                SeatColumn = seatDto.SeatColumn,
                IsAtWindow = seatDto.IsAtWindow,
                IsAtAisle = seatDto.IsAtAisle,
                IsInFront = seatDto.IsInFront,
                IsInBack = seatDto.IsInBack,
                IsAccessible = seatDto.IsAccessible
            };
            return _seatRepository.AddAsync(seat);
        }

        public Task<OperationResult> AddSeatsAsync(IEnumerable<CreateSeatDto> seatDtos)
        {
            var seats = seatDtos.Select(seatDto => new Seat
            {
                VehicleModelId = seatDto.VehicleModelId,
                SeatRow = seatDto.SeatRow,
                SeatColumn = seatDto.SeatColumn,
                IsAtWindow = seatDto.IsAtWindow,
                IsAtAisle = seatDto.IsAtAisle,
                IsInFront = seatDto.IsInFront,
                IsInBack = seatDto.IsInBack,
                IsAccessible = seatDto.IsAccessible
            });
            return _seatRepository.AddMultipleAsync(seats);
        }

        public Task<OperationResult> UpdateSeatAsync(UpdateSeatDto seatDto)
        {
            var seat = new Seat
            {
                SeatId = seatDto.Id,
                VehicleModelId = seatDto.VehicleModelId,
                SeatRow = seatDto.SeatRow,
                SeatColumn = seatDto.SeatColumn,
                IsAtWindow = seatDto.IsAtWindow,
                IsAtAisle = seatDto.IsAtAisle,
                IsInFront = seatDto.IsInFront,
                IsInBack = seatDto.IsInBack,
                IsAccessible = seatDto.IsAccessible
            };
            return _seatRepository.UpdateAsync(seat);
        }

        public Task<OperationResult> UpdateSeatsAsync(IEnumerable<UpdateSeatDto> seatDtos)
        {
            var seats = seatDtos.Select(seatDto => new Seat
            {
                SeatId = seatDto.Id,
                VehicleModelId = seatDto.VehicleModelId,
                SeatRow = seatDto.SeatRow,
                SeatColumn = seatDto.SeatColumn,
                IsAtWindow = seatDto.IsAtWindow,
                IsAtAisle = seatDto.IsAtAisle,
                IsInFront = seatDto.IsInFront,
                IsInBack = seatDto.IsInBack,
                IsAccessible = seatDto.IsAccessible
            });
            return _seatRepository.UpdateMultipleAsync(seats);
        }

        public Task<OperationResult> DeleteSeatAsync(int id)
            => _seatRepository.DeleteAsync(id);

        public Task<OperationResult> DeleteSeatsAsync(IEnumerable<int> ids)
            => _seatRepository.DeleteMultipleAsync(ids);
    }
}
