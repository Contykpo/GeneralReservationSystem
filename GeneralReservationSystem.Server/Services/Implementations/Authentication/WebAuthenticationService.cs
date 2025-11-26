using FluentValidation;
using GeneralReservationSystem.Application.DTOs.Authentication;
using GeneralReservationSystem.Application.Services.Interfaces.Authentication;
using GeneralReservationSystem.Web.Client.Services.Interfaces.Authentication;
using System.Security.Claims;

namespace GeneralReservationSystem.Server.Services.Implementations.Authentication
{
    public class WebAuthenticationService(
        IAuthenticationService authenticationService,
        IHttpContextAccessor httpContextAccessor,
        IValidator<RegisterUserDto> registerValidator,
        IValidator<LoginDto> loginValidator,
        IValidator<ChangePasswordDto> changePasswordValidator) : WebServiceBase(httpContextAccessor), IClientAuthenticationService
    {
        public Task<UserInfo> GetCurrentUserAsync(CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();

            string? userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            string? userName = User?.FindFirst(ClaimTypes.Name)?.Value;
            string? email = User?.FindFirst(ClaimTypes.Email)?.Value;
            bool isAdmin = User?.IsInRole("Admin") ?? false;

            return Task.FromResult(new UserInfo
            {
                UserId = int.Parse(userId!),
                UserName = userName ?? string.Empty,
                Email = email ?? string.Empty,
                IsAdmin = isAdmin
            });
        }

        public async Task<UserInfo> RegisterUserAsync(RegisterUserDto dto, CancellationToken cancellationToken = default)
        {
            await ValidateAsync(registerValidator, dto, cancellationToken);
            return await authenticationService.RegisterUserAsync(dto, cancellationToken);
        }

        public async Task<UserInfo> RegisterAdminAsync(RegisterUserDto dto, CancellationToken cancellationToken = default)
        {
            await ValidateAsync(registerValidator, dto, cancellationToken);
            return await authenticationService.RegisterAdminAsync(dto, cancellationToken);
        }

        public async Task<UserInfo> AuthenticateAsync(LoginDto dto, CancellationToken cancellationToken = default)
        {
            await ValidateAsync(loginValidator, dto, cancellationToken);
            return await authenticationService.AuthenticateAsync(dto, cancellationToken);
        }

        public async Task LogoutAsync(CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
        }

        public async Task ChangePasswordAsync(ChangePasswordDto dto, CancellationToken cancellationToken = default)
        {
            await ValidateAsync(changePasswordValidator, dto, cancellationToken);
            EnsureOwnerOrAdmin(dto.UserId);
            await authenticationService.ChangePasswordAsync(dto, cancellationToken);
        }
    }
}


