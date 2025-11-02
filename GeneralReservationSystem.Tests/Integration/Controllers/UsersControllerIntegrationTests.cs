using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.DTOs.Authentication;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace GeneralReservationSystem.Tests.Integration.Controllers;

public class UsersControllerIntegrationTests : IntegrationTestBase
{
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private string _adminToken = null!;
    private string _userToken = null!;
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
    }

    private async Task GetUserIdAsync()
    {
        HttpClient userClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _userToken);
        HttpResponseMessage meResponse = await userClient.GetAsync("/api/auth/me");
        JsonElement userInfo = await meResponse.Content.ReadFromJsonAsync<JsonElement>();
        _userId = userInfo.GetProperty("userId").GetInt32();
        userClient.Dispose();
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
    public async Task GetCurrentUser_Authenticated_ReturnsUserInfo()
    {
        // Arrange
        HttpClient userClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _userToken);

        // Act
        HttpResponseMessage response = await userClient.GetAsync("/api/users/me");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        UserInfo? userInfo = await response.Content.ReadFromJsonAsync<UserInfo>();
        Assert.NotNull(userInfo);
        Assert.Equal("regularuser", userInfo.UserName);
        Assert.Equal("user@example.com", userInfo.Email);

        userClient.Dispose();
    }

    [Fact]
    public async Task GetCurrentUser_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange

        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/users/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUserById_OwnUser_ReturnsOk()
    {
        // Arrange
        HttpClient userClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _userToken);

        // Act
        HttpResponseMessage response = await userClient.GetAsync($"/api/users/{_userId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        JsonElement user = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEqual(JsonValueKind.Undefined, user.ValueKind);

        userClient.Dispose();
    }

    [Fact]
    public async Task GetUserById_AsAdmin_ReturnsOk()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        // Act
        HttpResponseMessage response = await adminClient.GetAsync($"/api/users/{_userId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        adminClient.Dispose();
    }

    [Fact]
    public async Task GetUserById_OtherUserAsRegular_ReturnsForbidden()
    {
        // Arrange
        HttpClient userClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _userToken);

        // Act
        HttpResponseMessage response = await userClient.GetAsync("/api/users/999");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        userClient.Dispose();
    }

    [Fact]
    public async Task UpdateCurrentUser_ValidData_ReturnsOk()
    {
        // Arrange
        HttpClient userClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _userToken);

        UpdateUserDto updateDto = new()
        {
            UserId = _userId,
            UserName = "updateduser",
            Email = "updated@example.com"
        };

        // Act
        HttpResponseMessage response = await userClient.PutAsJsonAsync("/api/users/me", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        UserInfo? userInfo = await response.Content.ReadFromJsonAsync<UserInfo>();
        Assert.NotNull(userInfo);
        Assert.Equal("updateduser", userInfo.UserName);

        userClient.Dispose();
    }

    [Fact]
    public async Task UpdateCurrentUser_DuplicateUsername_ReturnsConflict()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        HttpResponseMessage meResponse = await adminClient.GetAsync("/api/auth/me");
        JsonElement adminInfo = await meResponse.Content.ReadFromJsonAsync<JsonElement>();
        int adminId = adminInfo.GetProperty("userId").GetInt32();

        UpdateUserDto updateDto = new()
        {
            UserId = adminId,
            UserName = "regularuser",
            Email = "admin2@example.com"
        };

        // Act
        HttpResponseMessage response = await adminClient.PutAsJsonAsync("/api/users/me", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        adminClient.Dispose();
    }

    [Fact]
    public async Task UpdateUserById_OwnUser_ReturnsOk()
    {
        // Arrange
        HttpClient userClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _userToken);

        UpdateUserDto updateDto = new()
        {
            UserId = _userId,
            UserName = "updateduser2",
            Email = "updated2@example.com"
        };

        // Act
        HttpResponseMessage response = await userClient.PutAsJsonAsync($"/api/users/{_userId}", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        userClient.Dispose();
    }

    [Fact]
    public async Task UpdateUserById_AsAdmin_ReturnsOk()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        UpdateUserDto updateDto = new()
        {
            UserId = _userId,
            UserName = "adminupdated",
            Email = "adminupdated@example.com"
        };

        // Act
        HttpResponseMessage response = await adminClient.PutAsJsonAsync($"/api/users/{_userId}", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        adminClient.Dispose();
    }

    [Fact]
    public async Task UpdateUserById_OtherUserAsRegular_ReturnsForbidden()
    {
        // Arrange
        HttpClient userClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _userToken);

        UpdateUserDto updateDto = new()
        {
            UserId = 999,
            UserName = "hacker",
            Email = "hacker@example.com"
        };

        // Act
        HttpResponseMessage response = await userClient.PutAsJsonAsync("/api/users/999", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        userClient.Dispose();
    }

    [Fact]
    public async Task SearchUsers_AsAdmin_ReturnsResults()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        PagedSearchRequestDto searchDto = new()
        {
            Page = 1,
            PageSize = 10
        };

        // Act
        HttpResponseMessage response = await adminClient.PostAsJsonAsync("/api/users/search", searchDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        adminClient.Dispose();
    }

    [Fact]
    public async Task SearchUsers_AsRegularUser_ReturnsForbidden()
    {
        // Arrange
        HttpClient userClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _userToken);

        PagedSearchRequestDto searchDto = new()
        {
            Page = 1,
            PageSize = 10
        };

        // Act
        HttpResponseMessage response = await userClient.PostAsJsonAsync("/api/users/search", searchDto);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        userClient.Dispose();
    }

    [Fact]
    public async Task SearchUsers_GetMethod_AsAdmin_ReturnsOk()
    {
        // Arrange
        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        // Act
        HttpResponseMessage response = await adminClient.GetAsync("/api/users/search?page=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        adminClient.Dispose();
    }

    [Fact]
    public async Task DeleteCurrentUser_ReturnsNoContent()
    {
        // Arrange
        string token = await IntegrationTestHelpers.RegisterUserAsync(
            _client,
            "deleteuser",
            "delete@example.com",
            "DeletePassword123!");

        HttpClient deleteClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, token);

        // Act
        HttpResponseMessage response = await deleteClient.DeleteAsync("/api/users/me");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        HttpResponseMessage getResponse = await deleteClient.GetAsync("/api/users/me");
        Assert.True(
            getResponse.StatusCode is HttpStatusCode.Unauthorized or
            HttpStatusCode.NotFound,
            $"Expected Unauthorized or NotFound, but got {getResponse.StatusCode}");

        deleteClient.Dispose();
    }

    [Fact]
    public async Task DeleteUserById_OwnUser_ReturnsNoContent()
    {
        // Arrange
        string token = await IntegrationTestHelpers.RegisterUserAsync(
            _client,
            "deleteuser2",
            "delete2@example.com",
            "DeletePassword123!");

        HttpClient deleteClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, token);

        HttpResponseMessage meResponse = await deleteClient.GetAsync("/api/auth/me");
        JsonElement userInfo = await meResponse.Content.ReadFromJsonAsync<JsonElement>();
        int deleteUserId = userInfo.GetProperty("userId").GetInt32();

        // Act
        HttpResponseMessage response = await deleteClient.DeleteAsync($"/api/users/{deleteUserId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        deleteClient.Dispose();
    }

    [Fact]
    public async Task DeleteUserById_AsAdmin_ReturnsNoContent()
    {
        // Arrange
        string token = await IntegrationTestHelpers.RegisterUserAsync(
            _client,
            "deleteuser3",
            "delete3@example.com",
            "DeletePassword123!");

        HttpClient userClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, token);
        HttpResponseMessage meResponse = await userClient.GetAsync("/api/auth/me");
        JsonElement userInfo = await meResponse.Content.ReadFromJsonAsync<JsonElement>();
        int deleteUserId = userInfo.GetProperty("userId").GetInt32();
        userClient.Dispose();

        HttpClient adminClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _adminToken);

        // Act
        HttpResponseMessage response = await adminClient.DeleteAsync($"/api/users/{deleteUserId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        adminClient.Dispose();
    }

    [Fact]
    public async Task DeleteUserById_OtherUserAsRegular_ReturnsForbidden()
    {
        // Arrange
        HttpClient userClient = IntegrationTestHelpers.CreateAuthenticatedClient(_factory, _userToken);

        // Act
        HttpResponseMessage response = await userClient.DeleteAsync("/api/users/999");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        userClient.Dispose();
    }
}
