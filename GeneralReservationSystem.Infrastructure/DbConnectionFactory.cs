using Microsoft.Extensions.Configuration;
using System.Data.Common;

namespace GeneralReservationSystem.Infrastructure
{
    public static class DbConnectionFactory
    {
        public static Func<DbConnection> CreateFactory<TConnection>(
            IConfiguration config,
            string connectionStringName)
            where TConnection : DbConnection, new()
        {
            string? connectionString = config.GetConnectionString(connectionStringName);

            return () =>
            {
                TConnection conn = new()
                {
                    ConnectionString = connectionString
                };
                conn.Open();
                return conn;
            };
        }
    }
}
