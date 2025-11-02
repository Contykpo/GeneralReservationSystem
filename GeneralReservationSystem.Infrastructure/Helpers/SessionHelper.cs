using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace GeneralReservationSystem.Infrastructure.Helpers
{
    public class UserSessionInfo
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
    }

    public static class SessionHelper
    {
        public const string CookieName = "UserSession";

        public static void SetUserSessionCookie(HttpContext context, UserSessionInfo userSession)
        {
            CookieOptions options = new()
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            };
            string value = JsonSerializer.Serialize(userSession);
            context.Response.Cookies.Append(CookieName, value, options);
        }

        public static void ClearUserSessionCookie(HttpContext context)
        {
            context.Response.Cookies.Delete(CookieName);
        }
    }
}
