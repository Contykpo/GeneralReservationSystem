using GeneralReservationSystem.Application.Entities.Authentication;

namespace GeneralReservationSystem.Application.Repositories.Interfaces.Authentication
{
    public interface IRoleRepository : IRepository<ApplicationRole>
    {
        public Task<ApplicationRole?> GetByNameAsync(string roleName, CancellationToken cancellationToken = default);
    }

    /*public interface IRoleRepository
    {
        public Task<Guid> CreateRoleAsync(ApplicationRole role);
        public Task<ApplicationRole?> GetByGuidAsync(Guid guid);
        public Task<ApplicationRole?> GetByNameAsync(string roleName);
        public Task<int> UpdateRoleAsync(ApplicationRole role);
        public Task<int> DeleteRoleAsync(Guid roleId);
    }*/
}
