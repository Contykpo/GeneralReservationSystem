using GeneralReservationSystem.Application.Services.Interfaces;
using GeneralReservationSystem.Application.Services.Interfaces.Authentication;
using GeneralReservationSystem.Web.Client.Services.Implementations;
using GeneralReservationSystem.Web.Client.Services.Implementations.Authentication;
using GeneralReservationSystem.Web.Services.Authentication;
using Microsoft.AspNetCore.Components.Authorization;

#if DEBUG && USE_MOCK_SERVICES
using GeneralReservationSystem.MockServices;
#endif

namespace GeneralReservationSystem.Web.Client
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
            services.AddAuthorizationCore();
            services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

#if DEBUG && USE_MOCK_SERVICES

			services.AddScoped<IUserService, UserService>();
			services.AddScoped<IStationService, StationService>();
			services.AddScoped<ITripService>(_ => MockTripService.GetService());
			services.AddScoped<IReservationService, ReservationService>();

#else
			services.AddScoped<IUserService, UserService>();
            services.AddScoped<IStationService, StationService>();
            services.AddScoped<ITripService, TripService>();
            services.AddScoped<IReservationService, ReservationService>();
#endif

			return services;
        }
    }
}
