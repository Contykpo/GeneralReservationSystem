using GeneralReservationSystem.Application.Entities.Authentication;

namespace GeneralReservationSystem.Application.Repositories.Interfaces.Authentication
{
    public interface IUserRepository : IRepository<ApplicationUser>
    {
        public Task<ApplicationRole?> GetByNameAsync(string roleName, CancellationToken cancellationToken = default);
        public Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
        public Task<IEnumerable<ApplicationUser>> GetWithRoleAsync(string roleName, CancellationToken cancellationToken = default);
        public Task<bool> ExistsWithEmailAsync(string email, CancellationToken cancellationToken = default);
		public Task<IEnumerable<ApplicationRole>> GetUserRolesAsync(Guid userGuid, CancellationToken cancellationToken = default);
    }

    /*public interface IUserRepository
	{
		public Task<ApplicationUser?> GetByGuidAsync(Guid guid);
		public Task<ApplicationUser?> GetByEmailAsync(string email);
		public Task<bool> ExistsWithEmailAsync(string email);
		public Task<IEnumerable<ApplicationUser>> GetWithRoleAsync(string roleName);
		public Task<IEnumerable<ApplicationUser>> GetUserRolesAsync(Guid userGuid);
		public Task<IEnumerable<ApplicationUser>> GetAllAsync();
		public Task<IEnumerable<ApplicationUser>> FilterAsync(Func<ApplicationUser, bool> predicate);
		public Task<Guid> CreateUserAsync(ApplicationUser newUser, IEnumerable<ApplicationRole> userRoles);
		public Task<int> UpdateUserAsync(ApplicationUser user);
		public Task<int> AddRoleAsync(Guid userGuid, string roleName);
		public Task<int> RemoveRoleAsync(Guid userGuid, string roleName);
	}*/
}