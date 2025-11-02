using GeneralReservationSystem.Infrastructure.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace GeneralReservationSystem.Tests.Integration;

public class CustomWebApplicationFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _ = builder.ConfigureAppConfiguration((context, config) =>
        {
            _ = config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString,
                ["Jwt:SecretKey"] = "TestSecretKeyThatIsAtLeast32CharactersLong!",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience",
                ["Jwt:ExpirationDays"] = "7",
                ["CorsOrigins"] = "http://localhost:5000"
            });
        });

        _ = builder.ConfigureTestServices(services =>
        {
        });

        _ = builder.ConfigureLogging(logging =>
        {
            _ = logging.ClearProviders();
            _ = logging.AddConsole();
            _ = logging.SetMinimumLevel(LogLevel.Warning);
        });
    }

    public async Task InitializeDatabaseAsync()
    {
        await Task.Run(() =>
        {
            using NpgsqlConnection connection = new(connectionString);
            connection.Open();

            using NpgsqlCommand command = connection.CreateCommand();
            command.CommandText = "CREATE SCHEMA IF NOT EXISTS grsdb";
            _ = command.ExecuteNonQuery();

            MigrationsRunner.RunMigrations(connectionString);
        });
    }
}
