using GeneralReservationSystem.Application.Entities;

namespace GeneralReservationSystem.Application.Repositories.Interfaces
{
    public interface IDriverRepository : IRepository<Driver> { }

    /*public interface IDriverRepository
    {
        Task<OptionalResult<PagedResult<Driver>>> SearchPagedAsync(int pageIndex, int pageSize, string? firstName = null,
            string? lastName = null, string? licenseNumber = null, string? phoneNumber = null,
            DriverSearchOrderBy? OrderBy = null, bool descending = false);
        Task<OptionalResult<Driver>> GetByIdAsync(int id);
        Task<OptionalResult<Driver>> GetByLicenseNumberAsync(string licenseNumber);
        Task<OptionalResult<Driver>> GetByIdentificationNumberAsync(int identificationNumber);
        Task<OptionalResult<IList<Trip>>> GetTripsByDriverIdAsync(int id);
        Task<OperationResult> AddAsync(Driver driver);
        Task<OperationResult> UpdateAsync(Driver driver);
        Task<OperationResult> DeleteAsync(int id);
    }*/
}
