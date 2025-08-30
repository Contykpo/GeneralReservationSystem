using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.Design;
using System.Data;


namespace GeneralReservationSystem.Infrastructure
{
    public class DbConnectionHelper
    {
        private readonly string _connectionString;

        public DbConnectionHelper(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<List<string>> GetUsersAsync()
        {
            var users = new List<string>();
            
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT Username FROM Users;", connection);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                users.Add(reader.GetString(0));
            }

            return users;
        }

        public async Task<int> ExecuteAsync(string sql, Dictionary<string, object>? parameters = null)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    command.Parameters.AddWithValue(parameter.Key, parameter.Value);
                }
            }

            await connection.OpenAsync();
            
            return await command.ExecuteNonQueryAsync();
        }
    }
}