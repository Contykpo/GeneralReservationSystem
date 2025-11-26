using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.DTOs.Authentication;
using GeneralReservationSystem.Application.Entities.Authentication;
using GeneralReservationSystem.Web.Client.Helpers;
using GeneralReservationSystem.Web.Client.Services.Interfaces.Authentication;

namespace GeneralReservationSystem.Web.Client.Services.Implementations
{
    public class ClientUserService(HttpClient httpClient) : ClientServiceBase(httpClient), IClientUserService
    {
        public async Task<IEnumerable<User>> GetAllUsersAsync(CancellationToken cancellationToken = default)
        {
            return await GetAsync<IEnumerable<User>>("/api/users", cancellationToken);
        }

        public async Task<PagedResult<UserInfo>> SearchUsersAsync(PagedSearchRequestDto searchDto, CancellationToken cancellationToken = default)
        {
            string query = searchDto.ToQueryString();
            return await GetAsync<PagedResult<UserInfo>>($"/api/users/search?{query}", cancellationToken);
        }

        public async Task<User> GetUserAsync(UserKeyDto keyDto, CancellationToken cancellationToken = default)
        {
            return await GetAsync<User>($"/api/users/{keyDto.UserId}", cancellationToken);
        }

        public async Task<User> UpdateUserAsync(UpdateUserDto dto, CancellationToken cancellationToken = default)
        {
            return await PatchAsync<User>($"/api/users/{dto.UserId}", dto, cancellationToken);
        }

        public async Task DeleteUserAsync(UserKeyDto keyDto, CancellationToken cancellationToken = default)
        {
            await DeleteAsync($"/api/users/{keyDto.UserId}", cancellationToken);
        }
    }
}
