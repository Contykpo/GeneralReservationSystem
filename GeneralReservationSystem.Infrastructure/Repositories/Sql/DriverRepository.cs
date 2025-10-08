using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using System.Data.Common;

namespace GeneralReservationSystem.Infrastructure.Repositories.Sql
{
    public class DriverRepository : Repository<Driver>, IDriverRepository
    {
        public DriverRepository(Func<DbConnection> connectionFactory, DbTransaction? transaction = null)
            : base(connectionFactory, transaction) { }
    }
}
