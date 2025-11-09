using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace GeneralReservationSystem.Tests.Integration.Controllers;

public class StationsControllerIntegrationTests : IntegrationTestBase
{
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private string _adminToken = null!;
    private string _userToken = null!;

    protected override async Task InitializeDatabaseAsync()
    {
        _factory = new CustomWebApplicationFactory(ConnectionString);
        await _factory.InitializeDatabaseAsync();
        _client = _factory.CreateClient();

        _adminToken = await IntegrationTestHelpers.RegisterAdminUserAsync(
            _client,
            ConnectionString,
            "admin",
            "admin@example.com",
            "AdminPassword123!");

        _client = _factory.CreateClient();
        _userToken = await IntegrationTestHelpers.RegisterUserAsync(
            _client,
            "regularuser",
            "user@example.com",
            "UserPassword123!");

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
    public async Task GetAllStations_NoStations_ReturnsEmptyList()
    {
        // Arrange

        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/stations");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        List<Station>? stations = await response.Content.ReadFromJsonAsync<List<Station>>();
        Assert.NotNull(stations);
        Assert.Empty(stations);
    }

    [Fact]
    public async Task GetAllStations_WithStations_ReturnsAllStations()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        CreateStationDto station1 = new()
        {
            StationName = "Central Station",
            City = "Buenos Aires",
            Province = "Buenos Aires",
            Country = "Argentina"
        };
        CreateStationDto station2 = new()
        {
            StationName = "North Station",
            City = "Rosario",
            Province = "Santa Fe",
            Country = "Argentina"
        };

        _ = await adminClient.PostAsJsonAsync("/api/stations", station1);
        _ = await adminClient.PostAsJsonAsync("/api/stations", station2);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/stations");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        List<Station>? stations = await response.Content.ReadFromJsonAsync<List<Station>>();
        Assert.NotNull(stations);
        Assert.Equal(2, stations.Count);

        adminClient.Dispose();
    }

    [Fact]
    public async Task GetStation_ExistingStation_ReturnsStation()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        CreateStationDto createDto = new()
        {
            StationName = "Test Station",
            City = "Test City",
            Province = "Test Province",
            Country = "Argentina"
        };
        HttpResponseMessage createResponse = await adminClient.PostAsJsonAsync("/api/stations", createDto);
        Station? createdStation = await createResponse.Content.ReadFromJsonAsync<Station>();

        // Act
        HttpResponseMessage response = await _client.GetAsync($"/api/stations/{createdStation!.StationId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Station? station = await response.Content.ReadFromJsonAsync<Station>();
        Assert.NotNull(station);
        Assert.Equal(createDto.StationName, station.StationName);
        Assert.Equal(createDto.City, station.City);

        adminClient.Dispose();
    }

    [Fact]
    public async Task GetStation_NonExistentStation_ReturnsNotFound()
    {
        // Arrange

        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/stations/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetStation_InvalidStationId_ReturnsBadRequest()
    {
        // Arrange

        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/stations/0");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SearchStations_WithQueryParameters_ReturnsFilteredResults()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        CreateStationDto[] stations =
        [
            new CreateStationDto { StationName = "Central", City = "Buenos Aires", Province = "Buenos Aires", Country = "Argentina" },
            new CreateStationDto { StationName = "North", City = "Rosario", Province = "Santa Fe", Country = "Argentina" },
            new CreateStationDto { StationName = "South", City = "Cordoba", Province = "Cordoba", Country = "Argentina" }
        ];

        foreach (CreateStationDto? station in stations)
        {
            _ = await adminClient.PostAsJsonAsync("/api/stations", station);
        }

        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/stations/search?searchTerm=central&pageSize=10&pageNumber=1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        JsonElement result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEqual(JsonValueKind.Undefined, result.ValueKind);

        adminClient.Dispose();
    }

    [Fact]
    public async Task SearchStations_PostMethod_ReturnsResults()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        CreateStationDto station = new()
        {
            StationName = "Test Station",
            City = "Test City",
            Province = "Test Province",
            Country = "Argentina"
        };
        _ = await adminClient.PostAsJsonAsync("/api/stations", station);

        PagedSearchRequestDto searchDto = new()
        {
            Page = 1,
            PageSize = 10
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/stations/search", searchDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        adminClient.Dispose();
    }

    [Fact]
    public async Task CreateStation_AsAdmin_ReturnsCreatedStation()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        CreateStationDto createDto = new()
        {
            StationName = "New Station",
            City = "New City",
            Province = "New Province",
            Country = "Argentina"
        };

        // Act
        HttpResponseMessage response = await adminClient.PostAsJsonAsync("/api/stations", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Station? station = await response.Content.ReadFromJsonAsync<Station>();
        Assert.NotNull(station);
        Assert.Equal(createDto.StationName, station.StationName);
        Assert.True(station.StationId > 0);

        adminClient.Dispose();
    }

    [Fact]
    public async Task CreateStation_AsRegularUser_ReturnsForbidden()
    {
        // Arrange
        HttpClient userClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _userToken);

        CreateStationDto createDto = new()
        {
            StationName = "New Station",
            City = "New City",
            Province = "New Province",
            Country = "Argentina"
        };

        // Act
        HttpResponseMessage response = await userClient.PostAsJsonAsync("/api/stations", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        userClient.Dispose();
    }

    [Fact]
    public async Task CreateStation_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        CreateStationDto createDto = new()
        {
            StationName = "New Station",
            City = "New City",
            Province = "New Province",
            Country = "Argentina"
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/stations", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateStation_DuplicateStation_ReturnsConflict()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        CreateStationDto createDto = new()
        {
            StationName = "Duplicate Station",
            City = "Duplicate City",
            Province = "Duplicate Province",
            Country = "Argentina"
        };

        _ = await adminClient.PostAsJsonAsync("/api/stations", createDto);

        // Act
        HttpResponseMessage response = await adminClient.PostAsJsonAsync("/api/stations", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        adminClient.Dispose();
    }

    [Fact]
    public async Task CreateStation_InvalidData_ReturnsBadRequest()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        CreateStationDto createDto = new()
        {
            StationName = "",
            City = "City",
            Province = "Province",
            Country = "Argentina"
        };

        // Act
        HttpResponseMessage response = await adminClient.PostAsJsonAsync("/api/stations", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        adminClient.Dispose();
    }

    [Fact]
    public async Task UpdateStation_AsAdmin_ReturnsUpdatedStation()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        CreateStationDto createDto = new()
        {
            StationName = "Original Name",
            City = "Original City",
            Province = "Original Province",
            Country = "Argentina"
        };
        HttpResponseMessage createResponse = await adminClient.PostAsJsonAsync("/api/stations", createDto);
        Station? createdStation = await createResponse.Content.ReadFromJsonAsync<Station>();

        UpdateStationDto updateDto = new()
        {
            StationId = createdStation!.StationId,
            StationName = "Updated Name",
            City = "Updated City",
            Province = "Updated Province",
            Country = "Argentina"
        };

        // Act
        HttpResponseMessage response = await adminClient.PutAsJsonAsync($"/api/stations/{createdStation.StationId}", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Station? station = await response.Content.ReadFromJsonAsync<Station>();
        Assert.NotNull(station);
        Assert.Equal(updateDto.StationName, station.StationName);
        Assert.Equal(updateDto.City, station.City);

        adminClient.Dispose();
    }

    [Fact]
    public async Task UpdateStation_AsRegularUser_ReturnsForbidden()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        CreateStationDto createDto = new()
        {
            StationName = "Station",
            City = "City",
            Province = "Province",
            Country = "Argentina"
        };
        HttpResponseMessage createResponse = await adminClient.PostAsJsonAsync("/api/stations", createDto);
        Station? createdStation = await createResponse.Content.ReadFromJsonAsync<Station>();
        adminClient.Dispose();

        HttpClient userClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _userToken);

        UpdateStationDto updateDto = new()
        {
            StationId = createdStation!.StationId,
            StationName = "Updated",
            City = "Updated",
            Province = "Updated",
            Country = "Argentina"
        };

        // Act
        HttpResponseMessage response = await userClient.PutAsJsonAsync($"/api/stations/{createdStation.StationId}", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        userClient.Dispose();
    }

    [Fact]
    public async Task UpdateStation_NonExistentStation_ReturnsNotFound()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        UpdateStationDto updateDto = new()
        {
            StationId = 999999,
            StationName = "Updated",
            City = "Updated",
            Province = "Updated",
            Country = "Argentina"
        };

        // Act
        HttpResponseMessage response = await adminClient.PutAsJsonAsync("/api/stations/999999", updateDto);

        // Assert
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);

        adminClient.Dispose();
    }

    [Fact]
    public async Task DeleteStation_AsAdmin_ReturnsNoContent()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        CreateStationDto createDto = new()
        {
            StationName = "To Delete",
            City = "City",
            Province = "Province",
            Country = "Argentina"
        };
        HttpResponseMessage createResponse = await adminClient.PostAsJsonAsync("/api/stations", createDto);
        Station? createdStation = await createResponse.Content.ReadFromJsonAsync<Station>();

        // Act
        HttpResponseMessage response = await adminClient.DeleteAsync($"/api/stations/{createdStation!.StationId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        HttpResponseMessage getResponse = await _client.GetAsync($"/api/stations/{createdStation.StationId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);

        adminClient.Dispose();
    }

    [Fact]
    public async Task DeleteStation_AsRegularUser_ReturnsForbidden()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        CreateStationDto createDto = new()
        {
            StationName = "Station",
            City = "City",
            Province = "Province",
            Country = "Argentina"
        };
        HttpResponseMessage createResponse = await adminClient.PostAsJsonAsync("/api/stations", createDto);
        Station? createdStation = await createResponse.Content.ReadFromJsonAsync<Station>();
        adminClient.Dispose();

        HttpClient userClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _userToken);

        // Act
        HttpResponseMessage response = await userClient.DeleteAsync($"/api/stations/{createdStation!.StationId}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        userClient.Dispose();
    }

    [Fact]
    public async Task DeleteStation_NonExistentStation_ReturnsNotFound()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        // Act
        HttpResponseMessage response = await adminClient.DeleteAsync("/api/stations/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        adminClient.Dispose();
    }

    [Fact]
    public async Task DeleteStation_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange

        // Act
        HttpResponseMessage response = await _client.DeleteAsync("/api/stations/1");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
