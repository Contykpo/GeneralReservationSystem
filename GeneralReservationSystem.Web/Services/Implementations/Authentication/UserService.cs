using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.DTOs.Authentication;
using GeneralReservationSystem.Application.Entities.Authentication;
using GeneralReservationSystem.Application.Services.Interfaces.Authentication;

namespace GeneralReservationSystem.Web.Services.Implementations.Authentication
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
    }
}
