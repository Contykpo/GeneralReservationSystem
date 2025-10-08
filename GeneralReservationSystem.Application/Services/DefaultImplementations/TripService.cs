using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using GeneralReservationSystem.Application.Services.Interfaces;

namespace GeneralReservationSystem.Application.Services.DefaultImplementations
{
    public class TripService : ITripService
    {
        private readonly ITripRepository _tripRepository;
        private readonly IVehicleRepository _vehicleRepository;
        private readonly IDriverRepository _driverRepository;

        public TripService(ITripRepository tripRepository, IVehicleRepository vehicleRepository, IDriverRepository driverRepository)
        {
            _tripRepository = tripRepository;
            _vehicleRepository = vehicleRepository;
            _driverRepository = driverRepository;
        }

        public Task AddAsync(CreateTripDto tripDto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Trip?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<PagedResult<TripDetailsDto>> SearchPagedAsync(TripDetailsSearchRequestDto tripDetailsSearchRequestDto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(UpdateTripDto tripDto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
