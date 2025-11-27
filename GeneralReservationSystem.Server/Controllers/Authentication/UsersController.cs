using FluentValidation;
using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.DTOs.Authentication;
using GeneralReservationSystem.Application.Helpers;
using GeneralReservationSystem.Application.Services.Interfaces.Authentication;
using GeneralReservationSystem.Infrastructure.Helpers;
using GeneralReservationSystem.Server.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static GeneralReservationSystem.Application.Constants;

namespace GeneralReservationSystem.Server.Controllers.Authentication
{
    [Route("api/users")]
    [ApiController]
    public class UsersController(
        IUserService userService,
        IValidator<PagedSearchRequestDto> pagedSearchValidator,
        IValidator<UpdateUserDto> updateUserValidator,
        IValidator<UserKeyDto> userKeyValidator) : ControllerBase
    {
        [HttpGet("search")]
        [Authorize(Roles = AdminRoleName)]
        public async Task<IActionResult> SearchUsers(CancellationToken cancellationToken)
        {
            PagedSearchRequestDto searchDto = new();
            searchDto.PopulateFromQuery(Request.Query);
            await ValidateAsync(pagedSearchValidator, searchDto, cancellationToken);
            PagedResult<UserInfo> result = await userService.SearchUsersAsync(searchDto, cancellationToken);
            return Ok(result);
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
        {
            return await GetUserById((int)CurrentUserId!, cancellationToken);
        }

        [HttpGet("{userId:int}")]
        [Authorize]
        public async Task<IActionResult> GetUserById([FromRoute] int userId, CancellationToken cancellationToken)
        {
            if (!IsOwnerOrAdmin(userId))
            {
                return Forbid();
            }
            UserKeyDto keyDto = new() { UserId = userId };
            await ValidateAsync(userKeyValidator, keyDto, cancellationToken);
            UserInfo userInfo = (await userService.GetUserAsync(keyDto, cancellationToken)).GetUserInfo();
            return Ok(userInfo);
        }

        public async Task<IActionResult> UpdateUser(UpdateUserDto dto, CancellationToken cancellationToken)
        {
            await ValidateAsync(updateUserValidator, dto, cancellationToken);
            UserInfo userInfo = (await userService.UpdateUserAsync(dto, cancellationToken)).GetUserInfo();
            return Ok(userInfo);
        }

        [HttpPatch("me")]
        [Authorize]
        public async Task<IActionResult> UpdateCurrentUser([FromBody] UpdateUserDto dto, CancellationToken cancellationToken)
        {
            dto.UserId = (int)CurrentUserId!;
            return await UpdateUser(dto, cancellationToken);
        }

        [HttpPatch("{userId:int}")]
        [Authorize]
        public async Task<IActionResult> UpdateUserById([FromRoute] int userId, [FromBody] UpdateUserDto dto, CancellationToken cancellationToken)
        {
            if (!IsOwnerOrAdmin(userId))
            {
                return Forbid();
            }
            dto.UserId = userId;
            return await UpdateUser(dto, cancellationToken);
        }

        [HttpDelete("{userId:int}")]
        [Authorize(Roles = AdminRoleName)]
        public async Task<IActionResult> DeleteUserById([FromRoute] int userId, CancellationToken cancellationToken)
        {
            UserKeyDto userKeyDto = new() { UserId = userId };
            await ValidateAsync(userKeyValidator, userKeyDto, cancellationToken);
            UserKeyDto keyDto = new() { UserId = userId };
            await userService.DeleteUserAsync(keyDto, cancellationToken);
            if (userId == CurrentUserId)
            {
                Response.ClearSessionJwtCookie();
            }
            return NoContent();
        }

        [HttpDelete("me")]
        [Authorize]
        public async Task<IActionResult> DeleteCurrentUser(CancellationToken cancellationToken)
        {
            return await DeleteUserById((int)CurrentUserId!, cancellationToken);
        }
    }
}