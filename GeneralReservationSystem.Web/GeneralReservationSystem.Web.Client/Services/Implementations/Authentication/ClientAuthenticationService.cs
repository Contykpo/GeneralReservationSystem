using GeneralReservationSystem.Application.DTOs.Authentication;
using GeneralReservationSystem.Web.Client.Services.Interfaces.Authentication;

namespace GeneralReservationSystem.Web.Client.Services.Implementations.Authentication
{
    public class ClientAuthenticationService(HttpClient httpClient) : ApiServiceBase(httpClient), IClientAuthenticationService
    {
        public async Task<UserInfo> GetCurrentUserAsync(CancellationToken cancellationToken = default)
        {
            return await GetAsync<UserInfo>("/api/auth/me", cancellationToken);
        }

        private async Task<UserInfo> Post2AuthAndGetUserAsync<TDTO, TResponse>(string endpoint, TDTO dto, CancellationToken cancellationToken)
        {
            _ = await PostAsync<TResponse>($"/api/auth/{endpoint}", dto, cancellationToken);

            // After registration, get full user details from API
            return await GetCurrentUserAsync(cancellationToken);
        }

        public async Task<UserInfo> RegisterUserAsync(RegisterUserDto dto, CancellationToken cancellationToken = default)
        {
            return await Post2AuthAndGetUserAsync<RegisterUserDto, RegisterResponse>("register", dto, cancellationToken);
        }

        public async Task<UserInfo> RegisterAdminAsync(RegisterUserDto dto, CancellationToken cancellationToken = default)
        {
            return await Post2AuthAndGetUserAsync<RegisterUserDto, RegisterResponse>("register-admin", dto, cancellationToken);
        }

        public async Task<UserInfo> AuthenticateAsync(LoginDto dto, CancellationToken cancellationToken = default)
        {
            return await Post2AuthAndGetUserAsync<LoginDto, LoginResponse>("login", dto, cancellationToken);
        }

        public async Task LogoutAsync(CancellationToken cancellationToken = default)
        {
            _ = await PostAsync<object>($"/api/auth/logout", content: null!, cancellationToken: cancellationToken);
        }

        public async Task ChangePasswordAsync(ChangePasswordDto dto, CancellationToken cancellationToken = default)
        {
            await PostAsync("/api/auth/change-password", dto, cancellationToken);
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


