using GeneralReservationSystem.Application.DTOs.Authentication;
using GeneralReservationSystem.Tests.Integration.Helpers;
using Npgsql;
using System.Net.Http.Json;

namespace GeneralReservationSystem.Tests.Integration;

public static class IntegrationTestHelpers
{
    public static async Task<string> RegisterUserAsync(HttpClient client, string username, string email, string password)
    {
        RegisterUserDto registerDto = new()
        {
            UserName = username,
            Email = email,
            Password = password,
            ConfirmPassword = password
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/register", registerDto);
        _ = response.EnsureSuccessStatusCode();

        string? token = AuthenticationHelper.ExtractJwtTokenFromCookie(response);
        return token ?? throw new InvalidOperationException("Failed to extract JWT token from registration response");
    }

    public static async Task<string> RegisterAdminUserAsync(HttpClient client, string connectionString, string username, string email, string password)
    {
        _ = await RegisterUserAsync(client, username, email, password);
        await SetUserAsAdminAsync(connectionString, email);

        HttpClient newClient = client;
        LoginDto loginDto = new()
        {
            UserNameOrEmail = username,
            Password = password
        };

        HttpResponseMessage loginResponse = await newClient.PostAsJsonAsync("/api/auth/login", loginDto);
        _ = loginResponse.EnsureSuccessStatusCode();

        string? adminToken = AuthenticationHelper.ExtractJwtTokenFromCookie(loginResponse);
        return adminToken ?? throw new InvalidOperationException("Failed to extract JWT token after admin promotion");
    }

    public static async Task SetUserAsAdminAsync(string connectionString, string email)
    {
        await using NpgsqlConnection connection = new(connectionString);
        await connection.OpenAsync();

        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = "UPDATE grsdb.\"ApplicationUser\" SET \"IsAdmin\" = true WHERE \"Email\" = @email";
        _ = command.Parameters.AddWithValue("email", email);
        _ = await command.ExecuteNonQueryAsync();
    }

    public static async Task<string> LoginUserAsync(HttpClient client, string usernameOrEmail, string password)
    {
        LoginDto loginDto = new()
        {
            UserNameOrEmail = usernameOrEmail,
            Password = password
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/login", loginDto);
        _ = response.EnsureSuccessStatusCode();

        string? token = AuthenticationHelper.ExtractJwtTokenFromCookie(response);
        return token ?? throw new InvalidOperationException("Failed to extract JWT token from login response");
    }

    public static HttpClient CreateAuthenticatedClient(CustomWebApplicationFactory factory, string token)
    {
        HttpClient client = factory.CreateClient();
        AuthenticationHelper.SetAuthenticationCookie(client, token);
        return client;
    }
}
