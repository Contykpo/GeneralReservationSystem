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
    public async Task SearchTrips_ReturnsResults()
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

        _ = await adminClient.PostAsJsonAsync("/api/trips", createDto);

        PagedSearchRequestDto searchDto = new()
        {
            Page = 1,
            PageSize = 10
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/trips/search", searchDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        adminClient.Dispose();
    }
}
