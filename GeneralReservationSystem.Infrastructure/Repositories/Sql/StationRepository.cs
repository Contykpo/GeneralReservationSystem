using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using System.Data.Common;

namespace GeneralReservationSystem.Infrastructure.Repositories.Sql
{
    public class StationRepository(Func<DbConnection> connectionFactory, DbTransaction? transaction = null) : Repository<Station>(connectionFactory, transaction), IStationRepository
    {
    }
}
