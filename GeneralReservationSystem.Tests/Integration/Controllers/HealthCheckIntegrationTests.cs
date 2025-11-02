using System.Net;

namespace GeneralReservationSystem.Tests.Integration.Controllers;

public class HealthCheckIntegrationTests : IntegrationTestBase
{
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    protected override async Task InitializeDatabaseAsync()
    {
        _factory = new CustomWebApplicationFactory(ConnectionString);
        await _factory.InitializeDatabaseAsync();
        _client = _factory.CreateClient();
    }

    public override async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_factory != null)
        {
            await _factory.DisposeAsync();
        }
        await base.DisposeAsync();
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        // Arrange

        // Act
        HttpResponseMessage response = await _client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string content = await response.Content.ReadAsStringAsync();
        Assert.Equal("Healthy", content);
    }
}
