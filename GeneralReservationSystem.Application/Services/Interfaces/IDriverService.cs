using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.DTOs;

namespace GeneralReservationSystem.Application.Services.Interfaces
{
    public interface IDriverService
    {
        Task<OptionalResult<IList<Driver>>> SearchDriversAsync(int pageIndex, int pageSize, string? firstName = null,
            string? lastName = null, string? licenseNumber = null, string? phoneNumber = null,
            Repositories.Interfaces.DriverSearchSortBy? sortBy = null, bool descending = false);
        Task<OptionalResult<Driver>> GetDriverByIdAsync(int id);
        Task<OptionalResult<Driver>> GetDriverByLicenseNumberAsync(string licenseNumber);
        Task<OptionalResult<Driver>> GetDriverByIdentificationNumberAsync(int identificationNumber);
        Task<OperationResult> AddDriverAsync(CreateDriverDto driverDto);
        Task<OperationResult> UpdateDriverAsync(Driver driver);
        Task<OperationResult> DeleteDriverAsync(int id);
    }
}
