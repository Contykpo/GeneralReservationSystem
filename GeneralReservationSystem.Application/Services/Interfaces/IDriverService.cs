using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.DTOs;

namespace GeneralReservationSystem.Application.Services.Interfaces
{
    public interface IDriverService
    {
        Task<PagedResult<Driver>> SearchDriversAsync(DriverSearchRequestDto driverSearchRequestDto, CancellationToken cancellationToken = default);
        Task<Driver?> GetDriverByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Driver?> GetDriverByLicenseNumberAsync(string licenseNumber, CancellationToken cancellationToken = default);
        Task<Driver?> GetDriverByIdentificationNumberAsync(int identificationNumber, CancellationToken cancellationToken = default);
        Task AddDriverAsync(CreateDriverDto driverDto, CancellationToken cancellationToken = default);
        Task UpdateDriverAsync(UpdateDriverDto driverDto, CancellationToken cancellationToken = default);
        Task DeleteDriverAsync(int id, CancellationToken cancellationToken = default);
    }
}
