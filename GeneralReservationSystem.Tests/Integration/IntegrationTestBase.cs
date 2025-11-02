using Testcontainers.PostgreSql;

namespace GeneralReservationSystem.Tests.Integration;

public abstract class IntegrationTestBase : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    protected string ConnectionString => _postgresContainer.GetConnectionString();

    protected IntegrationTestBase()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("grsdb")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        await InitializeDatabaseAsync();
    }

    public virtual async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
    }

    protected virtual Task InitializeDatabaseAsync()
    {
        return Task.CompletedTask;
    }
}
