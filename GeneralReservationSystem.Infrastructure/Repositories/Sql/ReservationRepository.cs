using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using System.Data.Common;

namespace GeneralReservationSystem.Infrastructure.Repositories.Sql
{
    public class ReservationRepository(Func<DbConnection> connectionFactory, DbTransaction? transaction = null) : Repository<Reservation>(connectionFactory, transaction), IReservationRepository
    {
    }
}
