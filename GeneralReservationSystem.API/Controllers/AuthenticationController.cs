using GeneralReservationSystem.Application.DTOs.Authentication;
using GeneralReservationSystem.Application.Entities.Authentication;
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
    public class AuthenticationController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly JwtSettings _jwtSettings;

        public AuthenticationController(IUserService userService, JwtSettings jwtSettings)
        {
            _userService = userService;
            _jwtSettings = jwtSettings;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userService.RegisterUserAsync(dto, cancellationToken);

                CreateSessionAndLogin(user);
                
                return Ok(new { message = "Usuario registrado exitosamente", userId = user.UserId });
            }
            catch (ServiceBusinessException)
            {
                return BadRequest(new { error = "El nombre de usuario o correo electrónico ya está en uso. Por favor, elija otro." });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userService.AuthenticateAsync(dto, cancellationToken);

                CreateSessionAndLogin(user);

                return Ok(new { message = "Inicio de sesión exitoso", userId = user.UserId, isAdmin = user.IsAdmin });
            }
            catch (ServiceBusinessException)
            {
                return BadRequest(new { error = "Credenciales incorrectas. Verifique su nombre de usuario/correo y contraseña." });
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
            var userId      = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName    = User.FindFirst(ClaimTypes.Name)?.Value;
            var email       = User.FindFirst(ClaimTypes.Email)?.Value;
            var isAdmin     = User.IsInRole("Admin");

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { error = "No hay una sesión activa." });

            return Ok(new
            {
                userId      = int.Parse(userId),
                userName    = userName ?? string.Empty,
                email       = email ?? string.Empty,
                isAdmin     = isAdmin
            });
        }

        private void CreateSessionAndLogin(User user)
        {
            ThrowHelpers.ThrowIfNull(user, nameof(user));

			var session = new UserSessionInfo
			{
				UserId      = user.UserId,
				UserName    = user.UserName,
				Email       = user.Email,
				IsAdmin     = user.IsAdmin
			};

            JwtHelper.GenerateAndSetJwtCookie(HttpContext, session, _jwtSettings);
		}
    }
}
