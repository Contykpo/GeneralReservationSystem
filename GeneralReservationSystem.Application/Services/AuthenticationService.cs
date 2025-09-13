using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities.Authentication;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using GeneralReservationSystem.Application.Repositories.Interfaces.Authentication;
using System.Text;

namespace GeneralReservationSystem.Application.Services
{
    public class AuthenticationService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly ISessionRepository _sessionRepository;

        public AuthenticationService(IUserRepository userRepository, IRoleRepository roleRepository, ISessionRepository sessionRepository)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _sessionRepository = sessionRepository;
        }

        private static bool VerifyPassword(string password, byte[] storedHash, byte[] storedSalt)
        {
            if (string.IsNullOrWhiteSpace(password)) return false;
            if (storedHash == null || storedHash.Length == 0) return false;
            if (storedSalt == null || storedSalt.Length == 0) return false;

            using (var hmac = new System.Security.Cryptography.HMACSHA512(storedSalt))
            {
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                if (computedHash.Length != storedHash.Length) return false;
                for (int i = 0; i < computedHash.Length; i++)
                {
                    if (computedHash[i] != storedHash[i]) return false;
                }
            }
            return true;
        }

        public async Task<NewSessionDTO> LoginAsync(UserLoginDTO userLoginDTO)
        {
            ApplicationUser? user = null;
            (await _userRepository.GetByEmailAsync(userLoginDTO.Email))
                .IfValue((u) =>
                {
                    user = u;
                })
                .IfEmpty(() => throw new UnauthorizedAccessException("Invalid email or password."))
                .IfError((error) => throw new UnauthorizedAccessException($"Error while retrieving user: {error}"));

            // Ensure user, PasswordHash, and PasswordSalt are not null before calling VerifyPassword
            if (!VerifyPassword(userLoginDTO.Password, user!.PasswordHash, user!.PasswordSalt))
                throw new UnauthorizedAccessException("Invalid email or password.");

            var newSession = new UserSession
            {
                UserID = user.UserId,
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(1), // Example: session valid for 1 hour
                SessionInfo = "User logged in from IP XYZ" // Example info
            };

            (await _sessionRepository.CreateSessionAsync(newSession)).IfFailure((error) =>
            {
                throw new Exception($"Error creating user session: {error}");
            });

            return new NewSessionDTO
            {
                SessionId = newSession.SessionID,
                UserId = newSession.UserID,
                CreatedAt = newSession.CreatedAt.UtcDateTime,
                ExpiresAt = newSession.ExpiresAt?.UtcDateTime ?? DateTime.MinValue
            };
        }

        public async Task<NewSessionDTO> RegisterAsync(UserRegisterDTO userRegisterDTO)
        {
            // Check if user with the same email already exists
            bool userExists = false;
            (await _userRepository.ExistsWithEmailAsync(userRegisterDTO.Email))
                .IfValue((exists) => userExists = exists)
                .IfError((error) => throw new Exception($"Error checking existing user: {error}"));
            if (userExists)
                throw new InvalidOperationException("A user with this email already exists.");

            // TODO: Quizás se pueda optimizar obteniendo el rol y creando el usuario en una sola transaccion.
            // TODO: Quizás se pueda optimizar obteniendo el rol una sola vez y cacheandolo.
            // TODO: Quizás se pueda omitir la asignacion del rol por defecto en el registro y asumir tal rol en case que no tenga
            // fila asociada en UserRoles.
            ApplicationRole? defaultRole = null;
            (await _roleRepository.GetByNameAsync("Customer"))
                .IfValue((role) => defaultRole = role)
                .IfEmpty(() => throw new Exception("Default role 'Customer' not found."))
                .IfError((error) => throw new Exception($"Error retrieving default role: {error}"));

            // Create password hash and salt
            using var hmac = new System.Security.Cryptography.HMACSHA512();
            var passwordSalt = hmac.Key;
            var passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(userRegisterDTO.Password));

            var newUser = new ApplicationUser
            {
                Email = userRegisterDTO.Email,
                UserName = userRegisterDTO.UserName,
                NormalizedUserName = userRegisterDTO.UserName.ToUpperInvariant(),
                NormalizedEmail = userRegisterDTO.Email.ToUpperInvariant(),
                EmailConfirmed = false,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt
            };

            // Insert the new user into the database and assign default role
            (await _userRepository.CreateUserAsync(newUser, new[] { defaultRole! }))
                .IfFailure((error) => throw new Exception($"Error creating user: {error}"));

            // Automatically log in the newly registered user
            return await LoginAsync(new UserLoginDTO
            {
                Email = userRegisterDTO.Email,
                Password = userRegisterDTO.Password
            });
        }

        public async Task<OperationResult> LogoutAsync(Guid sessionId)
        {
            return await _sessionRepository.DeleteSessionAsync(sessionId);
        }
    }
}
