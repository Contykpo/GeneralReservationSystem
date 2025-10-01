using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using GeneralReservationSystem.Application.Services.Interfaces;

namespace GeneralReservationSystem.Application.Services.DefaultImplementations
{
    public class DefaultDriverService : IDriverService
    {
        private readonly IDriverRepository _driverRepository;

        public DefaultDriverService(IDriverRepository driverRepository)
        {
            _driverRepository = driverRepository;
        }

        public async Task<OptionalResult<PagedResult<Driver>>> SearchDriversAsync(int pageIndex, int pageSize, string? firstName = null,
            string? lastName = null, string? licenseNumber = null, string? phoneNumber = null,
            DriverSearchSortBy? sortBy = null, bool descending = false)
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
                throw new ArgumentException("El número de identificación debe ser un número válido de 8 dígitos.");

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

        public Task<OperationResult> UpdateDriverAsync(UpdateDriverDto driverDto)
        {
            if (!int.TryParse(driverDto.IdentificationNumber, out int identificationNumber))
                throw new ArgumentException("El número de identificación debe ser un número válido de 8 dígitos.");

            var driver = new Driver
            {
                DriverId = driverDto.Id,
                IdentificationNumber = identificationNumber,
                FirstName = driverDto.FirstName,
                LastName = driverDto.LastName,
                LicenseNumber = driverDto.LicenseNumber,
                LicenseExpiryDate = driverDto.LicenseExpiryDate
            };
            return _driverRepository.UpdateAsync(driver);
        }

        public Task<OperationResult> DeleteDriverAsync(int id)
            => _driverRepository.DeleteAsync(id);
    }
}
