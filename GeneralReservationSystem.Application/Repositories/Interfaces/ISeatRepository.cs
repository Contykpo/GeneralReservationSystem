using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.Entities;

namespace GeneralReservationSystem.Application.Repositories.Interfaces
{
    public interface ISeatRepository : IRepository<Seat> { }

    /*public interface ISeatRepository
    {
        Task<OptionalResult<Seat>> GetByIdAsync(int id);
        Task<OperationResult> AddAsync(Seat seat);
        Task<OperationResult> AddMultipleAsync(IEnumerable<Seat> seats);
        Task<OperationResult> UpdateAsync(Seat seat);
        Task<OperationResult> UpdateMultipleAsync(IEnumerable<Seat> seats);
        Task<OperationResult> DeleteAsync(int id);
        Task<OperationResult> DeleteMultipleAsync(IEnumerable<int> ids);
    }*/
}
