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
            IEnumerable<Application.Entities.Authentication.User> users = await userService.GetAllUsersAsync(cancellationToken);
            return Ok(users);
        }

        [HttpPost("search")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SearchUsers([FromBody] PagedSearchRequestDto searchDto, CancellationToken cancellationToken)
        {
            Application.Common.PagedResult<UserInfo> result = await userService.SearchUsersAsync(searchDto, cancellationToken);
            return Ok(result);
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            try
            {
                UserKeyDto keyDto = new() { UserId = int.Parse(userId) };
                Application.Entities.Authentication.User user = await userService.GetUserAsync(keyDto, cancellationToken);
                UserInfo userInfo = new()
                {
                    UserId = user.UserId,
                    UserName = user.UserName,
                    Email = user.Email,
                    IsAdmin = user.IsAdmin
                };
                return Ok(userInfo);
            }
            catch (ServiceNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpGet("{userId:int}")]
        public async Task<IActionResult> GetUserById([FromRoute] int userId, CancellationToken cancellationToken)
        {
            string? currentIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentIdStr))
            {
                return Unauthorized();
            }

            int currentId = int.Parse(currentIdStr);
            if (!User.IsInRole("Admin") && userId != currentId)
            {
                return Forbid();
            }

            try
            {
                UserKeyDto keyDto = new() { UserId = userId };
                Application.Entities.Authentication.User user = await userService.GetUserAsync(keyDto, cancellationToken);
                return Ok(user);
            }
            catch (ServiceNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpPut("me")]
        [Authorize]
        public async Task<IActionResult> UpdateCurrentUser([FromBody] UpdateUserDto dto, CancellationToken cancellationToken)
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            try
            {
                dto.UserId = int.Parse(userId);
                Application.Entities.Authentication.User user = await userService.UpdateUserAsync(dto, cancellationToken);
                UserInfo userInfo = new()
                {
                    UserId = user.UserId,
                    UserName = user.UserName,
                    Email = user.Email,
                    IsAdmin = user.IsAdmin
                };
                return Ok(userInfo);
            }
            catch (ServiceNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ServiceBusinessException ex)
            {
                return Conflict(new { error = ex.Message });
            }
        }

        [HttpPut("{userId:int}")]
        public async Task<IActionResult> UpdateUserById([FromRoute] int userId, [FromBody] UpdateUserDto dto, CancellationToken cancellationToken)
        {
            string? currentIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentIdStr))
            {
                return Unauthorized();
            }

            int currentId = int.Parse(currentIdStr);
            if (!User.IsInRole("Admin") && userId != currentId)
            {
                return Forbid();
            }

            try
            {
                dto.UserId = userId;
                Application.Entities.Authentication.User user = await userService.UpdateUserAsync(dto, cancellationToken);
                UserInfo userInfo = new()
                {
                    UserId = user.UserId,
                    UserName = user.UserName,
                    Email = user.Email,
                    IsAdmin = user.IsAdmin
                };
                return Ok(userInfo);
            }
            catch (ServiceNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ServiceBusinessException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("me")]
        [Authorize]
        public async Task<IActionResult> DeleteCurrentUser(CancellationToken cancellationToken)
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            try
            {
                UserKeyDto keyDto = new() { UserId = int.Parse(userId) };
                await userService.DeleteUserAsync(keyDto, cancellationToken);
                return NoContent();
            }
            catch (ServiceNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpDelete("{userId:int}")]
        public async Task<IActionResult> DeleteUserById([FromRoute] int userId, CancellationToken cancellationToken)
        {
            string? currentIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentIdStr))
            {
                return Unauthorized();
            }

            int currentId = int.Parse(currentIdStr);
            if (!User.IsInRole("Admin") && userId != currentId)
            {
                return Forbid();
            }

            try
            {
                UserKeyDto keyDto = new() { UserId = userId };
                await userService.DeleteUserAsync(keyDto, cancellationToken);
                return NoContent();
            }
            catch (ServiceNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }
    }
}
