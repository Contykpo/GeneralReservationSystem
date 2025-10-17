using GeneralReservationSystem.Application.Repositories.Util.Interfaces;

namespace GeneralReservationSystem.Application.Repositories.Interfaces
{
    public interface IRepository
    {
    }

    public interface IRepository<T> : IRepository where T : class, new()
    {
        Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
        RepositoryQuery<T> Query(); // GetByIdAsync can be implemented via RepositoryQuery().Where(...).FirstOrDefaultAsync()
        Task<int> CreateAsync(T entity, CancellationToken cancellationToken = default); // Returns the number of affected rows
        Task<int> CreateBulkAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default); // Returns the number of affected rows
        Task<int> UpdateAsync(T entity, Func<T, object?>? selector = null, CancellationToken cancellationToken = default); // Returns the number of affected rows
        Task<int> UpdateBulkAsync(IEnumerable<T> entities, Func<T, object?>? selector = null, CancellationToken cancellationToken = default); // Returns the number of affected rows
        Task<int> DeleteAsync(T entity, CancellationToken cancellationToken = default); // Returns the number of affected rows
        Task<int> DeleteBulkAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default); // Returns the number of affected rows
    }
}