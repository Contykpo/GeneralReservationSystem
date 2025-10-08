using GeneralReservationSystem.Application.Entities.Authentication;
using GeneralReservationSystem.Application.Repositories.Interfaces.Authentication;
using Microsoft.Extensions.Logging;
using System.Data.Common;

namespace GeneralReservationSystem.Infrastructure.Repositories.Sql.Authentication
{
    public class RoleRepository : Repository<ApplicationRole>, IRoleRepository
    {
        public RoleRepository(Func<DbConnection> connectionFactory, DbTransaction? transaction = null)
            : base(connectionFactory, transaction) { }

        public Task<ApplicationRole?> GetByNameAsync(string roleName, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}