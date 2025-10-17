using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.DTOs.Authentication;
using GeneralReservationSystem.Application.Entities.Authentication;
using GeneralReservationSystem.Application.Exceptions.Repositories;
using GeneralReservationSystem.Application.Exceptions.Services;
using GeneralReservationSystem.Application.Repositories.Interfaces.Authentication;
using GeneralReservationSystem.Application.Services.Interfaces.Authentication;

namespace GeneralReservationSystem.Application.Services.DefaultImplementations.Authentication
{
    public class UserService(IUserRepository userRepository) : IUserService
    {
        public async Task<User> GetUserAsync(UserKeyDto keyDto, CancellationToken cancellationToken = default)
        {
            try
            {
                /*User user = await userRepository.Query()
                    .Where(u => u.UserId == keyDto.UserId)
                    .FirstOrDefaultAsync(cancellationToken) ?? throw new ServiceNotFoundException("No se encontró el usuario solicitado.");
                return user;*/

                User user = userRepository.Query().Where(u => u.UserId == keyDto.UserId).FirstOrDefault() ?? throw new ServiceNotFoundException("No se encontró el usuario solicitado.");
                return user;
            }
            catch (RepositoryException ex)
            {
                throw new ServiceException("Error al consultar el usuario.", ex);
            }
        }

        public async Task<User> UpdateUserAsync(UpdateUserDto dto, CancellationToken cancellationToken = default)
        {
            User user = new() { UserId = dto.UserId };
            bool hasUpdates = false;

            if (dto.UserName != null)
            {
                user.UserName = dto.UserName;
                hasUpdates = true;
            }
            if (dto.Email != null)
            {
                user.Email = dto.Email;
                hasUpdates = true;
            }

            if (!hasUpdates)
            {
                // Nothing to update, return the existing user
                return await GetUserAsync(new UserKeyDto { UserId = dto.UserId }, cancellationToken);
            }

            try
            {
                // Build selector based on what properties are being updated
                Func<User, object?> selector = dto.UserName != null && dto.Email != null
                    ? (u => new { u.UserName, u.Email })
                    : dto.UserName != null ? (u => u.UserName) : (u => u.Email);
                int affected = await userRepository.UpdateAsync(
                    user,
                    selector,
                    cancellationToken: cancellationToken
                );
                return affected == 0 ? throw new ServiceNotFoundException("No se encontró el usuario para actualizar.") : user;
            }
            catch (UniqueConstraintViolationException ex)
            {
                throw new ServiceBusinessException("Ya existe un usuario con el mismo nombre o correo electrónico.", ex);
            }
            catch (RepositoryException ex)
            {
                throw new ServiceException("Error al actualizar el usuario.", ex);
            }
        }

        public async Task DeleteUserAsync(UserKeyDto keyDto, CancellationToken cancellationToken = default)
        {
            User user = new() { UserId = keyDto.UserId };
            try
            {
                int affected = await userRepository.DeleteAsync(user, cancellationToken);
                if (affected == 0)
                {
                    throw new ServiceNotFoundException("No se encontró el usuario para eliminar.");
                }
            }
            catch (RepositoryException ex)
            {
                throw new ServiceException("Error al eliminar el usuario.", ex);
            }
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await userRepository.GetAllAsync(cancellationToken);
            }
            catch (RepositoryException ex)
            {
                throw new ServiceException("Error al obtener la lista de usuarios.", ex);
            }
        }

        public async Task<PagedResult<UserInfo>> SearchUsersAsync(PagedSearchRequestDto searchDto, CancellationToken cancellationToken = default)
        {
            try
            {
                /*var paginatedResults = userRepository.Query()
                    .Select(u => new UserInfo
                    {
                        UserId = u.UserId,
                        UserName = u.UserName,
                        Email = u.Email,
                        IsAdmin = u.IsAdmin
                    })
                    .ApplyFilters(searchDto.Filters)
                    .ApplySorting(searchDto.Orders)
                    .Page(searchDto.Page, searchDto.PageSize)
                    .ToPagedResultAsync(cancellationToken);
                return await paginatedResults; */

                var query = userRepository.Query()
                    .Select(u => new UserInfo
                    {
                        UserId = u.UserId,
                        UserName = u.UserName,
                        Email = u.Email,
                        IsAdmin = u.IsAdmin
                    });

                var count = query.Count();

                var items = query.Skip((searchDto.Page - 1) * searchDto.PageSize)
                    .Take(searchDto.PageSize)
                    .ToList();

                return new PagedResult<UserInfo>
                {
                    Items = items,
                    TotalCount = count,
                    Page = searchDto.Page,
                    PageSize = searchDto.PageSize
                };
            }
            catch (RepositoryException ex)
            {
                throw new ServiceException("Error al buscar usuarios.", ex);
            }

        }
    }
}
