using GeneralReservationSystem.Application.Repositories.Interfaces.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

        public async Task InvokeAsync(HttpContext context, IServiceProvider serviceProvider)
        {
            var sessionRepository = serviceProvider.GetService<ISessionRepository>();
            var logger = serviceProvider.GetService<ILogger<SessionMiddleware>>();

            if (sessionRepository == null)
            {
                throw new Exception($"Unable to get {nameof(ISessionRepository)} service");
            }

            if (context.Request.Cookies.TryGetValue(Constants.CookieNames.SessionID, out var sessionIDCookieValue)
                && Guid.TryParse(sessionIDCookieValue, out Guid sessionId))
            {
                var sessionWithUser = await sessionRepository.GetSessionWithUserAsync(sessionId);

                if (sessionWithUser is (var session, var user))
                {
                    //TODO: Agregar roles del usuario

                    // If ExpiresAt is set and in the past, consider the session expired
                    if (session.ExpiresAt.HasValue && session.ExpiresAt.Value < DateTimeOffset.UtcNow)
                    {
                        logger?.LogDebug($"Session expired: {session.SessionId}");
                    }
                    else
                    {
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, user.UserName),
                            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString())
                        };

                        var userClaims = new ClaimsIdentity(claims, "Session");
                        var userIdentity = new ClaimsPrincipal(userClaims);

                        await context.SignInAsync(
                            Constants.AuthenticationScheme,
                            userIdentity,
                            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(1) });

                        logger?.LogDebug($"User Authenticated: {user.UserName}");
                    }
                }
                else
                {
                    logger?.LogDebug($"No session found for id {sessionId}");
                }
            }

            await _next(context);
        }
    }
}