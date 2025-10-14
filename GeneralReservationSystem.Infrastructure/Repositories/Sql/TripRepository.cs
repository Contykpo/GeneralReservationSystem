using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using System.Data.Common;

namespace GeneralReservationSystem.Infrastructure.Repositories.Sql
{
    public class TripRepository(Func<DbConnection> connectionFactory, DbTransaction? transaction = null) : Repository<Trip>(connectionFactory, transaction), ITripRepository
    {
    }
}
