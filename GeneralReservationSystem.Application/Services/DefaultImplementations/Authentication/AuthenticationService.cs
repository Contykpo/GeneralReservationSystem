using GeneralReservationSystem.Application.DTOs.Authentication;
using GeneralReservationSystem.Application.Entities.Authentication;
using GeneralReservationSystem.Application.Exceptions.Repositories;
using GeneralReservationSystem.Application.Exceptions.Services;
using GeneralReservationSystem.Application.Helpers;
using GeneralReservationSystem.Application.Repositories.Interfaces.Authentication;
using GeneralReservationSystem.Application.Repositories.Util.Interfaces;
using GeneralReservationSystem.Application.Services.Interfaces.Authentication;

namespace GeneralReservationSystem.Application.Services.DefaultImplementations.Authentication
{
    public class AuthenticationService(IUserRepository userRepository) : IAuthenticationService
    {
        public async Task<UserInfo> RegisterUserAsync(RegisterUserDto dto, CancellationToken cancellationToken = default)
        {
            (byte[] hash, byte[] salt) = PasswordHelper.HashPassword(dto.Password);
            User user = new()
            {
                UserName = dto.UserName,
                Email = dto.Email,
                PasswordHash = hash,
                PasswordSalt = salt,
                IsAdmin = false
            };
            try
            {
                _ = await userRepository.CreateAsync(user, cancellationToken);
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
                string normalizedInput = dto.UserNameOrEmail.Trim().ToUpperInvariant();
                User user = await userRepository.Query()
                    .Where(u => u.NormalizedUserName == normalizedInput || u.NormalizedEmail == normalizedInput)
                    .FirstOrDefaultAsync(cancellationToken) ?? throw new ServiceNotFoundException("No se encontró el usuario.");
                return !PasswordHelper.VerifyPassword(dto.Password, user.PasswordHash, user.PasswordSalt)
                    ? throw new ServiceBusinessException("Usuario o contraseña incorrectos.")
                    : new UserInfo
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
                /*User user = await userRepository.Query()
                    .Where(u => u.UserId == dto.UserId)
                    .FirstOrDefaultAsync(cancellationToken) ?? throw new ServiceNotFoundException("No se encontró el usuario para cambiar la contraseña.");*/
                User user = userRepository.Query()
                    .Where(u => u.UserId == dto.UserId)
                    .FirstOrDefault() ?? throw new ServiceNotFoundException("No se encontró el usuario para cambiar la contraseña.");

                if (!PasswordHelper.VerifyPassword(dto.CurrentPassword, user.PasswordHash, user.PasswordSalt))
                {
                    throw new ServiceBusinessException("La contraseña actual es incorrecta.");
                }

                (byte[] hash, byte[] salt) = PasswordHelper.HashPassword(dto.NewPassword);
                user.PasswordHash = hash;
                user.PasswordSalt = salt;
                int affected = await userRepository.UpdateAsync(user, cancellationToken: cancellationToken);
                if (affected == 0)
                {
                    throw new ServiceNotFoundException("No se pudo cambiar la contraseña.");
                }
            }
            catch (RepositoryException ex)
            {
                throw new ServiceException("Error al cambiar la contraseña.", ex);
            }
        }
    }
}
