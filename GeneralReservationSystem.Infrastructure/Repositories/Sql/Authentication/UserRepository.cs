using GeneralReservationSystem.Application.Entities.Authentication;
using GeneralReservationSystem.Application.Repositories.Interfaces.Authentication;
using GeneralReservationSystem.Application.Repositories.Util.Interfaces;
using Microsoft.Extensions.Logging;
using System.Data.Common;

namespace GeneralReservationSystem.Infrastructure.Repositories.Sql.Authentication
{
    public class UserRepository : Repository<ApplicationUser>, IUserRepository
    {
        public UserRepository(Func<DbConnection> connectionFactory, DbTransaction? transaction = null)
            : base(connectionFactory, transaction) { }

        public Task<bool> ExistsWithEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ApplicationRole?> GetByNameAsync(string roleName, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<ApplicationRole>> GetUserRolesAsync(Guid userGuid, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<ApplicationUser>> GetWithRoleAsync(string roleName, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
