using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using GeneralReservationSystem.Application.Services.Interfaces;

namespace GeneralReservationSystem.Application.Services.DefaultImplementations
{
    public class DriverService : IDriverService
    {
        private readonly IDriverRepository _driverRepository;

        public DriverService(IDriverRepository driverRepository)
        {
            _driverRepository = driverRepository;
        }

        public Task AddDriverAsync(CreateDriverDto driverDto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task DeleteDriverAsync(int id, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Driver?> GetDriverByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Driver?> GetDriverByIdentificationNumberAsync(int identificationNumber, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Driver?> GetDriverByLicenseNumberAsync(string licenseNumber, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<PagedResult<Driver>> SearchDriversAsync(DriverSearchRequestDto driverSearchRequestDto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task UpdateDriverAsync(UpdateDriverDto driverDto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
