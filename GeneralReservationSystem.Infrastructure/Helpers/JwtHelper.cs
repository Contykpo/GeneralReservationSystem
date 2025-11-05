using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GeneralReservationSystem.Infrastructure.Helpers
{
    public class JwtSettings
    {
        //No es la mejor idea tener la secret key en plano en un string, pero por simplicidad dejomoslo asi por ahora
        public string SecretKey { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int ExpirationDays { get; set; } = 7;
    }

    public static class JwtHelper
    {
        public const string CookieName = "jwt_token";

        public static SymmetricSecurityKey GetIssuerSigningKeyFromString(string secretKey)
        {
            return new(Encoding.UTF8.GetBytes(secretKey));
        }

        public static string GenerateJwtToken(UserSessionInfo userSession, JwtSettings settings)
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

        public static void SetJwtCookie(HttpContext context, string token, JwtSettings settings)
        {
            CookieOptions options = new()
            {
                HttpOnly = true,
                Secure = true,

                SameSite = SameSiteMode.None,

                Expires = DateTimeOffset.UtcNow.AddDays(settings.ExpirationDays)
            };

            context.Response.Cookies.Append(CookieName, token, options);
        }

        public static string GenerateAndSetJwtCookie(HttpContext context, UserSessionInfo userSession, JwtSettings settings)
        {
            string token = GenerateJwtToken(userSession, settings);
            SetJwtCookie(context, token, settings);

            return token;
        }

        public static void ClearJwtCookie(HttpContext context)
        {
            context.Response.Cookies.Delete(CookieName);
        }
    }
}
