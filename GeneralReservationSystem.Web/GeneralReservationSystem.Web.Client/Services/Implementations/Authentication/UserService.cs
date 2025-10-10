using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.DTOs.Authentication;
using GeneralReservationSystem.Application.Entities.Authentication;
using GeneralReservationSystem.Application.Services.Interfaces.Authentication;
using GeneralReservationSystem.Web.Client.Services;

namespace GeneralReservationSystem.Web.Client.Services.Implementations.Authentication
{
    public class UserService(HttpClient httpClient) : ApiServiceBase(httpClient), IUserService
    {
        public async Task<IEnumerable<User>> GetAllUsersAsync(CancellationToken cancellationToken = default)
        {
            return await GetAsync<IEnumerable<User>>("/api/users", cancellationToken);
        }

        public async Task<PagedResult<User>> SearchUsersAsync(PagedSearchRequestDto searchDto, CancellationToken cancellationToken = default)
        {
            return await PostAsync<PagedResult<User>>("/api/users/search", searchDto, cancellationToken);
        }

        public async Task<User> RegisterUserAsync(RegisterUserDto dto, CancellationToken cancellationToken = default)
        {
            _ = await PostAsync<RegisterResponse>("/api/auth/register", dto, cancellationToken);
            // After registration, get full user details from API
            return await GetUserAsync(new UserKeyDto(), cancellationToken);
        }

        public async Task<User> AuthenticateAsync(LoginDto dto, CancellationToken cancellationToken = default)
        {
            _ = await PostAsync<LoginResponse>("/api/auth/login", dto, cancellationToken);
            // After login, get full user details from API
            return await GetUserAsync(new UserKeyDto(), cancellationToken);
        }

        public async Task LogoutAsync(CancellationToken cancellationToken = default)
        {
            await PostAsync<object>($"/api/auth/logout", content: null!, cancellationToken: cancellationToken);
        }

        public async Task<User> GetUserAsync(UserKeyDto keyDto, CancellationToken cancellationToken = default)
        {
            return await GetAsync<User>($"/api/users/me", cancellationToken);
        }

        public async Task<User> UpdateUserAsync(UpdateUserDto dto, CancellationToken cancellationToken = default)
        {
            return await PutAsync<User>("/api/users/me", dto, cancellationToken);
        }

        public async Task DeleteUserAsync(UserKeyDto keyDto, CancellationToken cancellationToken = default)
        {
            await DeleteAsync("/api/users/me", cancellationToken);
        }

        public async Task ChangePasswordAsync(ChangePasswordDto dto, CancellationToken cancellationToken = default)
        {
            await PostAsync("/api/users/me/change-password", dto, cancellationToken);
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
