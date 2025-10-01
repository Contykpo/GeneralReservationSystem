using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GeneralReservationSystem.Application.Repositories.Interfaces
{
    public enum DriverSearchSortBy
    {
        FirstName,
        LastName,
        LicenseNumber
    }

    public interface IDriverRepository
    {
        Task<OptionalResult<PagedResult<Driver>>> SearchPagedAsync(int pageIndex, int pageSize, string? firstName = null,
            string? lastName = null, string? licenseNumber = null, string? phoneNumber = null,
            DriverSearchSortBy? sortBy = null, bool descending = false);
        Task<OptionalResult<Driver>> GetByIdAsync(int id);
        Task<OptionalResult<Driver>> GetByLicenseNumberAsync(string licenseNumber);
        Task<OptionalResult<Driver>> GetByIdentificationNumberAsync(int identificationNumber);
        Task<OptionalResult<IList<Trip>>> GetTripsByDriverIdAsync(int id);
        Task<OperationResult> AddAsync(Driver driver);
        Task<OperationResult> UpdateAsync(Driver driver);
        Task<OperationResult> DeleteAsync(int id);
    }
}
