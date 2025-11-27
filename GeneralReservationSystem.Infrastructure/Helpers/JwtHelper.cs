using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GeneralReservationSystem.Infrastructure.Helpers
{
    public class JwtSettings
    {
        public string SecretKey { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int ExpirationDays { get; set; } = 7;
    }

    public static class JwtHelper
    {
        public const string SessionCookieName = "jwt_token";
        public const string SessionCookiePath = "/";

        public static SymmetricSecurityKey GetIssuerSigningKeyFromString(string secretKey)
        {
            return new(Encoding.UTF8.GetBytes(secretKey));
        }

        public static string GenerateSessionJwtToken(this UserSessionInfo userSession, JwtSettings settings)
        {
            SymmetricSecurityKey key = GetIssuerSigningKeyFromString(settings.SecretKey);
            SigningCredentials credentials = new(key, SecurityAlgorithms.HmacSha256);

            Claim[] claims =
            [
                new Claim(ClaimTypes.NameIdentifier, userSession.UserId.ToString()),
                new Claim(ClaimTypes.Name, userSession.UserName),
                new Claim(ClaimTypes.Email, userSession.Email),
                new Claim(ClaimTypes.Role, userSession.IsAdmin ? "Admin" : "User"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iss, settings.Issuer),
                new Claim(JwtRegisteredClaimNames.Aud, settings.Audience)
            ];

            JwtSecurityToken token = new(
                issuer: settings.Issuer,
                audience: settings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(settings.ExpirationDays),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public static string GenerateAndSetSessionJwtCookie(this HttpContext context, UserSessionInfo userSession, JwtSettings jwtSettings)
        {
            CookieOptions options = new()
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Path = SessionCookiePath,
                Expires = DateTimeOffset.UtcNow.AddDays(jwtSettings.ExpirationDays)
            };

            string token = GenerateSessionJwtToken(userSession, jwtSettings);
            context.Response.Cookies.Append(SessionCookieName, token, options);

            return token;
        }

        public static void ClearSessionJwtCookie(this HttpResponse response)
        {
            response.Cookies.Delete(SessionCookieName);
        }

        public static void ClearSessionJwtCookie(this HttpContext context)
        {
            context.Response.ClearSessionJwtCookie();
        }
    }
}
