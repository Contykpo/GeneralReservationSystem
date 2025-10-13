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
            => new(Encoding.UTF8.GetBytes(secretKey));

        public static string GenerateJwtToken(UserSessionInfo userSession, JwtSettings settings)
        {
            var key = GetIssuerSigningKeyFromString(settings.SecretKey);
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userSession.UserId.ToString()),
                new Claim(ClaimTypes.Name, userSession.UserName),
                new Claim(ClaimTypes.Email, userSession.Email),
                new Claim(ClaimTypes.Role, userSession.IsAdmin ? "Admin" : "User"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iss, settings.Issuer),
                new Claim(JwtRegisteredClaimNames.Aud, settings.Audience)
            };

            var token = new JwtSecurityToken(
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
            var options = new CookieOptions
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
            var token = GenerateJwtToken(userSession, settings);
            SetJwtCookie(context, token, settings);

            return token;
        }

        public static void ClearJwtCookie(HttpContext context)
        {
            context.Response.Cookies.Delete(CookieName);
        }
    }
}
