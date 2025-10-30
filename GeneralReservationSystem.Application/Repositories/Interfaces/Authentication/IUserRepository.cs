using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.DTOs.Authentication;
using GeneralReservationSystem.Application.Entities.Authentication;

namespace GeneralReservationSystem.Application.Repositories.Interfaces.Authentication
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken);
        Task<User?> GetByUserNameOrEmailAsync(string normalizedInput, CancellationToken cancellationToken);
        Task<PagedResult<UserInfo>> SearchWithInfoAsync(PagedSearchRequestDto searchDto, CancellationToken cancellationToken);
    }
}