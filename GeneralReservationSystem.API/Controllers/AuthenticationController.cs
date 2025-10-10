using GeneralReservationSystem.Application.DTOs.Authentication;
using GeneralReservationSystem.Application.Exceptions.Services;
using GeneralReservationSystem.Application.Services.Interfaces.Authentication;
using GeneralReservationSystem.Infrastructure.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
                var session = new UserSessionInfo
                {
                    UserId = user.UserId,
                    UserName = user.UserName,
                    Email = user.Email,
                    IsAdmin = user.IsAdmin
                };
                
                var token = JwtHelper.GenerateJwtToken(session, _jwtSettings);
                JwtHelper.SetJwtCookie(HttpContext, token, _jwtSettings.ExpirationDays);
                
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
                var session = new UserSessionInfo
                {
                    UserId = user.UserId,
                    UserName = user.UserName,
                    Email = user.Email,
                    IsAdmin = user.IsAdmin
                };
                
                var token = JwtHelper.GenerateJwtToken(session, _jwtSettings);
                JwtHelper.SetJwtCookie(HttpContext, token, _jwtSettings.ExpirationDays);
                
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
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var isAdmin = User.IsInRole("Admin");

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { error = "No hay una sesión activa." });

            return Ok(new
            {
                userId = int.Parse(userId),
                userName = userName ?? string.Empty,
                email = email ?? string.Empty,
                isAdmin = isAdmin
            });
        }
    }
}
