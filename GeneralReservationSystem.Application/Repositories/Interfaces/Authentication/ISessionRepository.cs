using GeneralReservationSystem.Application.Entities.Authentication;

namespace GeneralReservationSystem.Application.Repositories.Interfaces.Authentication
{
    public interface ISessionRepository : IRepository<UserSession>
    {
        public Task<UserSession?> GetByIdAsync(Guid id);
        public Task<(UserSession session, ApplicationUser user)?> GetSessionWithUserAsync(Guid sessionId, CancellationToken cancellationToken = default);
        public Task<IEnumerable<UserSession>> GetAllSessionsForUserAsync(Guid userId, CancellationToken cancellationToken = default);
        public Task<IEnumerable<UserSession>> GetActiveSessionsForUserAsync(Guid userId, CancellationToken cancellationToken = default);
        public Task<int> RevokeSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
        public Task<int> RevokeAllSessionsAsync(Guid userId, CancellationToken cancellationToken = default);
    }

    /*public interface ISessionRepository
	{
		public Task<(UserSession session, ApplicationUser user)?> GetSessionAsync(Guid sessionId);
		public Task<IEnumerable<UserSession>> GetAllSessionsForUserAsync(Guid userId);
		public Task<IEnumerable<UserSession>> GetActiveSessionsForUserAsync(Guid userId);
		public Task<Guid> CreateSessionAsync(UserSession newSession);
		public Task<int> UpdateSessionAsync(UserSession session);
		public Task<int> RevokeSessionAsync(Guid sessionId);
		public Task<int> RevokeAllSessionsAsync(Guid userId);
	}*/
}