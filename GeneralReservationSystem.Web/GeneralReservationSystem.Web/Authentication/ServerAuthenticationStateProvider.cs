using GeneralReservationSystem.Infrastructure.Helpers;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace GeneralReservationSystem.Web.Authentication
{
    public class ServerAuthenticationStateProvider(IHttpContextAccessor httpContextAccessor, JwtSettings jwtSettings) : AuthenticationStateProvider
    {
        private readonly JwtSettings jwtSettings = jwtSettings;

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            HttpContext? context = httpContextAccessor.HttpContext;
            if (context == null)
            {
                return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
            }

            if (!context.Request.Cookies.TryGetValue(JwtHelper.CookieName, out string? token) || string.IsNullOrEmpty(token))
            {
                return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
            }

            JwtSecurityTokenHandler handler = new();
            try
            {
                ClaimsPrincipal principal = handler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = JwtHelper.GetIssuerSigningKeyFromString(jwtSettings.SecretKey),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateLifetime = true,
                    RoleClaimType = ClaimTypes.Role,
                    ClockSkew = TimeSpan.Zero
                }, out _);
                return Task.FromResult(new AuthenticationState(principal));
            }
            catch
            {
                return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
            }
        }
    }
}
