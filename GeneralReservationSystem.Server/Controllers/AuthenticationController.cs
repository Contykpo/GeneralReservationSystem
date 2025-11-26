using FluentValidation;
using GeneralReservationSystem.Application.DTOs.Authentication;
using GeneralReservationSystem.Application.Exceptions.Services;
using GeneralReservationSystem.Application.Helpers;
using GeneralReservationSystem.Application.Services.Interfaces.Authentication;
using GeneralReservationSystem.Infrastructure.Helpers;
using GeneralReservationSystem.Server.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GeneralReservationSystem.Server.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthenticationController(
        IAuthenticationService authenticationService,
        JwtSettings jwtSettings,
        IValidator<RegisterUserDto> registerValidator,
        IValidator<LoginDto> loginValidator,
        IValidator<ChangePasswordDto> changePasswordValidator) : ControllerBase
    {
        private async Task<(IActionResult actionResult, UserInfo? createdUser)> RegisterUserAsync(RegisterUserDto dto, bool isAdmin, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = await ValidationHelper.ValidateAsync(registerValidator, dto, cancellationToken);
            if (validationResult != null)
            {
                return new(validationResult, null);
            }
            try
            {
                UserInfo userInfo = isAdmin ? await authenticationService.RegisterAdminAsync(dto, cancellationToken) : await authenticationService.RegisterUserAsync(dto, cancellationToken);

                return new(
                    Ok(new { message = "Administrador registrado exitosamente", userId = userInfo.UserId }),
                    userInfo);
            }
            catch (ServiceBusinessException ex)
            {
                return new(Conflict(new { error = ex.Message }), null);
            }
            catch (ServiceException)
            {
                return new(StatusCode(500, new { error = "Error al registrar el administrador. Por favor, intente nuevamente." }), null);
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserDto dto, CancellationToken cancellationToken)
        {
            (IActionResult? actionResult, UserInfo? createdUser) = await RegisterUserAsync(dto, isAdmin: false, cancellationToken);

            if (createdUser is not null)
            {
                CreateSessionAndLogin(createdUser);
            }

            return actionResult;
        }

        [HttpPost("register-admin")]
        public async Task<IActionResult> RegisterAdmin([FromBody] RegisterUserDto dto, CancellationToken cancellationToken)
        {
            (IActionResult? registrationResult, _) = await RegisterUserAsync(dto, isAdmin: true, cancellationToken);

            return registrationResult;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = await ValidationHelper.ValidateAsync(loginValidator, dto, cancellationToken);
            if (validationResult != null)
            {
                return validationResult;
            }

            try
            {
                UserInfo userInfo = await authenticationService.AuthenticateAsync(dto, cancellationToken);

                CreateSessionAndLogin(userInfo);

                return Ok(new { message = "Inicio de sesión exitoso", userId = userInfo.UserId, isAdmin = userInfo.IsAdmin });
            }
            catch (ServiceBusinessException ex)
            {
                return Conflict(new { error = ex.Message });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            HttpContext.ClearJwtCookie();
            return Ok(new { message = "Cierre de sesión exitoso" });
        }

        [HttpGet("me")]
        [Authorize]
        public IActionResult GetCurrentUser()
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            string? userName = User.FindFirst(ClaimTypes.Name)?.Value;
            string? email = User.FindFirst(ClaimTypes.Email)?.Value;
            bool isAdmin = User.IsInRole("Admin");

            return string.IsNullOrEmpty(userId)
                ? Unauthorized()
                : Ok(new UserInfo
                {
                    UserId = int.Parse(userId),
                    UserName = userName ?? string.Empty,
                    Email = email ?? string.Empty,
                    IsAdmin = isAdmin
                });
        }

        [HttpPatch("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = await ValidationHelper.ValidateAsync(changePasswordValidator, dto, cancellationToken);
            if (validationResult != null)
            {
                return validationResult;
            }

            try
            {
                string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || int.Parse(userIdClaim) != dto.UserId)
                {
                    return Unauthorized();
                }

                await authenticationService.ChangePasswordAsync(dto, cancellationToken);
                return Ok(new { message = "Contraseña cambiada exitosamente." });
            }
            catch (ServiceBusinessException ex)
            {
                return Conflict(new { error = ex.Message });
            }
            catch (ServiceNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        private void CreateSessionAndLogin(UserInfo userInfo)
        {
            ThrowHelpers.ThrowIfNull(userInfo, nameof(userInfo));

            UserSessionInfo session = new()
            {
                UserId = userInfo.UserId,
                UserName = userInfo.UserName,
                Email = userInfo.Email,
                IsAdmin = userInfo.IsAdmin
            };

            _ = HttpContext.GenerateAndSetJwtCookie(session, jwtSettings);
        }
    }
}
