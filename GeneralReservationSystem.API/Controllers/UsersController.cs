using FluentValidation;
using GeneralReservationSystem.API.Helpers;
using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.DTOs.Authentication;
using GeneralReservationSystem.Application.Exceptions.Services;
using GeneralReservationSystem.Application.Helpers;
using GeneralReservationSystem.Application.Services.Interfaces.Authentication;
using GeneralReservationSystem.Infrastructure.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static GeneralReservationSystem.Application.Constants;

namespace GeneralReservationSystem.API.Controllers
{
    [Route("api/users")]
    [ApiController]
    [Authorize]
    public class UsersController(IUserService userService, IValidator<PagedSearchRequestDto> pagedSearchValidator, IValidator<UpdateUserDto> updateUserValidator, IValidator<UserKeyDto> userKeyValidator) : ControllerBase
    {
        [HttpGet("search")]
        [Authorize(Roles = AdminRoleName)]
        public async Task<IActionResult> SearchUsers(CancellationToken cancellationToken)
        {
            PagedSearchRequestDto searchDto = new();
            searchDto.PopulateFromQuery(Request.Query);
            IActionResult? validationResult = await ValidationHelper.ValidateAsync(pagedSearchValidator, searchDto, cancellationToken);
            if (validationResult != null)
            {
                return validationResult;
            }
            PagedResult<UserInfo> result = await userService.SearchUsersAsync(searchDto, cancellationToken);
            return Ok(result);
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return string.IsNullOrEmpty(userId) ? Unauthorized() : await GetUserById(int.Parse(userId), cancellationToken);
        }

        [HttpGet("{userId:int}")]
        [Authorize]
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

            UserKeyDto keyDto = new() { UserId = userId };

            IActionResult? validationResult = await ValidationHelper.ValidateAsync(userKeyValidator, keyDto, cancellationToken);

            if (validationResult != null)
            {
                return validationResult;
            }

            try
            {
                UserInfo userInfo = (await userService.GetUserAsync(keyDto, cancellationToken)).GetUserInfo();

                return Ok(userInfo);
            }
            catch (ServiceNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        public async Task<IActionResult> UpdateUser(UpdateUserDto dto, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = await ValidationHelper.ValidateAsync(updateUserValidator, dto, cancellationToken);

            if (validationResult != null)
            {
                return validationResult;
            }

            try
            {
                UserInfo userInfo = (await userService.UpdateUserAsync(dto, cancellationToken)).GetUserInfo();

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

        [HttpPatch("me")]
        [Authorize]
        public async Task<IActionResult> UpdateCurrentUser([FromBody] UpdateUserDto dto, CancellationToken cancellationToken)
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            dto.UserId = int.Parse(userId);

            return await UpdateUser(dto, cancellationToken);
        }

        [HttpPatch("{userId:int}")]
        [Authorize]
        public async Task<IActionResult> UpdateUserById([FromRoute] int userId, [FromBody] UpdateUserDto dto, CancellationToken cancellationToken)
        {
            dto.UserId = userId;

            string? currentIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(currentIdStr))
            {
                return Unauthorized();
            }

            int currentId = int.Parse(currentIdStr);

            return !User.IsInRole("Admin") && userId != currentId ? Forbid() : await UpdateUser(dto, cancellationToken);
        }

        [HttpDelete("{userId:int}")]
        [Authorize(Roles = AdminRoleName)]
        public async Task<IActionResult> DeleteUserById([FromRoute] int userId, CancellationToken cancellationToken)
        {
            string? currentIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(currentIdStr))
            {
                return Unauthorized();
            }

            int currentId = int.Parse(currentIdStr);

            UserKeyDto userKeyDto = new() { UserId = userId };

            IActionResult? validationResult = await ValidationHelper.ValidateAsync(userKeyValidator, userKeyDto, cancellationToken);

            if (validationResult != null)
            {
                return validationResult;
            }

            try
            {
                UserKeyDto keyDto = new() { UserId = userId };
                await userService.DeleteUserAsync(keyDto, cancellationToken);

                //Si el usuario pretende eliminarse a si mismo, se elimina la cookie con el JWT.
                if (userId == currentId)
                {
                    Response.ClearJwtCookie();
                }

                return NoContent();
            }
            catch (ServiceNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpDelete("me")]
        [Authorize]
        public async Task<IActionResult> DeleteCurrentUser(CancellationToken cancellationToken)
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return string.IsNullOrEmpty(userId) ? Unauthorized() : await DeleteUserById(int.Parse(userId), cancellationToken);
        }
    }
}