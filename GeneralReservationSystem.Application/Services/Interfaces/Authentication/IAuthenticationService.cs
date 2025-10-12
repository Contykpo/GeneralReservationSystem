using GeneralReservationSystem.Application.DTOs.Authentication;

namespace GeneralReservationSystem.Application.Services.Interfaces.Authentication
{
    public interface IAuthenticationService
    {
        Task<UserInfo> RegisterUserAsync(RegisterUserDto dto, CancellationToken cancellationToken = default);
        Task<UserInfo> AuthenticateAsync(LoginDto dto, CancellationToken cancellationToken = default);
        Task ChangePasswordAsync(ChangePasswordDto dto, CancellationToken cancellationToken = default);
    }
}
