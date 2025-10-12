using GeneralReservationSystem.Application.DTOs.Authentication;
using GeneralReservationSystem.Web.Services.Interfaces.Authentication;

namespace GeneralReservationSystem.Web.Services.Implementations.Authentication
{
    public class ClientAuthenticationService(HttpClient httpClient) : ApiServiceBase(httpClient), IClientAuthenticationService
    {
        public async Task<UserInfo> RegisterUserAsync(RegisterUserDto dto, CancellationToken cancellationToken = default)
        {
            _ = await PostAsync<RegisterResponse>("/api/auth/register", dto, cancellationToken);
            // After registration, get full user details from API
            return await GetAsync<UserInfo>("/api/auth/me", cancellationToken);
        }

        public async Task<UserInfo> AuthenticateAsync(LoginDto dto, CancellationToken cancellationToken = default)
        {
            _ = await PostAsync<LoginResponse>("/api/auth/login", dto, cancellationToken);
            // After login, get full user details from API
            return await GetAsync<UserInfo>("/api/auth/me", cancellationToken);
        }

        public async Task LogoutAsync(CancellationToken cancellationToken = default)
        {
            await PostAsync<object>($"/api/auth/logout", content: null!, cancellationToken: cancellationToken);
        }

        public async Task ChangePasswordAsync(ChangePasswordDto dto, CancellationToken cancellationToken = default)
        {
            await PostAsync("/api/auth/change-password", dto, cancellationToken);
        }

        public async Task<UserInfo> GetCurrentUserAsync(CancellationToken cancellationToken = default)
        {
            return await GetAsync<UserInfo>("/api/auth/me", cancellationToken);
        }

        private class RegisterResponse
        {
            public string Message { get; set; } = string.Empty;
            public int UserId { get; set; }
        }

        private class LoginResponse
        {
            public string Message { get; set; } = string.Empty;
            public int UserId { get; set; }
            public bool IsAdmin { get; set; }
        }
    }
}


