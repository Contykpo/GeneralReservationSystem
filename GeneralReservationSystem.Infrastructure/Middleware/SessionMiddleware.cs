using GeneralReservationSystem.Infrastructure.Helpers;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Text.Json;

namespace GeneralReservationSystem.Infrastructure.Middleware
{
    public class SessionMiddleware(RequestDelegate next)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Cookies.TryGetValue(SessionHelper.CookieName, out var cookieValue))
            {
                try
                {
                    var userSession = JsonSerializer.Deserialize<UserSessionInfo>(cookieValue);
                    if (userSession != null)
                    {
                        context.Items["UserSession"] = userSession;
                        var claims = new[]
                        {
                            new Claim(ClaimTypes.NameIdentifier, userSession.UserId.ToString()),
                            new Claim(ClaimTypes.Name, userSession.UserName),
                            new Claim(ClaimTypes.Email, userSession.Email),
                            new Claim(ClaimTypes.Role, userSession.IsAdmin ? "Admin" : "User")
                        };
                        var identity = new ClaimsIdentity(claims, "Cookie");
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