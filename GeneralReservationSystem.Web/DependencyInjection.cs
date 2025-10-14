using GeneralReservationSystem.Application.Services.Interfaces;
using GeneralReservationSystem.Application.Services.Interfaces.Authentication;
using GeneralReservationSystem.Web.Authentication;
using GeneralReservationSystem.Web.Services.Implementations;
using GeneralReservationSystem.Web.Services.Implementations.Authentication;
using GeneralReservationSystem.Web.Services.Interfaces;
using GeneralReservationSystem.Web.Services.Interfaces.Authentication;
using Microsoft.AspNetCore.Components.Authorization;

namespace GeneralReservationSystem.Web
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddClientServices(this IServiceCollection services, string apiBaseUrl)
        {
            // HttpClient for API calls
            // Credentials (cookies) are configured per-request in ApiServiceBase
            _ = services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

            // Register authentication state provider for Blazor client
            _ = services.AddOptions();
            _ = services.AddScoped<CustomAuthenticationStateProvider>();
            _ = services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<CustomAuthenticationStateProvider>());
            _ = services.AddAuthorizationCore();

            _ = services.AddScoped<IClientAuthenticationService, ClientAuthenticationService>();
            _ = services.AddScoped<IUserService, UserService>();
            _ = services.AddScoped<IStationService, StationService>();
            _ = services.AddScoped<ITripService, TripService>();
            _ = services.AddScoped<IClientReservationService, ClientReservationService>();

            return services;
        }
    }
}
