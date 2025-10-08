using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using System.Data.Common;

namespace GeneralReservationSystem.Infrastructure.Repositories.Sql
{
    public class ReservationRepository : Repository<Reservation>, IReservationRepository
    {
        public ReservationRepository(Func<DbConnection> connectionFactory, DbTransaction? transaction = null)
            : base(connectionFactory, transaction) { }
    }
}
