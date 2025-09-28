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
        Task<OptionalResult<IList<VehicleModel>>> SearchAsync(int pageIndex, int pageSize, string? name = null, string? manufacturer = null,
            VehicleModelSearchSortBy? sortBy = null, bool descending = false); // NOTE: Ideally, search should be in a service layer, but 
                                                                               // we don't use an ORM. This is simpler.
        Task<OptionalResult<VehicleModel>> GetByIdAsync(int id);
        Task<OperationResult> AddAsync(VehicleModel vehicleModel, IEnumerable<Seat> seats); // NOTE: Ideally, this should be in a service
                                                                                            // layer to handle transactions, but we don't
                                                                                            // use an ORM. This is simpler.
        Task<OperationResult> UpdateAsync(VehicleModel vehicleModel);
        Task<OperationResult> DeleteAsync(int id);
    }
}
