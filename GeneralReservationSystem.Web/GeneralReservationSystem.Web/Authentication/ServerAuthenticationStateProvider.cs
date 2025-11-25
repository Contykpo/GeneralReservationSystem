using GeneralReservationSystem.Application.DTOs.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using System.Security.Claims;

namespace GeneralReservationSystem.Web.Authentication
{
    public class ServerAuthenticationStateProvider(IHttpContextAccessor httpContextAccessor, HttpClient httpClient) : AuthenticationStateProvider
    {
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me")
                .SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

            HttpContext? context = httpContextAccessor.HttpContext;

            if (context == null)
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            foreach (KeyValuePair<string, string> cookie in context.Request.Cookies)
            {
                request.Headers.Add("Cookie", $"{cookie.Key}={cookie.Value}");
            }

            try
            {
                HttpResponseMessage response = await httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                UserInfo? currentUser = await response.Content.ReadFromJsonAsync<UserInfo>();
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
            catch(Exception ex)
            {
                Console.WriteLine($"Error retrieving authentication state from API: {ex}");

                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
        }
    }
}
