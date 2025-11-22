using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace GeneralReservationSystem.Tests.Integration.Controllers;

public class TripsControllerIntegrationTests : IntegrationTestBase
{
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private string _adminToken = null!;
    private string _userToken = null!;
    private int _departureStationId;
    private int _arrivalStationId;

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

        await CreateTestStationsAsync();
    }

    private async Task CreateTestStationsAsync()
    {
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        CreateStationDto departureStation = new()
        {
            StationName = "Departure Station",
            City = "Buenos Aires",
            Province = "Buenos Aires",
            Country = "Argentina"
        };

        CreateStationDto arrivalStation = new()
        {
            StationName = "Arrival Station",
            City = "Cordoba",
            Province = "Cordoba",
            Country = "Argentina"
        };

        HttpResponseMessage departureResponse = await adminClient.PostAsJsonAsync("/api/stations", departureStation);
        Station? departure = await departureResponse.Content.ReadFromJsonAsync<Station>();
        _departureStationId = departure!.StationId;

        HttpResponseMessage arrivalResponse = await adminClient.PostAsJsonAsync("/api/stations", arrivalStation);
        Station? arrival = await arrivalResponse.Content.ReadFromJsonAsync<Station>();
        _arrivalStationId = arrival!.StationId;

        adminClient.Dispose();
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
    public async Task GetAllTrips_NoTrips_ReturnsEmptyList()
    {
        // Arrange

        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/trips");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        List<Trip>? trips = await response.Content.ReadFromJsonAsync<List<Trip>>();
        Assert.NotNull(trips);
        Assert.Empty(trips);
    }

    [Fact]
    public async Task GetAllTrips_WithTrips_ReturnsAllTrips()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        CreateTripDto trip = new()
        {
            DepartureStationId = _departureStationId,
            DepartureTime = DateTime.UtcNow.AddDays(1),
            ArrivalStationId = _arrivalStationId,
            ArrivalTime = DateTime.UtcNow.AddDays(1).AddHours(2),
            AvailableSeats = 50
        };

        _ = await adminClient.PostAsJsonAsync("/api/trips", trip);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/trips");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        List<Trip>? trips = await response.Content.ReadFromJsonAsync<List<Trip>>();
        Assert.NotNull(trips);
        _ = Assert.Single(trips);

        adminClient.Dispose();
    }

    [Fact]
    public async Task GetTrip_ExistingTrip_ReturnsTrip()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        CreateTripDto createDto = new()
        {
            DepartureStationId = _departureStationId,
            DepartureTime = DateTime.UtcNow.AddDays(1),
            ArrivalStationId = _arrivalStationId,
            ArrivalTime = DateTime.UtcNow.AddDays(1).AddHours(2),
            AvailableSeats = 50
        };

        HttpResponseMessage createResponse = await adminClient.PostAsJsonAsync("/api/trips", createDto);
        Trip? createdTrip = await createResponse.Content.ReadFromJsonAsync<Trip>();

        // Act
        HttpResponseMessage response = await _client.GetAsync($"/api/trips/{createdTrip!.TripId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Trip? trip = await response.Content.ReadFromJsonAsync<Trip>();
        Assert.NotNull(trip);
        Assert.Equal(createdTrip.TripId, trip.TripId);

        adminClient.Dispose();
    }

    [Fact]
    public async Task GetTrip_NonExistentTrip_ReturnsNotFound()
    {
        // Arrange

        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/trips/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetTripWithDetails_ExistingTrip_ReturnsDetails()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        CreateTripDto createDto = new()
        {
            DepartureStationId = _departureStationId,
            DepartureTime = DateTime.UtcNow.AddDays(1),
            ArrivalStationId = _arrivalStationId,
            ArrivalTime = DateTime.UtcNow.AddDays(1).AddHours(2),
            AvailableSeats = 50
        };

        HttpResponseMessage createResponse = await adminClient.PostAsJsonAsync("/api/trips", createDto);
        Trip? createdTrip = await createResponse.Content.ReadFromJsonAsync<Trip>();

        // Act
        HttpResponseMessage response = await _client.GetAsync($"/api/trips/{createdTrip!.TripId}/details");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        JsonElement details = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEqual(JsonValueKind.Undefined, details.ValueKind);

        adminClient.Dispose();
    }

    [Fact]
    public async Task CreateTrip_AsAdmin_ReturnsCreatedTrip()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        CreateTripDto createDto = new()
        {
            DepartureStationId = _departureStationId,
            DepartureTime = DateTime.UtcNow.AddDays(1),
            ArrivalStationId = _arrivalStationId,
            ArrivalTime = DateTime.UtcNow.AddDays(1).AddHours(2),
            AvailableSeats = 50
        };

        // Act
        HttpResponseMessage response = await adminClient.PostAsJsonAsync("/api/trips", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Trip? trip = await response.Content.ReadFromJsonAsync<Trip>();
        Assert.NotNull(trip);
        Assert.True(trip.TripId > 0);

        adminClient.Dispose();
    }

    [Fact]
    public async Task CreateTrip_AsRegularUser_ReturnsForbidden()
    {
        // Arrange
        HttpClient userClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _userToken);

        CreateTripDto createDto = new()
        {
            DepartureStationId = _departureStationId,
            DepartureTime = DateTime.UtcNow.AddDays(1),
            ArrivalStationId = _arrivalStationId,
            ArrivalTime = DateTime.UtcNow.AddDays(1).AddHours(2),
            AvailableSeats = 50
        };

        // Act
        HttpResponseMessage response = await userClient.PostAsJsonAsync("/api/trips", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        userClient.Dispose();
    }

    [Fact]
    public async Task CreateTrip_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        CreateTripDto createDto = new()
        {
            DepartureStationId = _departureStationId,
            DepartureTime = DateTime.UtcNow.AddDays(1),
            ArrivalStationId = _arrivalStationId,
            ArrivalTime = DateTime.UtcNow.AddDays(1).AddHours(2),
            AvailableSeats = 50
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/trips", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateTrip_InvalidData_ReturnsBadRequest()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        CreateTripDto createDto = new()
        {
            DepartureStationId = _departureStationId,
            DepartureTime = DateTime.UtcNow.AddDays(1),
            ArrivalStationId = _arrivalStationId,
            ArrivalTime = DateTime.UtcNow,
            AvailableSeats = 50
        };

        // Act
        HttpResponseMessage response = await adminClient.PostAsJsonAsync("/api/trips", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        adminClient.Dispose();
    }

    [Fact]
    public async Task UpdateTrip_AsAdmin_ReturnsUpdatedTrip()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        CreateTripDto createDto = new()
        {
            DepartureStationId = _departureStationId,
            DepartureTime = DateTime.UtcNow.AddDays(1),
            ArrivalStationId = _arrivalStationId,
            ArrivalTime = DateTime.UtcNow.AddDays(1).AddHours(2),
            AvailableSeats = 50
        };

        HttpResponseMessage createResponse = await adminClient.PostAsJsonAsync("/api/trips", createDto);
        Trip? createdTrip = await createResponse.Content.ReadFromJsonAsync<Trip>();

        UpdateTripDto updateDto = new()
        {
            TripId = createdTrip!.TripId,
            DepartureStationId = _departureStationId,
            DepartureTime = DateTime.UtcNow.AddDays(2),
            ArrivalStationId = _arrivalStationId,
            ArrivalTime = DateTime.UtcNow.AddDays(2).AddHours(2),
            AvailableSeats = 100
        };

        // Act
        HttpResponseMessage response = await adminClient.PutAsJsonAsync($"/api/trips/{createdTrip.TripId}", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Trip? trip = await response.Content.ReadFromJsonAsync<Trip>();
        Assert.NotNull(trip);
        Assert.Equal(100, trip.AvailableSeats);

        adminClient.Dispose();
    }

    [Fact]
    public async Task UpdateTrip_AsRegularUser_ReturnsForbidden()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        CreateTripDto createDto = new()
        {
            DepartureStationId = _departureStationId,
            DepartureTime = DateTime.UtcNow.AddDays(1),
            ArrivalStationId = _arrivalStationId,
            ArrivalTime = DateTime.UtcNow.AddDays(1).AddHours(2),
            AvailableSeats = 50
        };

        HttpResponseMessage createResponse = await adminClient.PostAsJsonAsync("/api/trips", createDto);
        Trip? createdTrip = await createResponse.Content.ReadFromJsonAsync<Trip>();
        adminClient.Dispose();

        HttpClient userClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _userToken);

        UpdateTripDto updateDto = new()
        {
            TripId = createdTrip!.TripId,
            DepartureStationId = _departureStationId,
            DepartureTime = DateTime.UtcNow.AddDays(2),
            ArrivalStationId = _arrivalStationId,
            ArrivalTime = DateTime.UtcNow.AddDays(2).AddHours(2),
            AvailableSeats = 100
        };

        // Act
        HttpResponseMessage response = await userClient.PutAsJsonAsync($"/api/trips/{createdTrip.TripId}", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        userClient.Dispose();
    }

    [Fact]
    public async Task DeleteTrip_AsAdmin_ReturnsNoContent()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        CreateTripDto createDto = new()
        {
            DepartureStationId = _departureStationId,
            DepartureTime = DateTime.UtcNow.AddDays(1),
            ArrivalStationId = _arrivalStationId,
            ArrivalTime = DateTime.UtcNow.AddDays(1).AddHours(2),
            AvailableSeats = 50
        };

        HttpResponseMessage createResponse = await adminClient.PostAsJsonAsync("/api/trips", createDto);
        Trip? createdTrip = await createResponse.Content.ReadFromJsonAsync<Trip>();

        // Act
        HttpResponseMessage response = await adminClient.DeleteAsync($"/api/trips/{createdTrip!.TripId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        HttpResponseMessage getResponse = await _client.GetAsync($"/api/trips/{createdTrip.TripId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);

        adminClient.Dispose();
    }

    [Fact]
    public async Task DeleteTrip_AsRegularUser_ReturnsForbidden()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        CreateTripDto createDto = new()
        {
            DepartureStationId = _departureStationId,
            DepartureTime = DateTime.UtcNow.AddDays(1),
            ArrivalStationId = _arrivalStationId,
            ArrivalTime = DateTime.UtcNow.AddDays(1).AddHours(2),
            AvailableSeats = 50
        };

        HttpResponseMessage createResponse = await adminClient.PostAsJsonAsync("/api/trips", createDto);
        Trip? createdTrip = await createResponse.Content.ReadFromJsonAsync<Trip>();
        adminClient.Dispose();

        HttpClient userClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _userToken);

        // Act
        HttpResponseMessage response = await userClient.DeleteAsync($"/api/trips/{createdTrip!.TripId}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        userClient.Dispose();
    }

    [Fact]
    public async Task GetFreeSeats_ExistingTrip_ReturnsSeats()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        CreateTripDto createDto = new()
        {
            DepartureStationId = _departureStationId,
            DepartureTime = DateTime.UtcNow.AddDays(1),
            ArrivalStationId = _arrivalStationId,
            ArrivalTime = DateTime.UtcNow.AddDays(1).AddHours(2),
            AvailableSeats = 50
        };

        HttpResponseMessage createResponse = await adminClient.PostAsJsonAsync("/api/trips", createDto);
        Trip? createdTrip = await createResponse.Content.ReadFromJsonAsync<Trip>();

        // Act
        HttpResponseMessage response = await _client.GetAsync($"/api/trips/{createdTrip!.TripId}/free-seats");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        List<int>? freeSeats = await response.Content.ReadFromJsonAsync<List<int>>();
        Assert.NotNull(freeSeats);
        Assert.Equal(50, freeSeats.Count);

        adminClient.Dispose();
    }

    [Fact]
    public async Task SearchTrips_NoFiltersOrOrders_ReturnsPagedResult()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        CreateTripDto trip1 = new()
        {
            DepartureStationId = _departureStationId,
            DepartureTime = DateTime.UtcNow.AddDays(1),
            ArrivalStationId = _arrivalStationId,
            ArrivalTime = DateTime.UtcNow.AddDays(1).AddHours(2),
            AvailableSeats = 50
        };
        CreateTripDto trip2 = new()
        {
            DepartureStationId = _departureStationId,
            DepartureTime = DateTime.UtcNow.AddDays(2),
            ArrivalStationId = _arrivalStationId,
            ArrivalTime = DateTime.UtcNow.AddDays(2).AddHours(2),
            AvailableSeats = 40
        };
        CreateTripDto trip3 = new()
        {
            DepartureStationId = _departureStationId,
            DepartureTime = DateTime.UtcNow.AddDays(3),
            ArrivalStationId = _arrivalStationId,
            ArrivalTime = DateTime.UtcNow.AddDays(3).AddHours(2),
            AvailableSeats = 60
        };

        _ = await adminClient.PostAsJsonAsync("/api/trips", trip1);
        _ = await adminClient.PostAsJsonAsync("/api/trips", trip2);
        _ = await adminClient.PostAsJsonAsync("/api/trips", trip3);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/trips/search");

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
    public async Task SearchTrips_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        for (int i = 1; i <= 25; i++)
        {
            CreateTripDto trip = new()
            {
                DepartureStationId = _departureStationId,
                DepartureTime = DateTime.UtcNow.AddDays(i),
                ArrivalStationId = _arrivalStationId,
                ArrivalTime = DateTime.UtcNow.AddDays(i).AddHours(2),
                AvailableSeats = 50
            };
            _ = await adminClient.PostAsJsonAsync("/api/trips", trip);
        }

        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/trips/search?page=2&pageSize=10");

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
        Assert.Equal(25, totalCount);
        Assert.Equal(10, itemsCount);

        adminClient.Dispose();
    }

    [Fact]
    public async Task SearchTrips_WithFilters_ReturnsFilteredResults()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        CreateStationDto thirdStation = new()
        {
            StationName = "Third Station",
            City = "Rosario",
            Province = "Santa Fe",
            Country = "Argentina"
        };
        HttpResponseMessage thirdStationResponse = await adminClient.PostAsJsonAsync("/api/stations", thirdStation);
        Station? thirdStationObj = await thirdStationResponse.Content.ReadFromJsonAsync<Station>();
        int thirdStationId = thirdStationObj!.StationId;

        CreateTripDto trip1 = new()
        {
            DepartureStationId = _departureStationId,
            DepartureTime = DateTime.UtcNow.AddDays(1),
            ArrivalStationId = _arrivalStationId,
            ArrivalTime = DateTime.UtcNow.AddDays(1).AddHours(2),
            AvailableSeats = 50
        };
        CreateTripDto trip2 = new()
        {
            DepartureStationId = _departureStationId,
            DepartureTime = DateTime.UtcNow.AddDays(2),
            ArrivalStationId = thirdStationId,
            ArrivalTime = DateTime.UtcNow.AddDays(2).AddHours(2),
            AvailableSeats = 40
        };
        CreateTripDto trip3 = new()
        {
            DepartureStationId = thirdStationId,
            DepartureTime = DateTime.UtcNow.AddDays(3),
            ArrivalStationId = _arrivalStationId,
            ArrivalTime = DateTime.UtcNow.AddDays(3).AddHours(2),
            AvailableSeats = 60
        };

        _ = await adminClient.PostAsJsonAsync("/api/trips", trip1);
        _ = await adminClient.PostAsJsonAsync("/api/trips", trip2);
        _ = await adminClient.PostAsJsonAsync("/api/trips", trip3);

        // Act
        HttpResponseMessage response = await _client.GetAsync($"/api/trips/search?filters=[DepartureStationId|Equals|{_departureStationId}]");

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
    public async Task SearchTrips_WithMultipleFilters_ReturnsMatchingResults()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        CreateTripDto trip1 = new()
        {
            DepartureStationId = _departureStationId,
            DepartureTime = DateTime.UtcNow.AddDays(1),
            ArrivalStationId = _arrivalStationId,
            ArrivalTime = DateTime.UtcNow.AddDays(1).AddHours(2),
            AvailableSeats = 50
        };
        CreateTripDto trip2 = new()
        {
            DepartureStationId = _departureStationId,
            DepartureTime = DateTime.UtcNow.AddDays(2),
            ArrivalStationId = _arrivalStationId,
            ArrivalTime = DateTime.UtcNow.AddDays(2).AddHours(2),
            AvailableSeats = 30
        };
        CreateTripDto trip3 = new()
        {
            DepartureStationId = _departureStationId,
            DepartureTime = DateTime.UtcNow.AddDays(3),
            ArrivalStationId = _arrivalStationId,
            ArrivalTime = DateTime.UtcNow.AddDays(3).AddHours(2),
            AvailableSeats = 60
        };

        _ = await adminClient.PostAsJsonAsync("/api/trips", trip1);
        _ = await adminClient.PostAsJsonAsync("/api/trips", trip2);
        _ = await adminClient.PostAsJsonAsync("/api/trips", trip3);

        // Act
        HttpResponseMessage response = await _client.GetAsync($"/api/trips/search?filters=[DepartureStationId|Equals|{_departureStationId}]&filters=[AvailableSeats|GreaterThan|40]");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string content = await response.Content.ReadAsStringAsync();
        JsonDocument doc = JsonDocument.Parse(content);
        int totalCount = doc.RootElement.GetProperty("totalCount").GetInt32();
        Assert.Equal(2, totalCount);

        adminClient.Dispose();
    }

    [Fact]
    public async Task SearchTrips_WithOrders_ReturnsSortedResults()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        DateTime now = DateTime.UtcNow;
        CreateTripDto trip1 = new()
        {
            DepartureStationId = _departureStationId,
            DepartureTime = now.AddDays(3),
            ArrivalStationId = _arrivalStationId,
            ArrivalTime = now.AddDays(3).AddHours(2),
            AvailableSeats = 50
        };
        CreateTripDto trip2 = new()
        {
            DepartureStationId = _departureStationId,
            DepartureTime = now.AddDays(1),
            ArrivalStationId = _arrivalStationId,
            ArrivalTime = now.AddDays(1).AddHours(2),
            AvailableSeats = 40
        };
        CreateTripDto trip3 = new()
        {
            DepartureStationId = _departureStationId,
            DepartureTime = now.AddDays(2),
            ArrivalStationId = _arrivalStationId,
            ArrivalTime = now.AddDays(2).AddHours(2),
            AvailableSeats = 60
        };

        _ = await adminClient.PostAsJsonAsync("/api/trips", trip1);
        _ = await adminClient.PostAsJsonAsync("/api/trips", trip2);
        _ = await adminClient.PostAsJsonAsync("/api/trips", trip3);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/trips/search?orders=AvailableSeats|Asc");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string content = await response.Content.ReadAsStringAsync();
        JsonDocument doc = JsonDocument.Parse(content);
        JsonElement items = doc.RootElement.GetProperty("items");
        Assert.Equal(40, items[0].GetProperty("availableSeats").GetInt32());
        Assert.Equal(50, items[1].GetProperty("availableSeats").GetInt32());
        Assert.Equal(60, items[2].GetProperty("availableSeats").GetInt32());

        adminClient.Dispose();
    }

    [Fact]
    public async Task SearchTrips_WithDescendingOrder_ReturnsSortedResults()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        CreateTripDto trip1 = new()
        {
            DepartureStationId = _departureStationId,
            DepartureTime = DateTime.UtcNow.AddDays(1),
            ArrivalStationId = _arrivalStationId,
            ArrivalTime = DateTime.UtcNow.AddDays(1).AddHours(2),
            AvailableSeats = 30
        };
        CreateTripDto trip2 = new()
        {
            DepartureStationId = _departureStationId,
            DepartureTime = DateTime.UtcNow.AddDays(2),
            ArrivalStationId = _arrivalStationId,
            ArrivalTime = DateTime.UtcNow.AddDays(2).AddHours(2),
            AvailableSeats = 50
        };
        CreateTripDto trip3 = new()
        {
            DepartureStationId = _departureStationId,
            DepartureTime = DateTime.UtcNow.AddDays(3),
            ArrivalStationId = _arrivalStationId,
            ArrivalTime = DateTime.UtcNow.AddDays(3).AddHours(2),
            AvailableSeats = 40
        };

        _ = await adminClient.PostAsJsonAsync("/api/trips", trip1);
        _ = await adminClient.PostAsJsonAsync("/api/trips", trip2);
        _ = await adminClient.PostAsJsonAsync("/api/trips", trip3);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/trips/search?orders=AvailableSeats|Desc");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string content = await response.Content.ReadAsStringAsync();
        JsonDocument doc = JsonDocument.Parse(content);
        JsonElement items = doc.RootElement.GetProperty("items");
        Assert.Equal(50, items[0].GetProperty("availableSeats").GetInt32());
        Assert.Equal(40, items[1].GetProperty("availableSeats").GetInt32());
        Assert.Equal(30, items[2].GetProperty("availableSeats").GetInt32());

        adminClient.Dispose();
    }

    [Fact]
    public async Task SearchTrips_EmptyDatabase_ReturnsEmptyPagedResult()
    {
        // Arrange

        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/trips/search");

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
    public async Task SearchTrips_NoMatchingFilters_ReturnsEmptyResults()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        CreateTripDto trip = new()
        {
            DepartureStationId = _departureStationId,
            DepartureTime = DateTime.UtcNow.AddDays(1),
            ArrivalStationId = _arrivalStationId,
            ArrivalTime = DateTime.UtcNow.AddDays(1).AddHours(2),
            AvailableSeats = 50
        };
        _ = await adminClient.PostAsJsonAsync("/api/trips", trip);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/trips/search?filters=[AvailableSeats|Equals|999]");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string content = await response.Content.ReadAsStringAsync();
        JsonDocument doc = JsonDocument.Parse(content);
        int totalCount = doc.RootElement.GetProperty("totalCount").GetInt32();
        Assert.Equal(0, totalCount);

        adminClient.Dispose();
    }

    [Fact]
    public async Task SearchTrips_InvalidPageNumber_ReturnsEmptyPage()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        CreateTripDto trip = new()
        {
            DepartureStationId = _departureStationId,
            DepartureTime = DateTime.UtcNow.AddDays(1),
            ArrivalStationId = _arrivalStationId,
            ArrivalTime = DateTime.UtcNow.AddDays(1).AddHours(2),
            AvailableSeats = 50
        };
        _ = await adminClient.PostAsJsonAsync("/api/trips", trip);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/trips/search?page=100&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string content = await response.Content.ReadAsStringAsync();
        JsonDocument doc = JsonDocument.Parse(content);
        int itemsCount = doc.RootElement.GetProperty("items").GetArrayLength();
        Assert.Equal(0, itemsCount);

        adminClient.Dispose();
    }

    [Fact]
    public async Task SearchTrips_FilterByDepartureCity_ReturnsMatchingTrips()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        CreateTripDto trip1 = new()
        {
            DepartureStationId = _departureStationId,
            DepartureTime = DateTime.UtcNow.AddDays(1),
            ArrivalStationId = _arrivalStationId,
            ArrivalTime = DateTime.UtcNow.AddDays(1).AddHours(2),
            AvailableSeats = 50
        };
        CreateTripDto trip2 = new()
        {
            DepartureStationId = _departureStationId,
            DepartureTime = DateTime.UtcNow.AddDays(2),
            ArrivalStationId = _arrivalStationId,
            ArrivalTime = DateTime.UtcNow.AddDays(2).AddHours(2),
            AvailableSeats = 40
        };

        _ = await adminClient.PostAsJsonAsync("/api/trips", trip1);
        _ = await adminClient.PostAsJsonAsync("/api/trips", trip2);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/trips/search?filters=[DepartureCity|Contains|Buenos]");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string content = await response.Content.ReadAsStringAsync();
        JsonDocument doc = JsonDocument.Parse(content);
        int totalCount = doc.RootElement.GetProperty("totalCount").GetInt32();
        Assert.Equal(2, totalCount);

        adminClient.Dispose();
    }
}
