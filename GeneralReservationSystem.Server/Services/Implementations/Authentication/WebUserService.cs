using FluentValidation;
using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.DTOs.Authentication;
using GeneralReservationSystem.Application.Entities.Authentication;
using GeneralReservationSystem.Application.Services.Interfaces.Authentication;
using GeneralReservationSystem.Web.Client.Services.Interfaces.Authentication;

namespace GeneralReservationSystem.Server.Services.Implementations.Authentication
{
    public class WebUserService(
        IUserService userService,
        IHttpContextAccessor httpContextAccessor,
        IValidator<PagedSearchRequestDto> pagedSearchValidator,
        IValidator<UpdateUserDto> updateUserValidator,
        IValidator<UserKeyDto> userKeyValidator) : WebServiceBase(httpContextAccessor), IClientUserService
    {
        public async Task<IEnumerable<User>> GetAllUsersAsync(CancellationToken cancellationToken = default)
        {
            EnsureAdmin();
            return await userService.GetAllUsersAsync(cancellationToken);
        }

        public async Task<PagedResult<UserInfo>> SearchUsersAsync(PagedSearchRequestDto searchDto, CancellationToken cancellationToken = default)
        {
            EnsureAdmin();
            await ValidateAsync(pagedSearchValidator, searchDto, cancellationToken);
            return await userService.SearchUsersAsync(searchDto, cancellationToken);
        }

        public async Task<User> GetUserAsync(UserKeyDto keyDto, CancellationToken cancellationToken = default)
        {
            EnsureOwnerOrAdmin(keyDto.UserId);
            await ValidateAsync(userKeyValidator, keyDto, cancellationToken);
            return await userService.GetUserAsync(keyDto, cancellationToken);
        }

        public async Task<User> UpdateUserAsync(UpdateUserDto dto, CancellationToken cancellationToken = default)
        {
            EnsureOwnerOrAdmin(dto.UserId);
            await ValidateAsync(updateUserValidator, dto, cancellationToken);
            return await userService.UpdateUserAsync(dto, cancellationToken);
        }

        public async Task DeleteUserAsync(UserKeyDto keyDto, CancellationToken cancellationToken = default)
        {
            EnsureOwnerOrAdmin(keyDto.UserId);
            await ValidateAsync(userKeyValidator, keyDto, cancellationToken);
            await userService.DeleteUserAsync(keyDto, cancellationToken);
        }
    }
}
