using GeneralReservationSystem.Application.DTOs.Authentication;
using GeneralReservationSystem.Application.Exceptions.Services;
using GeneralReservationSystem.Application.Helpers;
using GeneralReservationSystem.Application.Services.Interfaces.Authentication;
using GeneralReservationSystem.Infrastructure.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GeneralReservationSystem.API.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthenticationController(IAuthenticationService authenticationService, JwtSettings jwtSettings) : ControllerBase
    {
        private readonly JwtSettings _jwtSettings = jwtSettings;

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var userInfo = await authenticationService.RegisterUserAsync(dto, cancellationToken);

                CreateSessionAndLogin(userInfo);

                return Ok(new { message = "Usuario registrado exitosamente", userId = userInfo.UserId });
            }
            catch (ServiceBusinessException ex)
            {
                return Conflict(new { error = ex.Message });
            }
            catch (ServiceException)
            {
                return StatusCode(500, new { error = "Error al registrar el usuario. Por favor, intente nuevamente." });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var userInfo = await authenticationService.AuthenticateAsync(dto, cancellationToken);

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
            JwtHelper.ClearJwtCookie(HttpContext);
            return Ok(new { message = "Cierre de sesión exitoso" });
        }

        [HttpGet("me")]
        [Authorize]
        public IActionResult GetCurrentUser()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = User.FindFirst(ClaimTypes.Name)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var isAdmin = User.IsInRole("Admin");

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            return Ok(new UserInfo
            {
                UserId = int.Parse(userId),
                UserName = userName ?? string.Empty,
                Email = email ?? string.Empty,
                IsAdmin = isAdmin
            });
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || int.Parse(userIdClaim) != dto.UserId)
                    return Unauthorized();

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

            var session = new UserSessionInfo
            {
                UserId = userInfo.UserId,
                UserName = userInfo.UserName,
                Email = userInfo.Email,
                IsAdmin = userInfo.IsAdmin
            };

            JwtHelper.GenerateAndSetJwtCookie(HttpContext, session, _jwtSettings);
        }
    }
}
