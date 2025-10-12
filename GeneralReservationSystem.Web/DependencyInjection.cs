using GeneralReservationSystem.Application.Services.Interfaces;
using GeneralReservationSystem.Application.Services.Interfaces.Authentication;
using GeneralReservationSystem.Web.Authentication;
using GeneralReservationSystem.Web.Services.Implementations;
using GeneralReservationSystem.Web.Services.Implementations.Authentication;
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
            services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

            // Register authentication state provider for Blazor client
            services.AddOptions();
            services.AddScoped<CustomAuthenticationStateProvider>();
            services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<CustomAuthenticationStateProvider>());
            services.AddAuthorizationCore();

            services.AddScoped<IClientAuthenticationService, ClientAuthenticationService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IStationService, StationService>();
            services.AddScoped<ITripService, TripService>();
            services.AddScoped<IReservationService, ReservationService>();

            return services;
        }
    }
}
