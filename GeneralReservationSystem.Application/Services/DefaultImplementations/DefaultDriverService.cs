using System.Collections.Generic;
using System.Threading.Tasks;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.Services.Interfaces;
using GeneralReservationSystem.Application.DTOs;

namespace GeneralReservationSystem.Application.Services
{
    public class DefaultDriverService : IDriverService
    {
        private readonly IDriverRepository _driverRepository;

        public DefaultDriverService(IDriverRepository driverRepository)
        {
            _driverRepository = driverRepository;
        }

        public async Task<OptionalResult<IList<Driver>>> SearchDriversAsync(int pageIndex, int pageSize, string? firstName = null,
            string? lastName = null, string? licenseNumber = null, string? phoneNumber = null,
            GeneralReservationSystem.Application.Repositories.Interfaces.DriverSearchSortBy? sortBy = null, bool descending = false)
        {
            return await _driverRepository.SearchPagedAsync(pageIndex, pageSize, firstName, lastName, licenseNumber, phoneNumber, sortBy, descending);
        }

        public Task<OptionalResult<Driver>> GetDriverByIdAsync(int id)
            => _driverRepository.GetByIdAsync(id);

        public Task<OptionalResult<Driver>> GetDriverByLicenseNumberAsync(string licenseNumber)
            => _driverRepository.GetByLicenseNumberAsync(licenseNumber);

        public Task<OptionalResult<Driver>> GetDriverByIdentificationNumberAsync(int identificationNumber)
            => _driverRepository.GetByIdentificationNumberAsync(identificationNumber);

        public Task<OperationResult> AddDriverAsync(CreateDriverDto driverDto)
        {
            if (!int.TryParse(driverDto.IdentificationNumber, out int identificationNumber))
                throw new ArgumentException("IdentificationNumber must be a valid 8-digit number.");

            var driver = new Driver
            {
                IdentificationNumber = identificationNumber,
                FirstName = driverDto.FirstName,
                LastName = driverDto.LastName,
                LicenseNumber = driverDto.LicenseNumber,
                LicenseExpiryDate = driverDto.LicenseExpiryDate
            };
            return _driverRepository.AddAsync(driver);
        }

        public Task<OperationResult> UpdateDriverAsync(Driver driver)
            => _driverRepository.UpdateAsync(driver);

        public Task<OperationResult> DeleteDriverAsync(int id)
            => _driverRepository.DeleteAsync(id);
    }
}
