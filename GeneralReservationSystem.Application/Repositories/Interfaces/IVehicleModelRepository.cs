using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.Entities;

namespace GeneralReservationSystem.Application.Repositories.Interfaces
{
    public enum VehicleModelSearchSortBy
    {
        Name,
        Manufacturer
    }

    public interface IVehicleModelRepository
    {
        Task<OptionalResult<IList<VehicleModel>>> SearchPagedAsync(int pageIndex, int pageSize, string? name = null, string? manufacturer = null,
            VehicleModelSearchSortBy? sortBy = null, bool descending = false);
        Task<OptionalResult<VehicleModel>> GetByIdAsync(int id);
        Task<OperationResult> AddAsync(VehicleModel vehicleModel, IEnumerable<Seat> seats);
        Task<OperationResult> UpdateAsync(VehicleModel vehicleModel);
        Task<OperationResult> DeleteAsync(int id);
    }
}
