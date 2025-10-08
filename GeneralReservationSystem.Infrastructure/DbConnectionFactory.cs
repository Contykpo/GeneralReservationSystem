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
            var connectionString = config.GetConnectionString(connectionStringName);

            return () =>
            {
                var conn = new TConnection
                {
                    ConnectionString = connectionString
                };
                conn.Open();
                return conn;
            };
        }
    }
}
