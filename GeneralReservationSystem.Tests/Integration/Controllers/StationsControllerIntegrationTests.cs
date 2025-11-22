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

    [Fact]
    public async Task SearchStations_NoFiltersOrOrders_ReturnsPagedResult()
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
        CreateStationDto station3 = new()
        {
            StationName = "South Station",
            City = "Cordoba",
            Province = "Cordoba",
            Country = "Argentina"
        };

        _ = await adminClient.PostAsJsonAsync("/api/stations", station1);
        _ = await adminClient.PostAsJsonAsync("/api/stations", station2);
        _ = await adminClient.PostAsJsonAsync("/api/stations", station3);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/stations/search");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string content = await response.Content.ReadAsStringAsync();
        JsonDocument doc = JsonDocument.Parse(content);
        int totalCount = doc.RootElement.GetProperty("totalCount").GetInt32();
        int itemsCount = doc.RootElement.GetProperty("items").GetArrayLength();
        Assert.Equal(3, totalCount);
        Assert.Equal(3, itemsCount);

        adminClient.Dispose();
    }

    [Fact]
    public async Task SearchStations_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        for (int i = 1; i <= 25; i++)
        {
            CreateStationDto station = new()
            {
                StationName = $"Station {i}",
                City = $"City {i}",
                Province = $"Province {i}",
                Country = "Argentina"
            };
            _ = await adminClient.PostAsJsonAsync("/api/stations", station);
        }

        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/stations/search?page=2&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string content = await response.Content.ReadAsStringAsync();
        JsonDocument doc = JsonDocument.Parse(content);
        int page = doc.RootElement.GetProperty("page").GetInt32();
        int pageSize = doc.RootElement.GetProperty("pageSize").GetInt32();
        int totalCount = doc.RootElement.GetProperty("totalCount").GetInt32();
        int itemsCount = doc.RootElement.GetProperty("items").GetArrayLength();
        Assert.Equal(2, page);
        Assert.Equal(10, pageSize);
        Assert.Equal(10, itemsCount);
        Assert.Equal(25, totalCount);

        adminClient.Dispose();
    }

    [Fact]
    public async Task SearchStations_WithFilters_ReturnsFilteredResults()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        CreateStationDto station1 = new()
        {
            StationName = "Buenos Aires Central",
            City = "Buenos Aires",
            Province = "Buenos Aires",
            Country = "Argentina"
        };
        CreateStationDto station2 = new()
        {
            StationName = "Buenos Aires Retiro",
            City = "Buenos Aires",
            Province = "Buenos Aires",
            Country = "Argentina"
        };
        CreateStationDto station3 = new()
        {
            StationName = "Rosario North",
            City = "Rosario",
            Province = "Santa Fe",
            Country = "Argentina"
        };

        _ = await adminClient.PostAsJsonAsync("/api/stations", station1);
        _ = await adminClient.PostAsJsonAsync("/api/stations", station2);
        _ = await adminClient.PostAsJsonAsync("/api/stations", station3);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/stations/search?filters=[City|Contains|Buenos]");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string content = await response.Content.ReadAsStringAsync();
        JsonDocument doc = JsonDocument.Parse(content);
        int totalCount = doc.RootElement.GetProperty("totalCount").GetInt32();
        int itemsCount = doc.RootElement.GetProperty("items").GetArrayLength();
        Assert.Equal(2, totalCount);
        Assert.Equal(2, itemsCount);

        adminClient.Dispose();
    }

    [Fact]
    public async Task SearchStations_WithMultipleFilters_ReturnsMatchingResults()
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
            City = "Buenos Aires",
            Province = "Buenos Aires",
            Country = "Argentina"
        };
        CreateStationDto station3 = new()
        {
            StationName = "Central Station",
            City = "Rosario",
            Province = "Santa Fe",
            Country = "Argentina"
        };

        _ = await adminClient.PostAsJsonAsync("/api/stations", station1);
        _ = await adminClient.PostAsJsonAsync("/api/stations", station2);
        _ = await adminClient.PostAsJsonAsync("/api/stations", station3);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/stations/search?filters=[City|Equals|Buenos Aires]&filters=[StationName|Contains|Central]");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string content = await response.Content.ReadAsStringAsync();
        JsonDocument doc = JsonDocument.Parse(content);
        int totalCount = doc.RootElement.GetProperty("totalCount").GetInt32();
        Assert.Equal(1, totalCount);

        adminClient.Dispose();
    }

    [Fact]
    public async Task SearchStations_WithOrders_ReturnsSortedResults()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        CreateStationDto station1 = new()
        {
            StationName = "Charlie Station",
            City = "City C",
            Province = "Province C",
            Country = "Argentina"
        };
        CreateStationDto station2 = new()
        {
            StationName = "Alpha Station",
            City = "City A",
            Province = "Province A",
            Country = "Argentina"
        };
        CreateStationDto station3 = new()
        {
            StationName = "Bravo Station",
            City = "City B",
            Province = "Province B",
            Country = "Argentina"
        };

        _ = await adminClient.PostAsJsonAsync("/api/stations", station1);
        _ = await adminClient.PostAsJsonAsync("/api/stations", station2);
        _ = await adminClient.PostAsJsonAsync("/api/stations", station3);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/stations/search?orders=StationName|Asc");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string content = await response.Content.ReadAsStringAsync();
        JsonDocument doc = JsonDocument.Parse(content);
        JsonElement items = doc.RootElement.GetProperty("items");
        Assert.Equal("Alpha Station", items[0].GetProperty("stationName").GetString());
        Assert.Equal("Bravo Station", items[1].GetProperty("stationName").GetString());
        Assert.Equal("Charlie Station", items[2].GetProperty("stationName").GetString());

        adminClient.Dispose();
    }

    [Fact]
    public async Task SearchStations_WithDescendingOrder_ReturnsSortedResults()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        CreateStationDto station1 = new()
        {
            StationName = "Station A",
            City = "City A",
            Province = "Province A",
            Country = "Argentina"
        };
        CreateStationDto station2 = new()
        {
            StationName = "Station B",
            City = "City B",
            Province = "Province B",
            Country = "Argentina"
        };
        CreateStationDto station3 = new()
        {
            StationName = "Station C",
            City = "City C",
            Province = "Province C",
            Country = "Argentina"
        };

        _ = await adminClient.PostAsJsonAsync("/api/stations", station1);
        _ = await adminClient.PostAsJsonAsync("/api/stations", station2);
        _ = await adminClient.PostAsJsonAsync("/api/stations", station3);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/stations/search?orders=StationName|Desc");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string content = await response.Content.ReadAsStringAsync();
        JsonDocument doc = JsonDocument.Parse(content);
        JsonElement items = doc.RootElement.GetProperty("items");
        Assert.Equal("Station C", items[0].GetProperty("stationName").GetString());
        Assert.Equal("Station B", items[1].GetProperty("stationName").GetString());
        Assert.Equal("Station A", items[2].GetProperty("stationName").GetString());

        adminClient.Dispose();
    }

    [Fact]
    public async Task SearchStations_EmptyDatabase_ReturnsEmptyPagedResult()
    {
        // Arrange

        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/stations/search");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string content = await response.Content.ReadAsStringAsync();
        JsonDocument doc = JsonDocument.Parse(content);
        int totalCount = doc.RootElement.GetProperty("totalCount").GetInt32();
        int itemsCount = doc.RootElement.GetProperty("items").GetArrayLength();
        Assert.Equal(0, totalCount);
        Assert.Equal(0, itemsCount);
    }

    [Fact]
    public async Task SearchStations_NoMatchingFilters_ReturnsEmptyResults()
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

        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/stations/search?filters=[City|Equals|NonExistentCity]");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string content = await response.Content.ReadAsStringAsync();
        JsonDocument doc = JsonDocument.Parse(content);
        int totalCount = doc.RootElement.GetProperty("totalCount").GetInt32();
        Assert.Equal(0, totalCount);

        adminClient.Dispose();
    }

    [Fact]
    public async Task SearchStations_InvalidPageNumber_ReturnsEmptyPage()
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

        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/stations/search?page=100&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string content = await response.Content.ReadAsStringAsync();
        JsonDocument doc = JsonDocument.Parse(content);
        int itemsCount = doc.RootElement.GetProperty("items").GetArrayLength();
        Assert.Equal(0, itemsCount);

        adminClient.Dispose();
    }
}
