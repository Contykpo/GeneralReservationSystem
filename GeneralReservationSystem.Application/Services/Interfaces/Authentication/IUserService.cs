using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.DTOs.Authentication;
using GeneralReservationSystem.Application.Entities.Authentication;

namespace GeneralReservationSystem.Application.Services.Interfaces.Authentication
{
    public interface IUserService
    {
        Task<IEnumerable<User>> GetAllUsersAsync(CancellationToken cancellationToken = default);
        Task<PagedResult<User>> SearchUsersAsync(PagedSearchRequestDto searchDto, CancellationToken cancellationToken = default);
        Task<User> GetUserAsync(UserKeyDto keyDto, CancellationToken cancellationToken = default);
        Task<User> UpdateUserAsync(UpdateUserDto dto, CancellationToken cancellationToken = default);
        Task DeleteUserAsync(UserKeyDto keyDto, CancellationToken cancellationToken = default);
    }
}
