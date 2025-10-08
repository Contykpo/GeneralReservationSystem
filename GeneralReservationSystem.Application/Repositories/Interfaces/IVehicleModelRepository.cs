using GeneralReservationSystem.Application.Entities;

namespace GeneralReservationSystem.Application.Repositories.Interfaces
{
    public interface IVehicleModelRepository : IRepository<VehicleModel> 
    {
        public Task<VehicleModel?> GetByNameAndManufacturerAsync(string name, string manufacturer, CancellationToken cancellationToken = default);
        public Task<IEnumerable<Seat>> GetSeatsByVehicleModelIdAsync(int vehicleModelId, CancellationToken cancellationToken = default);
    }

    /*public interface IVehicleModelRepository
    {
        Task<OptionalResult<PagedResult<VehicleModel>>> SearchPagedAsync(int pageIndex, int pageSize, string? name = null, string? manufacturer = null,
            VehicleModelSearchOrderBy? sortBy = null, bool descending = false);
        Task<OptionalResult<VehicleModel>> GetByIdAsync(int id);
        Task<OperationResult> AddAsync(VehicleModel vehicleModel, IEnumerable<Seat> seats);
        Task<OperationResult> UpdateAsync(VehicleModel vehicleModel);
        Task<OperationResult> DeleteAsync(int id);
    }*/
}
