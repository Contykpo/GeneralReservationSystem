using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using GeneralReservationSystem.Application.Repositories.Util.Interfaces;
using System.Data.Common;

namespace GeneralReservationSystem.Infrastructure.Repositories.Sql
{
    public class TripRepository(RepositoryQueryProvider provider, Func<DbConnection> connectionFactory, DbTransaction? transaction = null) : Repository<Trip>(provider, connectionFactory, transaction), ITripRepository
    {
    }
}
