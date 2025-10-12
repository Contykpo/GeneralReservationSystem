using GeneralReservationSystem.Application.DTOs.Authentication;
using GeneralReservationSystem.Application.Services.Interfaces.Authentication;

namespace GeneralReservationSystem.Web.Services.Interfaces.Authentication
{
    public interface IClientAuthenticationService : IAuthenticationService
    {
        Task LogoutAsync(CancellationToken cancellationToken = default);
        Task<UserInfo> GetCurrentUserAsync(CancellationToken cancellationToken = default);
    }
}
