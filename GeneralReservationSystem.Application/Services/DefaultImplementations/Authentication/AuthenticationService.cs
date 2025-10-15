using GeneralReservationSystem.Application.DTOs.Authentication;
using GeneralReservationSystem.Application.Entities.Authentication;
using GeneralReservationSystem.Application.Exceptions.Repositories;
using GeneralReservationSystem.Application.Exceptions.Services;
using GeneralReservationSystem.Application.Helpers;
using GeneralReservationSystem.Application.Repositories.Interfaces.Authentication;
using GeneralReservationSystem.Application.Services.Interfaces.Authentication;
using Microsoft.AspNetCore.Identity;
using System.Text;

namespace GeneralReservationSystem.Application.Services.DefaultImplementations.Authentication
{
    public class AuthenticationService(IUserRepository userRepository) : IAuthenticationService
    {
        public async Task<UserInfo> RegisterUserAsync(RegisterUserDto dto, CancellationToken cancellationToken = default)
        {
            User user = new();

            var passwordHasher = new Helpers.PasswordHashingHelper<User>();

            string hashedPassword = passwordHasher.HashPassword(user, dto.Password);

            user = new()
            {
                UserName = dto.UserName,
                Email = dto.Email,
                PasswordHash = Encoding.UTF8.GetBytes(hashedPassword),
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
                User? user = await userRepository.Query()
                    .Where(u => u.NormalizedUserName == normalizedInput || u.NormalizedEmail == normalizedInput)
                    .FirstOrDefaultAsync(cancellationToken);

                var passwordHasher = new Helpers.PasswordHashingHelper<User>();

                var verificationResult = passwordHasher.VerifyHashedPassword(user, Encoding.UTF8.GetString(user.PasswordHash), dto.Password);

                return user == null || verificationResult != PasswordVerificationResult.Success
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
                User user = await userRepository.Query()
                    .Where(u => u.UserId == dto.UserId)
                    .FirstOrDefaultAsync(cancellationToken) ?? throw new ServiceNotFoundException("No se encontró el usuario para cambiar la contraseña.");

                var passwordHasher = new Helpers.PasswordHashingHelper<User>();

                var verificationResult = passwordHasher.VerifyHashedPassword(user, System.Text.Encoding.UTF8.GetString(user.PasswordHash), dto.CurrentPassword);
                if (verificationResult != PasswordVerificationResult.Success)
                {
                    throw new ServiceBusinessException("La contraseña actual es incorrecta.");
                }

                string newHashedPassword = passwordHasher.HashPassword(user, dto.NewPassword);

                user.PasswordHash = Encoding.UTF8.GetBytes(newHashedPassword);

                int affected = await userRepository.UpdateAsync(user, cancellationToken: cancellationToken);
                if (affected == 0)
                {
                    throw new ServiceNotFoundException("No se pudo cambiar la contraseña.");
                }
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
