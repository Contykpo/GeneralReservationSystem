using GeneralReservationSystem.Application.Entities.Authentication;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using GeneralReservationSystem.Application.Repositories.Interfaces.Authentication;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace GeneralReservationSystem.Infrastructure.Middleware
{
    public class SessionMiddleware
    {
        private readonly RequestDelegate _next;

        public SessionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ISessionRepository sessionRepository, IUserRepository userRepository)
        {
            if (context.Request.Cookies.TryGetValue(Constants.CookieNames.SessionID, out var sessionIDCookieValue)
                && Guid.TryParse(sessionIDCookieValue, out Guid sessionId))
            {
                (await sessionRepository.GetSessionByIdAsync(sessionId))
                .IfValue(async (userSession) =>
                {
                    if (userSession.ExpiresAt < DateTime.UtcNow)
                    {
                        (await sessionRepository.DeleteSessionAsync(sessionId))
                        .IfFailure((error) => throw new Exception($"Error while deleting expired session: {error}"));
                        context.Response.Cookies.Delete(Constants.CookieNames.SessionID);
                    }
                    else
                    {
                        (await userRepository.GetByGuidAsync(userSession.UserID))
                        .IfValue(async (user) =>
                        {
                            (await userRepository.GetUserRolesAsync(userSession.UserID))
                            .IfValue((roles) =>
                            {
                                // Roles can be empty, a user might not have any roles assigned
                                if (roles == null)
                                    roles = new List<ApplicationRole>();

                                var claims = new List<Claim>
                                {
                                    new Claim(ClaimTypes.Name, user.UserName),
                                    new Claim(ClaimTypes.Email, user.Email),
                                    new Claim("EmailConfirmed", user.EmailConfirmed.ToString()),
                                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString())
                                };

                                foreach (var role in roles)
                                    claims.Add(new Claim(ClaimTypes.Role, role.Name));

                                var userClaims = new ClaimsIdentity(claims, "Session");
                                var userIdentity = new ClaimsPrincipal(userClaims);

                                context.User = userIdentity;
                            })
                            .IfEmpty(() => throw new Exception($"Roles not found for user: {user.UserId}"))
                            .IfError((error) => throw new Exception($"Error while retrieving roles for user: {error}"));
                        })
                        // TODO: Log this. It's weird that a session exists without a user. It shouldn't happen.
                        .IfEmpty(() => throw new Exception($"User not found: {userSession.UserID}"))
                        .IfError((error) => throw new Exception($"Error while retrieving user: {error}"));
                    }
                }).IfEmpty(() =>
                {
                    context.Response.Cookies.Delete(Constants.CookieNames.SessionID);
                })
                .IfError((error) =>
                {
                    throw new Exception($"Error while validating the session: {error}");
                });
            }

            await _next(context);
        }
    }
}
