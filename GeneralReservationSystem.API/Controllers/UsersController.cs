using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.DTOs.Authentication;
using GeneralReservationSystem.Application.Exceptions.Services;
using GeneralReservationSystem.Application.Services.Interfaces.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GeneralReservationSystem.API.Controllers
{
    [Route("api/users")]
    [ApiController]
    [Authorize]
    public class UsersController(IUserService userService) : ControllerBase
    {
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsers(CancellationToken cancellationToken)
        {
            var users = await userService.GetAllUsersAsync(cancellationToken);
            return Ok(users);
        }

        [HttpPost("search")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SearchUsers([FromBody] PagedSearchRequestDto searchDto, CancellationToken cancellationToken)
        {
            var result = await userService.SearchUsersAsync(searchDto, cancellationToken);
            return Ok(result);
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { error = "No hay una sesión activa." });

            try
            {
                var keyDto = new UserKeyDto { UserId = int.Parse(userId) };
                var user = await userService.GetUserAsync(keyDto, cancellationToken);
                return Ok(user);
            }
            catch (ServiceNotFoundException)
            {
                return NotFound(new { error = "No se encontró el usuario." });
            }
        }

        [HttpGet("{userId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUserById([FromRoute] int userId, CancellationToken cancellationToken)
        {
            try
            {
                var keyDto = new UserKeyDto { UserId = userId };
                var user = await userService.GetUserAsync(keyDto, cancellationToken);
                return Ok(user);
            }
            catch (ServiceNotFoundException)
            {
                return NotFound(new { error = $"No se encontró el usuario con ID {userId}." });
            }
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateCurrentUser([FromBody] UpdateUserDto dto, CancellationToken cancellationToken)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { error = "No hay una sesión activa." });

            try
            {
                dto.UserId = int.Parse(userId);
                var user = await userService.UpdateUserAsync(dto, cancellationToken);
                return Ok(user);
            }
            catch (ServiceNotFoundException)
            {
                return NotFound(new { error = "No se encontró el usuario para actualizar." });
            }
            catch (ServiceBusinessException)
            {
                return BadRequest(new { error = "El nombre de usuario o correo electrónico ya está en uso por otro usuario." });
            }
        }

        [HttpPut("{userId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUserById([FromRoute] int userId, [FromBody] UpdateUserDto dto, CancellationToken cancellationToken)
        {
            try
            {
                dto.UserId = userId;
                var user = await userService.UpdateUserAsync(dto, cancellationToken);
                return Ok(user);
            }
            catch (ServiceNotFoundException)
            {
                return NotFound(new { error = $"No se encontró el usuario con ID {userId} para actualizar." });
            }
            catch (ServiceBusinessException)
            {
                return BadRequest(new { error = "El nombre de usuario o correo electrónico ya está en uso por otro usuario." });
            }
        }

        [HttpDelete("me")]
        public async Task<IActionResult> DeleteCurrentUser(CancellationToken cancellationToken)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { error = "No hay una sesión activa." });

            try
            {
                var keyDto = new UserKeyDto { UserId = int.Parse(userId) };
                await userService.DeleteUserAsync(keyDto, cancellationToken);
                return NoContent();
            }
            catch (ServiceNotFoundException)
            {
                return NotFound(new { error = "No se encontró el usuario para eliminar." });
            }
        }

        [HttpDelete("{userId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUserById([FromRoute] int userId, CancellationToken cancellationToken)
        {
            try
            {
                var keyDto = new UserKeyDto { UserId = userId };
                await userService.DeleteUserAsync(keyDto, cancellationToken);
                return NoContent();
            }
            catch (ServiceNotFoundException)
            {
                return NotFound(new { error = $"No se encontró el usuario con ID {userId} para eliminar." });
            }
        }

        [HttpPost("me/change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto, CancellationToken cancellationToken)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { error = "No hay una sesión activa." });

            try
            {
                dto.UserId = int.Parse(userId);
                await userService.ChangePasswordAsync(dto, cancellationToken);
                return Ok(new { message = "Contraseña cambiada exitosamente" });
            }
            catch (ServiceNotFoundException)
            {
                return NotFound(new { error = "No se encontró el usuario." });
            }
            catch (ServiceBusinessException)
            {
                return BadRequest(new { error = "La contraseña actual es incorrecta. Por favor, verifíquela e intente nuevamente." });
            }
        }
    }
}
