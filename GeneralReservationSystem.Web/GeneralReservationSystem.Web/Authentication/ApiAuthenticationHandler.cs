using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace GeneralReservationSystem.Web.Authentication
{
    public class ApiAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ServerAuthenticationStateProvider authStateProvider) 
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "ApiAuthentication";

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            try
            {
                AuthenticationState authState = await authStateProvider.GetAuthenticationStateAsync();
                ClaimsPrincipal user = authState.User;

                if (user?.Identity?.IsAuthenticated == true)
                {
                    AuthenticationTicket ticket = new(user, SchemeName);
                    return AuthenticateResult.Success(ticket);
                }

                return AuthenticateResult.NoResult();
            }
            catch
            {
                return AuthenticateResult.Fail("Authentication failed");
            }
        }
    }
}
