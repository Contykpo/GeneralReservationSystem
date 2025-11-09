using GeneralReservationSystem.Application.DTOs.Authentication;
using GeneralReservationSystem.Tests.Integration.Helpers;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace GeneralReservationSystem.Tests.Integration.Controllers;

public class AuthenticationControllerIntegrationTests : IntegrationTestBase
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
    public async Task Register_ValidUser_ReturnsOkAndCreatesUser()
    {
        // Arrange
        RegisterUserDto registerDto = new()
        {
            UserName = "testuser",
            Email = "testuser@example.com",
            Password = "SecurePassword123!",
            ConfirmPassword = "SecurePassword123!"
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        JsonElement result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEqual(JsonValueKind.Undefined, result.ValueKind);

        string? token = AuthenticationHelper.ExtractJwtTokenFromCookie(response);
        Assert.NotNull(token);
    }

    [Fact]
    public async Task Register_DuplicateUsername_ReturnsConflict()
    {
        // Arrange
        RegisterUserDto registerDto = new()
        {
            UserName = "duplicateuser",
            Email = "duplicate@example.com",
            Password = "SecurePassword123!",
            ConfirmPassword = "SecurePassword123!"
        };

        _ = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        RegisterUserDto duplicateDto = new()
        {
            UserName = "duplicateuser",
            Email = "different@example.com",
            Password = "SecurePassword123!",
            ConfirmPassword = "SecurePassword123!"
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/register", duplicateDto);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        // Arrange
        RegisterUserDto registerDto = new()
        {
            UserName = "user1",
            Email = "same@example.com",
            Password = "SecurePassword123!",
            ConfirmPassword = "SecurePassword123!"
        };

        _ = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        RegisterUserDto duplicateDto = new()
        {
            UserName = "user2",
            Email = "same@example.com",
            Password = "SecurePassword123!",
            ConfirmPassword = "SecurePassword123!"
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/register", duplicateDto);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        RegisterUserDto registerDto = new()
        {
            UserName = "testuser",
            Email = "invalid-email",
            Password = "SecurePassword123!",
            ConfirmPassword = "SecurePassword123!"
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkAndSetsJwtCookie()
    {
        // Arrange
        RegisterUserDto registerDto = new()
        {
            UserName = "loginuser",
            Email = "login@example.com",
            Password = "SecurePassword123!",
            ConfirmPassword = "SecurePassword123!"
        };
        _ = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        _client = _factory.CreateClient();

        LoginDto loginDto = new()
        {
            UserNameOrEmail = "loginuser",
            Password = "SecurePassword123!"
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        JsonElement result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEqual(JsonValueKind.Undefined, result.ValueKind);

        string? token = AuthenticationHelper.ExtractJwtTokenFromCookie(response);
        Assert.NotNull(token);
    }

    [Fact]
    public async Task Login_WithEmail_ReturnsOk()
    {
        // Arrange
        RegisterUserDto registerDto = new()
        {
            UserName = "emailloginuser",
            Email = "emaillogin@example.com",
            Password = "SecurePassword123!",
            ConfirmPassword = "SecurePassword123!"
        };
        _ = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        _client = _factory.CreateClient();

        LoginDto loginDto = new()
        {
            UserNameOrEmail = "emaillogin@example.com",
            Password = "SecurePassword123!"
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_InvalidPassword_ReturnsConflict()
    {
        // Arrange
        RegisterUserDto registerDto = new()
        {
            UserName = "passwordtest",
            Email = "passwordtest@example.com",
            Password = "CorrectPassword123!",
            ConfirmPassword = "CorrectPassword123!"
        };
        _ = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        _client = _factory.CreateClient();

        LoginDto loginDto = new()
        {
            UserNameOrEmail = "passwordtest",
            Password = "WrongPassword123!"
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Login_NonExistentUser_ReturnsConflict()
    {
        // Arrange
        LoginDto loginDto = new()
        {
            UserNameOrEmail = "nonexistent",
            Password = "Password123!"
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        Assert.True(response.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Logout_AuthenticatedUser_ReturnsOkAndClearsCookie()
    {
        // Arrange
        RegisterUserDto registerDto = new()
        {
            UserName = "logoutuser",
            Email = "logout@example.com",
            Password = "SecurePassword123!",
            ConfirmPassword = "SecurePassword123!"
        };
        HttpResponseMessage registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
        string? token = AuthenticationHelper.ExtractJwtTokenFromCookie(registerResponse);

        _client = _factory.CreateClient();
        AuthenticationHelper.SetAuthenticationCookie(_client, token!);

        // Act
        HttpResponseMessage response = await _client.PostAsync("/api/auth/logout", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Logout_UnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange

        // Act
        HttpResponseMessage response = await _client.PostAsync("/api/auth/logout", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_AuthenticatedUser_ReturnsUserInfo()
    {
        // Arrange
        RegisterUserDto registerDto = new()
        {
            UserName = "currentuser",
            Email = "current@example.com",
            Password = "SecurePassword123!",
            ConfirmPassword = "SecurePassword123!"
        };
        HttpResponseMessage registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
        string? token = AuthenticationHelper.ExtractJwtTokenFromCookie(registerResponse);

        _client = _factory.CreateClient();
        AuthenticationHelper.SetAuthenticationCookie(_client, token!);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/auth/me");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        UserInfo? userInfo = await response.Content.ReadFromJsonAsync<UserInfo>();
        Assert.NotNull(userInfo);
        Assert.Equal("currentuser", userInfo.UserName);
        Assert.Equal("current@example.com", userInfo.Email);
        Assert.False(userInfo.IsAdmin);
    }

    [Fact]
    public async Task GetCurrentUser_UnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange

        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/auth/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_ValidRequest_ReturnsOk()
    {
        // Arrange
        RegisterUserDto registerDto = new()
        {
            UserName = "changepassuser",
            Email = "changepass@example.com",
            Password = "OldPassword123!",
            ConfirmPassword = "OldPassword123!"
        };
        HttpResponseMessage registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
        string? token = AuthenticationHelper.ExtractJwtTokenFromCookie(registerResponse);

        string responseContent = await registerResponse.Content.ReadAsStringAsync();
        JsonDocument jsonDoc = JsonDocument.Parse(responseContent);
        int userId = jsonDoc.RootElement.GetProperty("userId").GetInt32();

        _client = _factory.CreateClient();
        AuthenticationHelper.SetAuthenticationCookie(_client, token!);

        ChangePasswordDto changePasswordDto = new()
        {
            UserId = userId,
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!"
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/change-password", changePasswordDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        _client = _factory.CreateClient();
        LoginDto loginDto = new()
        {
            UserNameOrEmail = "changepassuser",
            Password = "NewPassword123!"
        };
        HttpResponseMessage loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_IncorrectCurrentPassword_ReturnsConflict()
    {
        // Arrange
        RegisterUserDto registerDto = new()
        {
            UserName = "wrongpassuser",
            Email = "wrongpass@example.com",
            Password = "CorrectPassword123!",
            ConfirmPassword = "CorrectPassword123!"
        };
        HttpResponseMessage registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
        string? token = AuthenticationHelper.ExtractJwtTokenFromCookie(registerResponse);

        string responseContent = await registerResponse.Content.ReadAsStringAsync();
        JsonDocument jsonDoc = JsonDocument.Parse(responseContent);
        int userId = jsonDoc.RootElement.GetProperty("userId").GetInt32();

        _client = _factory.CreateClient();
        AuthenticationHelper.SetAuthenticationCookie(_client, token!);

        ChangePasswordDto changePasswordDto = new()
        {
            UserId = userId,
            CurrentPassword = "WrongPassword123!",
            NewPassword = "NewPassword123!"
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/change-password", changePasswordDto);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_DifferentUser_ReturnsUnauthorized()
    {
        // Arrange
        RegisterUserDto registerDto1 = new()
        {
            UserName = "user1",
            Email = "user1@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };
        HttpResponseMessage registerResponse1 = await _client.PostAsJsonAsync("/api/auth/register", registerDto1);
        string? token1 = AuthenticationHelper.ExtractJwtTokenFromCookie(registerResponse1);

        _client = _factory.CreateClient();
        RegisterUserDto registerDto2 = new()
        {
            UserName = "user2",
            Email = "user2@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };
        HttpResponseMessage registerResponse2 = await _client.PostAsJsonAsync("/api/auth/register", registerDto2);
        string responseContent2 = await registerResponse2.Content.ReadAsStringAsync();
        JsonDocument jsonDoc2 = JsonDocument.Parse(responseContent2);
        int userId2 = jsonDoc2.RootElement.GetProperty("userId").GetInt32();

        _client = _factory.CreateClient();
        AuthenticationHelper.SetAuthenticationCookie(_client, token1!);

        ChangePasswordDto changePasswordDto = new()
        {
            UserId = userId2,
            CurrentPassword = "Password123!",
            NewPassword = "NewPassword123!"
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/change-password", changePasswordDto);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_UnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        ChangePasswordDto changePasswordDto = new()
        {
            UserId = 1,
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!"
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/change-password", changePasswordDto);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

	#region RegisterAdmin Tests

	[Fact]
	public async Task RegisterAdmin_ValidAdmin_ReturnsOkAndCreatesAdminUser()
	{
		// Arrange
		RegisterUserDto registerDto = new()
		{
			UserName        = "superadmin",
			Email           = "superadmin@grs.com",
			Password        = "123456",
			ConfirmPassword = "123456"
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/register-admin", registerDto);

		// Assert
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);

		string responseBody = await response.Content.ReadAsStringAsync();
		JsonDocument json = JsonDocument.Parse(responseBody);

		Assert.True(json.RootElement.TryGetProperty("userId", out _));
		Assert.Contains("Administrador registrado exitosamente", responseBody);
	}

	[Fact]
	public async Task RegisterAdmin_DuplicateUsername_ReturnsConflict()
	{
		// Arrange
		RegisterUserDto registerDto = new()
		{
			UserName            = "adminDuplicate",
			Email               = "admindup@example.com",
			Password            = "SecurePassword123!",
			ConfirmPassword     = "SecurePassword123!"
		};

		_ = await _client.PostAsJsonAsync("/api/auth/register-admin", registerDto);

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/register-admin", registerDto);

		// Assert
		Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
	}

	[Fact]
	public async Task RegisterAdmin_DuplicateEmail_ReturnsConflict()
	{
		// Arrange
		RegisterUserDto registerDto = new()
		{
			UserName        = "admin1",
			Email           = "sameadmin@example.com",
			Password        = "SecurePassword123!",
			ConfirmPassword = "SecurePassword123!"
		};

		_ = await _client.PostAsJsonAsync("/api/auth/register-admin", registerDto);

		RegisterUserDto duplicateDto = new()
		{
			UserName        = "admin2",
			Email           = "sameadmin@example.com", // mismo email
			Password        = "SecurePassword123!",
			ConfirmPassword = "SecurePassword123!"
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/register-admin", duplicateDto);

		// Assert
		Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
	}

	[Fact]
	public async Task RegisterAdmin_InvalidEmail_ReturnsBadRequest()
	{
		// Arrange
		RegisterUserDto registerDto = new()
		{
			UserName        = "admininvalid",
			Email           = "not-an-email",
			Password        = "SecurePassword123!",
			ConfirmPassword = "SecurePassword123!"
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/register-admin", registerDto);

		// Assert
		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	[Fact]
	public async Task RegisterAdmin_WeakPassword_ReturnsBadRequest()
	{
		// Arrange
		RegisterUserDto registerDto = new()
		{
			UserName        = "weakadmin",
			Email           = "weak@example.com",
			Password        = "123",
			ConfirmPassword = "123"
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/register-admin", registerDto);

		// Assert
		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	[Fact]
	public async Task RegisterAdmin_PasswordMismatch_ReturnsBadRequest()
	{
		// Arrange
		RegisterUserDto registerDto = new()
		{
			UserName        = "mismatchadmin",
			Email           = "mismatch@example.com",
			Password        = "Password123!",
			ConfirmPassword = "DifferentPassword123!"
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/register-admin", registerDto);

		// Assert
		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	[Fact]
	public async Task RegisterAdmin_ValidAdmin_CannotLoginAsUserWithoutAdminPrivileges()
	{
		// Arrange
		RegisterUserDto registerDto = new()
		{
			UserName        = "adminrestricted",
			Email           = "restricted@example.com",
			Password        = "SecurePassword123!",
			ConfirmPassword = "SecurePassword123!"
		};

		HttpResponseMessage registerResponse = await _client.PostAsJsonAsync("/api/auth/register-admin", registerDto);

		string responseBody = await registerResponse.Content.ReadAsStringAsync();

		JsonDocument json = JsonDocument.Parse(responseBody);

		int adminUserId = json.RootElement.GetProperty("userId").GetInt32();

		// Simulamos login y obtenemos cookie
		LoginDto loginDto = new()
		{
			UserNameOrEmail = "adminrestricted",
			Password        = "SecurePassword123!"
		};

		HttpResponseMessage loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

		Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

		string? token = AuthenticationHelper.ExtractJwtTokenFromCookie(loginResponse);
		Assert.NotNull(token);

		// Act — consultamos /api/auth/me con cookie
		_client = _factory.CreateClient();
		AuthenticationHelper.SetAuthenticationCookie(_client, token!);
		HttpResponseMessage meResponse = await _client.GetAsync("/api/auth/me");

		// Assert
		Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);

		UserInfo? info = await meResponse.Content.ReadFromJsonAsync<UserInfo>();
		Assert.NotNull(info);
		Assert.Equal(adminUserId, info.UserId);
		Assert.True(info.IsAdmin);
	}

	#endregion
}
