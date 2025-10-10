using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.DTOs.Authentication;
using GeneralReservationSystem.Application.Entities.Authentication;
using GeneralReservationSystem.Application.Exceptions.Repositories;
using GeneralReservationSystem.Application.Exceptions.Services;
using GeneralReservationSystem.Application.Helpers;
using GeneralReservationSystem.Application.Repositories.Interfaces.Authentication;
using GeneralReservationSystem.Application.Services.Interfaces.Authentication;

namespace GeneralReservationSystem.Application.Services.DefaultImplementations.Authentication
{
    public class UserService(IUserRepository userRepository) : IUserService
    {
        public async Task<User> RegisterUserAsync(RegisterUserDto dto, CancellationToken cancellationToken = default)
        {
            var (hash, salt) = PasswordHelper.HashPassword(dto.Password);
            var user = new User
            {
                UserName = dto.UserName,
                Email = dto.Email,
                PasswordHash = hash,
                PasswordSalt = salt,
                IsAdmin = false
            };
            try
            {
                await userRepository.CreateAsync(user, cancellationToken);
                return user;
            }
            catch (UniqueConstraintViolationException ex)
            {
                throw new ServiceBusinessException("Ya existe un usuario con el mismo nombre o correo electrónico.", ex);
            }
            catch (RepositoryException ex)
            {
                throw new ServiceException("Error al registrar el usuario.", ex);
            }
        }

        public async Task<User> AuthenticateAsync(LoginDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var normalizedInput = dto.UserNameOrEmail.Trim().ToUpperInvariant();
                var user = await userRepository.Query()
                    .Where(u => u.NormalizedUserName == normalizedInput || u.NormalizedEmail == normalizedInput)
                    .FirstOrDefaultAsync(cancellationToken);
                if (user == null || !PasswordHelper.VerifyPassword(dto.Password, user.PasswordHash, user.PasswordSalt))
                    throw new ServiceBusinessException("Usuario o contraseña incorrectos.");
                return user;
            }
            catch (RepositoryException ex)
            {
                throw new ServiceException("Error al autenticar el usuario.", ex);
            }
        }

        public async Task<User> GetUserAsync(UserKeyDto keyDto, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await userRepository.Query()
                    .Where(u => u.UserId == keyDto.UserId)
                    .FirstOrDefaultAsync(cancellationToken) ?? throw new ServiceNotFoundException("No se encontró el usuario solicitado.");
                return user;
            }
            catch (RepositoryException ex)
            {
                throw new ServiceException("Error al consultar el usuario.", ex);
            }
        }

        public async Task<User> UpdateUserAsync(UpdateUserDto dto, CancellationToken cancellationToken = default)
        {
            var user = new User { UserId = dto.UserId };
            if (dto.UserName != null)
                user.UserName = dto.UserName;
            if (dto.Email != null)
                user.Email = dto.Email;
            try
            {
                var affected = await userRepository.UpdateAsync(user, cancellationToken);
                if (affected == 0)
                    throw new ServiceNotFoundException("No se encontró el usuario para actualizar.");
                return user;
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
            var user = new User { UserId = keyDto.UserId };
            try
            {
                var affected = await userRepository.DeleteAsync(user, cancellationToken);
                if (affected == 0)
                    throw new ServiceNotFoundException("No se encontró el usuario para eliminar.");
            }
            catch (RepositoryException ex)
            {
                throw new ServiceException("Error al eliminar el usuario.", ex);
            }
        }

        public async Task ChangePasswordAsync(ChangePasswordDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await userRepository.Query()
                    .Where(u => u.UserId == dto.UserId)
                    .FirstOrDefaultAsync(cancellationToken) ?? throw new ServiceNotFoundException("No se encontró el usuario para cambiar la contraseña.");
                if (!PasswordHelper.VerifyPassword(dto.CurrentPassword, user.PasswordHash, user.PasswordSalt))
                    throw new ServiceBusinessException("La contraseña actual es incorrecta.");
                var (hash, salt) = PasswordHelper.HashPassword(dto.NewPassword);
                user.PasswordHash = hash;
                user.PasswordSalt = salt;
                var affected = await userRepository.UpdateAsync(user, cancellationToken);
                if (affected == 0)
                    throw new ServiceNotFoundException("No se pudo cambiar la contraseña.");
            }
            catch (RepositoryException ex)
            {
                throw new ServiceException("Error al cambiar la contraseña.", ex);
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

        public async Task<PagedResult<User>> SearchUsersAsync(PagedSearchRequestDto searchDto, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = userRepository.Query()
                    .ApplyFilters(searchDto.Filters)
                    .ApplySorting(searchDto.Orders)
                    .Page(searchDto.Page, searchDto.PageSize);
                return await query.ToPagedResultAsync(cancellationToken);
            }
            catch (RepositoryException ex)
            {
                throw new ServiceException("Error al buscar usuarios.", ex);
            }

        }

        public Task LogoutAsync(CancellationToken cancellationToken = default)
        {
            // Logout is performed by the API layer (clearing cookie). Application service has nothing to do here.
            return Task.CompletedTask;
        }
    }
}
