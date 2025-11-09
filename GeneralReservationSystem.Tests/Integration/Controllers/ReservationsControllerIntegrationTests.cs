using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace GeneralReservationSystem.Tests.Integration.Controllers;

public class ReservationsControllerIntegrationTests : IntegrationTestBase
{
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private string _adminToken = null!;
    private string _userToken = null!;
    private int _tripId;
    private int _userId;

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

        await GetUserIdAsync();

        _client = _factory.CreateClient();

        await CreateTestTripAsync();
    }

    private async Task GetUserIdAsync()
    {
        HttpClient userClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _userToken);
        HttpResponseMessage meResponse = await userClient.GetAsync("/api/auth/me");
        JsonElement userInfo = await meResponse.Content.ReadFromJsonAsync<JsonElement>();
        _userId = userInfo.GetProperty("userId").GetInt32();
        userClient.Dispose();
    }

    private async Task CreateTestTripAsync()
    {
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        CreateStationDto departureStation = new()
        {
            StationName = "Station A",
            City = "Buenos Aires",
            Province = "Buenos Aires",
            Country = "Argentina"
        };

        CreateStationDto arrivalStation = new()
        {
            StationName = "Station B",
            City = "Cordoba",
            Province = "Cordoba",
            Country = "Argentina"
        };

        HttpResponseMessage departureResponse = await adminClient.PostAsJsonAsync("/api/stations", departureStation);
        Station? departure = await departureResponse.Content.ReadFromJsonAsync<Station>();

        HttpResponseMessage arrivalResponse = await adminClient.PostAsJsonAsync("/api/stations", arrivalStation);
        Station? arrival = await arrivalResponse.Content.ReadFromJsonAsync<Station>();

        CreateTripDto trip = new()
        {
            DepartureStationId = departure!.StationId,
            DepartureTime = DateTime.UtcNow.AddDays(1),
            ArrivalStationId = arrival!.StationId,
            ArrivalTime = DateTime.UtcNow.AddDays(1).AddHours(2),
            AvailableSeats = 50
        };

        HttpResponseMessage tripResponse = await adminClient.PostAsJsonAsync("/api/trips", trip);
        Trip? createdTrip = await tripResponse.Content.ReadFromJsonAsync<Trip>();
        _tripId = createdTrip!.TripId;

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
    public async Task CreateReservation_AsUser_ReturnsCreated()
    {
        // Arrange
        HttpClient userClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _userToken);

        CreateReservationDto createDto = new()
        {
            TripId = _tripId,
            Seat = 1,
            UserId = _userId
        };

        // Act
        HttpResponseMessage response = await userClient.PostAsJsonAsync("/api/reservations", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        userClient.Dispose();
    }

    [Fact]
    public async Task CreateReservation_ForAnotherUser_AsForbidden()
    {
        // Arrange
        HttpClient userClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _userToken);

        CreateReservationDto createDto = new()
        {
            TripId = _tripId,
            Seat = 2,
            UserId = 999
        };

        // Act
        HttpResponseMessage response = await userClient.PostAsJsonAsync("/api/reservations", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        userClient.Dispose();
    }

    [Fact]
    public async Task CreateReservationForMyself_ReturnsCreated()
    {
        // Arrange
        HttpClient userClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _userToken);

        ReservationKeyDto keyDto = new()
        {
            TripId = _tripId,
            Seat = 3
        };

        // Act
        HttpResponseMessage response = await userClient.PostAsJsonAsync("/api/reservations/me", keyDto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        userClient.Dispose();
    }

    [Fact]
    public async Task CreateReservation_DuplicateSeat_ReturnsConflict()
    {
        // Arrange
        HttpClient userClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _userToken);

        CreateReservationDto createDto = new()
        {
            TripId = _tripId,
            Seat = 10,
            UserId = _userId
        };

        _ = await userClient.PostAsJsonAsync("/api/reservations", createDto);

        // Act
        HttpResponseMessage response = await userClient.PostAsJsonAsync("/api/reservations", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        userClient.Dispose();
    }

    [Fact]
    public async Task CreateReservation_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        CreateReservationDto createDto = new()
        {
            TripId = _tripId,
            Seat = 20,
            UserId = _userId
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/reservations", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetReservation_OwnReservation_ReturnsOk()
    {
        // Arrange
        HttpClient userClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _userToken);

        CreateReservationDto createDto = new()
        {
            TripId = _tripId,
            Seat = 30,
            UserId = _userId
        };

        _ = await userClient.PostAsJsonAsync("/api/reservations", createDto);

        // Act
        HttpResponseMessage response = await userClient.GetAsync($"/api/reservations/{_tripId}/30");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Reservation? reservation = await response.Content.ReadFromJsonAsync<Reservation>();
        Assert.NotNull(reservation);
        Assert.Equal(_userId, reservation.UserId);

        userClient.Dispose();
    }

    [Fact]
    public async Task GetReservation_NonExistent_ReturnsNotFound()
    {
        // Arrange
        HttpClient userClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _userToken);

        // Act
        HttpResponseMessage response = await userClient.GetAsync($"/api/reservations/{_tripId}/999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        userClient.Dispose();
    }

    [Fact]
    public async Task GetMyReservations_ReturnsUserReservations()
    {
        // Arrange
        HttpClient userClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _userToken);

        CreateReservationDto createDto = new()
        {
            TripId = _tripId,
            Seat = 40,
            UserId = _userId
        };

        _ = await userClient.PostAsJsonAsync("/api/reservations", createDto);

        // Act
        HttpResponseMessage response = await userClient.GetAsync("/api/reservations/me");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        List<JsonElement>? reservations = await response.Content.ReadFromJsonAsync<List<JsonElement>>();
        Assert.NotNull(reservations);
        Assert.NotEmpty(reservations);

        userClient.Dispose();
    }

    [Fact]
    public async Task SearchCurrentUserReservations_ReturnsResults()
    {
        // Arrange
        HttpClient userClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _userToken);

        CreateReservationDto createDto = new()
        {
            TripId = _tripId,
            Seat = 45,
            UserId = _userId
        };

        _ = await userClient.PostAsJsonAsync("/api/reservations", createDto);

        // Act
        HttpResponseMessage response = await userClient.GetAsync("/api/reservations/search/me?page=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        userClient.Dispose();
    }

    [Fact]
    public async Task GetUserReservations_AsAdmin_ReturnsOk()
    {
        // Arrange
        HttpClient userClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _userToken);

        CreateReservationDto createDto = new()
        {
            TripId = _tripId,
            Seat = 50,
            UserId = _userId
        };

        _ = await userClient.PostAsJsonAsync("/api/reservations", createDto);
        userClient.Dispose();

        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        // Act
        HttpResponseMessage response = await adminClient.GetAsync($"/api/reservations/user/{_userId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        adminClient.Dispose();
    }

    [Fact]
    public async Task SearchReservations_AsAdmin_ReturnsOk()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        // Act
        HttpResponseMessage response = await adminClient.GetAsync("/api/reservations/search?page=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        adminClient.Dispose();
    }

    [Fact]
    public async Task SearchReservations_AsRegularUser_ReturnsForbidden()
    {
        // Arrange
        HttpClient userClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _userToken);

        // Act
        HttpResponseMessage response = await userClient.GetAsync("/api/reservations/search?page=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        userClient.Dispose();
    }

    [Fact]
    public async Task DeleteReservation_OwnReservation_ReturnsNoContent()
    {
        // Arrange
        HttpClient userClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _userToken);

        CreateReservationDto createDto = new()
        {
            TripId = _tripId,
            Seat = 60,
            UserId = _userId
        };

        _ = await userClient.PostAsJsonAsync("/api/reservations", createDto);

        // Act
        HttpResponseMessage response = await userClient.DeleteAsync($"/api/reservations/{_tripId}/60");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        HttpResponseMessage getResponse = await userClient.GetAsync($"/api/reservations/{_tripId}/60");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);

        userClient.Dispose();
    }

    [Fact]
    public async Task DeleteReservation_NonExistent_ReturnsNotFound()
    {
        // Arrange
        HttpClient userClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _userToken);

        // Act
        HttpResponseMessage response = await userClient.DeleteAsync($"/api/reservations/{_tripId}/999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        userClient.Dispose();
    }

    [Fact]
    public async Task SearchUserReservations_OwnReservations_ReturnsOk()
    {
        // Arrange
        HttpClient userClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _userToken);

        CreateReservationDto createDto = new()
        {
            TripId = _tripId,
            Seat = 70,
            UserId = _userId
        };

        _ = await userClient.PostAsJsonAsync("/api/reservations", createDto);

        // Act
        HttpResponseMessage response = await userClient.GetAsync($"/api/reservations/search/{_userId}?page=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        userClient.Dispose();
    }

    [Fact]
    public async Task SearchUserReservations_OtherUserAsRegular_ReturnsForbidden()
    {
        // Arrange
        HttpClient userClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _userToken);

        // Act
        HttpResponseMessage response = await userClient.GetAsync("/api/reservations/search/999?page=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        userClient.Dispose();
    }
}
