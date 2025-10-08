using GeneralReservationSystem.Application.Repositories.Interfaces.Authentication;
using GeneralReservationSystem.Application.Services.Interfaces.Authentication;

namespace GeneralReservationSystem.Application.Services.DefaultImplementations.Authentication
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly ISessionRepository _sessionRepository;

        public AuthenticationService(IUserRepository userRepository, IRoleRepository roleRepository, 
            ISessionRepository sessionRepository)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _sessionRepository = sessionRepository;
        }
    }
}
