using GeneralReservationSystem.Application.Entities.Authentication;
using GeneralReservationSystem.Application.Repositories.Interfaces.Authentication;
using System.Data.Common;

namespace GeneralReservationSystem.Infrastructure.Repositories.Sql.Authentication
{
    public class UserRepository(Func<DbConnection> connectionFactory, DbTransaction? transaction = null) : Repository<User>(connectionFactory, transaction), IUserRepository
    {
    }
}
