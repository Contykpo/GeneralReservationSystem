using GeneralReservationSystem.Application.DTOs.Authentication;
using GeneralReservationSystem.Web.Client.Services.Interfaces.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace GeneralReservationSystem.Web.Client.Authentication
{
    public class CustomAuthenticationStateProvider(IClientAuthenticationService clientAuthenticationService) : AuthenticationStateProvider
    {
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                UserInfo currentUser = await clientAuthenticationService.GetCurrentUserAsync();
                if (currentUser == null)
                {
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                List<Claim> claims =
                [
                    new(ClaimTypes.NameIdentifier, currentUser.UserId.ToString()),
                    new(ClaimTypes.Name, currentUser.UserName ?? string.Empty),
                    new(ClaimTypes.Email, currentUser.Email ?? string.Empty)
                ];

                if (currentUser.IsAdmin)
                {
                    claims.Add(new Claim(ClaimTypes.Role, "Admin"));
                }

                ClaimsIdentity identity = new(claims, "cookie");
                ClaimsPrincipal principal = new(identity);
                return new AuthenticationState(principal);
            }
            catch
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
        }

        // Called after successful login/register to update the UI
        public void MarkUserAsAuthenticated(UserInfo userInfo)
        {
            List<Claim> claims =
            [
                new(ClaimTypes.NameIdentifier, userInfo.UserId.ToString()),
                new(ClaimTypes.Name, userInfo.UserName ?? string.Empty),
                new(ClaimTypes.Email, userInfo.Email ?? string.Empty)
            ];
            if (userInfo.IsAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }

            ClaimsIdentity identity = new(claims, "cookie");
            ClaimsPrincipal principal = new(identity);
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(principal)));
        }

        public void MarkUserAsLoggedOut()
        {
            ClaimsPrincipal anonymous = new(new ClaimsIdentity());
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(anonymous)));
        }
    }
}
