using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.Entities;

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
        Task<OptionalResult<IList<Driver>>> GetAllAsync();
        Task<SearchResult<IList<Driver>>> SearchPagedAsync(int pageIndex, int pageSize, string? firstName = null, // NOTE: Ideally, search should be in a service layer, but 
            string? lastName = null, string? licenseNumber = null, string? phoneNumber = null,                      // we don't use an ORM. This is simpler.
            DriverSearchSortBy? sortBy = null, bool descending = false);
        Task<OptionalResult<Driver>> GetByIdAsync(int id);
        Task<OptionalResult<Driver>> GetByLicenseNumberAsync(string licenseNumber);
        Task<OptionalResult<Driver>> GetByIdentificationNumberAsync(int identificationNumber);
        Task<OperationResult> AddAsync(Driver driver);
        Task<OperationResult> UpdateAsync(Driver driver);
        Task<OperationResult> DeleteAsync(int id);
    }
}
