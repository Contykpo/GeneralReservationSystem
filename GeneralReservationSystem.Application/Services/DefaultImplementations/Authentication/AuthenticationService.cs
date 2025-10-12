using GeneralReservationSystem.Application.DTOs.Authentication;
using GeneralReservationSystem.Application.Entities.Authentication;
using GeneralReservationSystem.Application.Exceptions.Repositories;
using GeneralReservationSystem.Application.Exceptions.Services;
using GeneralReservationSystem.Application.Helpers;
using GeneralReservationSystem.Application.Repositories.Interfaces.Authentication;
using GeneralReservationSystem.Application.Services.Interfaces.Authentication;

namespace GeneralReservationSystem.Application.Services.DefaultImplementations.Authentication
{
    public class AuthenticationService(IUserRepository userRepository) : IAuthenticationService
    {
        public async Task<UserInfo> RegisterUserAsync(RegisterUserDto dto, CancellationToken cancellationToken = default)
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
                return new UserInfo
                {
                    UserId = user.UserId,
                    UserName = user.UserName,
                    Email = user.Email,
                    IsAdmin = user.IsAdmin
                };
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

        public async Task<UserInfo> AuthenticateAsync(LoginDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var normalizedInput = dto.UserNameOrEmail.Trim().ToUpperInvariant();
                var user = await userRepository.Query()
                    .Where(u => u.NormalizedUserName == normalizedInput || u.NormalizedEmail == normalizedInput)
                    .FirstOrDefaultAsync(cancellationToken);
                if (user == null || !PasswordHelper.VerifyPassword(dto.Password, user.PasswordHash, user.PasswordSalt))
                    throw new ServiceBusinessException("Usuario o contraseña incorrectos.");
                return new UserInfo
                {
                    UserId = user.UserId,
                    UserName = user.UserName,
                    Email = user.Email,
                    IsAdmin = user.IsAdmin
                };
            }
            catch (ServiceBusinessException)
            {
                throw;
            }
            catch (RepositoryException ex)
            {
                throw new ServiceException("Error al autenticar el usuario.", ex);
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
            catch (ServiceNotFoundException)
            {
                throw;
            }
            catch (ServiceBusinessException)
            {
                throw;
            }
            catch (RepositoryException ex)
            {
                throw new ServiceException("Error al cambiar la contraseña.", ex);
            }
        }
    }
}
