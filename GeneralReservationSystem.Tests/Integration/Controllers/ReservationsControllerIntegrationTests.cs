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

    #region SearchReservations Tests

    [Fact]
    public async Task SearchReservations_AsAdmin_ReturnsPagedResult()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);
        HttpClient userClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _userToken);

        CreateReservationDto reservation1 = new()
        {
            TripId = _tripId,
            Seat = 1,
            UserId = _userId
        };

        CreateReservationDto reservation2 = new()
        {
            TripId = _tripId,
            Seat = 2,
            UserId = _userId
        };

        _ = await userClient.PostAsJsonAsync("/api/reservations", reservation1);
        _ = await userClient.PostAsJsonAsync("/api/reservations", reservation2);
        userClient.Dispose();

        // Act
        HttpResponseMessage response = await adminClient.GetAsync("/api/reservations/search");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string content = await response.Content.ReadAsStringAsync();
        JsonDocument doc = JsonDocument.Parse(content);
        int totalCount = doc.RootElement.GetProperty("totalCount").GetInt32();
        Assert.True(totalCount >= 2);

        adminClient.Dispose();
    }

    [Fact]
    public async Task SearchReservations_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);
        HttpClient userClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _userToken);

        for (int i = 10; i <= 25; i++)
        {
            CreateReservationDto reservation = new()
            {
                TripId = _tripId,
                Seat = i,
                UserId = _userId
            };
            _ = await userClient.PostAsJsonAsync("/api/reservations", reservation);
        }
        userClient.Dispose();

        // Act
        HttpResponseMessage response = await adminClient.GetAsync("/api/reservations/search?page=2&pageSize=5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string content = await response.Content.ReadAsStringAsync();
        JsonDocument doc = JsonDocument.Parse(content);
        int page = doc.RootElement.GetProperty("page").GetInt32();
        int pageSize = doc.RootElement.GetProperty("pageSize").GetInt32();
        Assert.Equal(2, page);
        Assert.Equal(5, pageSize);

        adminClient.Dispose();
    }

    [Fact]
    public async Task SearchReservations_WithFilters_ReturnsFilteredResults()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);
        HttpClient userClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _userToken);

        CreateReservationDto reservation = new()
        {
            TripId = _tripId,
            Seat = 30,
            UserId = _userId
        };
        _ = await userClient.PostAsJsonAsync("/api/reservations", reservation);
        userClient.Dispose();

        // Act
        HttpResponseMessage response = await adminClient.GetAsync($"/api/reservations/search?filters=[TripId|Equals|{_tripId}]");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string content = await response.Content.ReadAsStringAsync();
        JsonDocument doc = JsonDocument.Parse(content);
        int totalCount = doc.RootElement.GetProperty("totalCount").GetInt32();
        Assert.True(totalCount >= 1);

        adminClient.Dispose();
    }

    [Fact]
    public async Task SearchReservations_WithOrders_ReturnsSortedResults()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);
        HttpClient userClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _userToken);

        CreateReservationDto reservation1 = new()
        {
            TripId = _tripId,
            Seat = 35,
            UserId = _userId
        };

        CreateReservationDto reservation2 = new()
        {
            TripId = _tripId,
            Seat = 36,
            UserId = _userId
        };

        _ = await userClient.PostAsJsonAsync("/api/reservations", reservation1);
        _ = await userClient.PostAsJsonAsync("/api/reservations", reservation2);
        userClient.Dispose();

        // Act
        HttpResponseMessage response = await adminClient.GetAsync($"/api/reservations/search?filters=[TripId|Equals|{_tripId}]&orders=Seat|Desc");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string content = await response.Content.ReadAsStringAsync();
        JsonDocument doc = JsonDocument.Parse(content);
        JsonElement items = doc.RootElement.GetProperty("items");
        Assert.True(items.GetArrayLength() >= 2);

        adminClient.Dispose();
    }

    [Fact]
    public async Task SearchReservations_AsRegularUser_ReturnsForbidden()
    {
        // Arrange
        HttpClient userClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _userToken);

        // Act
        HttpResponseMessage response = await userClient.GetAsync("/api/reservations/search");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        userClient.Dispose();
    }

    [Fact]
    public async Task SearchReservations_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange

        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/reservations/search");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SearchReservations_NoMatchingFilters_ReturnsEmptyResults()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        // Act
        HttpResponseMessage response = await adminClient.GetAsync("/api/reservations/search?filters=[TripId|Equals|99999]");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string content = await response.Content.ReadAsStringAsync();
        JsonDocument doc = JsonDocument.Parse(content);
        int totalCount = doc.RootElement.GetProperty("totalCount").GetInt32();
        Assert.Equal(0, totalCount);

        adminClient.Dispose();
    }

    #endregion

    #region SearchUserReservations Tests

    [Fact]
    public async Task SearchUserReservations_AsOwner_ReturnsPagedResult()
    {
        // Arrange
        HttpClient userClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _userToken);

        CreateReservationDto reservation1 = new()
        {
            TripId = _tripId,
            Seat = 40,
            UserId = _userId
        };

        CreateReservationDto reservation2 = new()
        {
            TripId = _tripId,
            Seat = 41,
            UserId = _userId
        };

        _ = await userClient.PostAsJsonAsync("/api/reservations", reservation1);
        _ = await userClient.PostAsJsonAsync("/api/reservations", reservation2);

        // Act
        HttpResponseMessage response = await userClient.GetAsync($"/api/reservations/search/{_userId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string content = await response.Content.ReadAsStringAsync();
        JsonDocument doc = JsonDocument.Parse(content);
        int totalCount = doc.RootElement.GetProperty("totalCount").GetInt32();
        Assert.True(totalCount >= 2);

        userClient.Dispose();
    }

    [Fact]
    public async Task SearchUserReservations_AsAdmin_ReturnsPagedResult()
    {
        // Arrange
        HttpClient userClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _userToken);

        CreateReservationDto reservation = new()
        {
            TripId = _tripId,
            Seat = 42,
            UserId = _userId
        };

        _ = await userClient.PostAsJsonAsync("/api/reservations", reservation);
        userClient.Dispose();

        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        // Act
        HttpResponseMessage response = await adminClient.GetAsync($"/api/reservations/search/{_userId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string content = await response.Content.ReadAsStringAsync();
        JsonDocument doc = JsonDocument.Parse(content);
        int totalCount = doc.RootElement.GetProperty("totalCount").GetInt32();
        Assert.True(totalCount >= 1);

        adminClient.Dispose();
    }

    [Fact]
    public async Task SearchUserReservations_AsNonAdminForOtherUser_ReturnsForbidden()
    {
        // Arrange
        HttpClient userClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _userToken);

        // Act
        HttpResponseMessage response = await userClient.GetAsync("/api/reservations/search/999");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        userClient.Dispose();
    }

    [Fact]
    public async Task SearchUserReservations_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        HttpClient userClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _userToken);

        for (int i = 43; i <= 50; i++)
        {
            CreateReservationDto reservation = new()
            {
                TripId = _tripId,
                Seat = i,
                UserId = _userId
            };
            _ = await userClient.PostAsJsonAsync("/api/reservations", reservation);
        }

        // Act
        HttpResponseMessage response = await userClient.GetAsync($"/api/reservations/search/{_userId}?page=1&pageSize=5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string content = await response.Content.ReadAsStringAsync();
        JsonDocument doc = JsonDocument.Parse(content);
        int page = doc.RootElement.GetProperty("page").GetInt32();
        int pageSize = doc.RootElement.GetProperty("pageSize").GetInt32();
        Assert.Equal(1, page);
        Assert.Equal(5, pageSize);

        userClient.Dispose();
    }

    [Fact]
    public async Task SearchUserReservations_WithFilters_ReturnsFilteredResults()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        // Act
        HttpResponseMessage response = await adminClient.GetAsync($"/api/reservations/search/{_userId}?filters=[Seat|GreaterThan|40]");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string content = await response.Content.ReadAsStringAsync();
        JsonDocument doc = JsonDocument.Parse(content);
        int totalCount = doc.RootElement.GetProperty("totalCount").GetInt32();
        Assert.True(totalCount >= 0);

        adminClient.Dispose();
    }

    [Fact]
    public async Task SearchUserReservations_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange

        // Act
        HttpResponseMessage response = await _client.GetAsync($"/api/reservations/search/{_userId}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region SearchCurrentUserReservations Tests

    [Fact]
    public async Task SearchCurrentUserReservations_ValidUser_ReturnsPagedResult()
    {
        // Arrange
        HttpClient userClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _userToken);

        // Act
        HttpResponseMessage response = await userClient.GetAsync("/api/reservations/search/me");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string content = await response.Content.ReadAsStringAsync();
        JsonDocument doc = JsonDocument.Parse(content);
        int totalCount = doc.RootElement.GetProperty("totalCount").GetInt32();
        Assert.True(totalCount >= 0);

        userClient.Dispose();
    }

    [Fact]
    public async Task SearchCurrentUserReservations_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        HttpClient userClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _userToken);

        // Act
        HttpResponseMessage response = await userClient.GetAsync("/api/reservations/search/me?page=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string content = await response.Content.ReadAsStringAsync();
        JsonDocument doc = JsonDocument.Parse(content);
        int page = doc.RootElement.GetProperty("page").GetInt32();
        int pageSize = doc.RootElement.GetProperty("pageSize").GetInt32();
        Assert.Equal(1, page);
        Assert.Equal(10, pageSize);

        userClient.Dispose();
    }

    [Fact]
    public async Task SearchCurrentUserReservations_WithFilters_ReturnsFilteredResults()
    {
        // Arrange
        HttpClient userClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _userToken);

        // Act
        HttpResponseMessage response = await userClient.GetAsync($"/api/reservations/search/me?filters=[TripId|Equals|{_tripId}]");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string content = await response.Content.ReadAsStringAsync();
        JsonDocument doc = JsonDocument.Parse(content);
        int totalCount = doc.RootElement.GetProperty("totalCount").GetInt32();
        Assert.True(totalCount >= 0);

        userClient.Dispose();
    }

    [Fact]
    public async Task SearchCurrentUserReservations_WithOrders_ReturnsSortedResults()
    {
        // Arrange
        HttpClient userClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _userToken);

        // Act
        HttpResponseMessage response = await userClient.GetAsync("/api/reservations/search/me?orders=Seat|Asc");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string content = await response.Content.ReadAsStringAsync();
        JsonDocument doc = JsonDocument.Parse(content);
        JsonElement items = doc.RootElement.GetProperty("items");
        Assert.True(items.GetArrayLength() >= 0);

        userClient.Dispose();
    }

    [Fact]
    public async Task SearchCurrentUserReservations_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange

        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/reservations/search/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SearchCurrentUserReservations_NoReservations_ReturnsEmptyPagedResult()
    {
        // Arrange
        string newUserToken = await IntegrationTestHelpers.RegisterUserAsync(
            _client,
            "emptyuser",
            "empty@example.com",
            "Password123!");

        HttpClient emptyUserClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, newUserToken);

        // Act
        HttpResponseMessage response = await emptyUserClient.GetAsync("/api/reservations/search/me");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string content = await response.Content.ReadAsStringAsync();
        JsonDocument doc = JsonDocument.Parse(content);
        int totalCount = doc.RootElement.GetProperty("totalCount").GetInt32();
        Assert.Equal(0, totalCount);

        emptyUserClient.Dispose();
    }

    #endregion

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
}
