using GeneralReservationSystem.Infrastructure.Helpers;
using System.Security.Claims;
using System.Text.Json;

namespace GeneralReservationSystem.API.Middleware
{
    public class SessionMiddleware(RequestDelegate next)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Cookies.TryGetValue(SessionHelper.CookieName, out string? cookieValue))
            {
                try
                {
                    UserSessionInfo? userSession = JsonSerializer.Deserialize<UserSessionInfo>(cookieValue);
                    if (userSession != null)
                    {
                        context.Items["UserSession"] = userSession;
                        Claim[] claims =
                        [
                            new Claim(ClaimTypes.NameIdentifier, userSession.UserId.ToString()),
                            new Claim(ClaimTypes.Name, userSession.UserName),
                            new Claim(ClaimTypes.Email, userSession.Email),
                            new Claim(ClaimTypes.Role, userSession.IsAdmin ? "Admin" : "User")
                        ];
                        ClaimsIdentity identity = new(claims, "Cookie");
                        context.User = new ClaimsPrincipal(identity);
                    }
                }
                catch
                {
                    // Invalid cookie, ignore
                }
            }
            await next(context);
        }
    }
}