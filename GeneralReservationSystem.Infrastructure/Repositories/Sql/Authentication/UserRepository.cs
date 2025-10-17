using GeneralReservationSystem.Application.Entities.Authentication;
using GeneralReservationSystem.Application.Repositories.Interfaces.Authentication;
using GeneralReservationSystem.Application.Repositories.Util.Interfaces;
using System.Data.Common;

namespace GeneralReservationSystem.Infrastructure.Repositories.Sql.Authentication
{
    public class UserRepository(RepositoryQueryProvider provider, Func<DbConnection> connectionFactory, DbTransaction? transaction = null) : Repository<User>(provider, connectionFactory, transaction), IUserRepository
    {
    }
}
