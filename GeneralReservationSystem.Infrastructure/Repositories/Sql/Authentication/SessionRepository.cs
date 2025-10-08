using GeneralReservationSystem.Application.Entities.Authentication;
using GeneralReservationSystem.Application.Repositories.Interfaces.Authentication;
using Microsoft.Extensions.Logging;
using System.Data.Common;

namespace GeneralReservationSystem.Infrastructure.Repositories.Sql.Authentication
{
    public class SessionRepository : Repository<UserSession>, ISessionRepository
    {
        public SessionRepository(Func<DbConnection> connectionFactory, DbTransaction? transaction = null)
            : base(connectionFactory, transaction) { }

        public Task<UserSession?> GetByIdAsync(Guid id)
        {
            return Query()
                .Where(s => s.SessionId == id)
                .FirstOrDefaultAsync();
        }

        public Task<IEnumerable<UserSession>> GetActiveSessionsForUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<UserSession>> GetAllSessionsForUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<(UserSession session, ApplicationUser user)?> GetSessionWithUserAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<int> RevokeAllSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<int> RevokeSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}