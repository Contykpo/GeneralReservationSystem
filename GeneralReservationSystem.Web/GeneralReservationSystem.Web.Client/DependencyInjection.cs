using GeneralReservationSystem.Application;
using GeneralReservationSystem.Application.Services.Interfaces;
using GeneralReservationSystem.Application.Services.Interfaces.Authentication;
using GeneralReservationSystem.Web.Client.Authentication;
using GeneralReservationSystem.Web.Client.Helpers;
using GeneralReservationSystem.Web.Client.Services.Implementations;
using GeneralReservationSystem.Web.Client.Services.Implementations.Authentication;
using GeneralReservationSystem.Web.Client.Services.Interfaces;
using GeneralReservationSystem.Web.Client.Services.Interfaces.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor.Services;

namespace GeneralReservationSystem.Web.Client
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddClientServices(this IServiceCollection services, string apiBaseUrl)
        {
            // Register API base URL provider as singleton
            _ = services.AddSingleton<IApiBaseUrlProvider>(new ApiBaseUrlProvider(apiBaseUrl));

            // HttpClient for API calls
            // Credentials (cookies) are configured per-request in ApiServiceBase
            _ = services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

            // Register MudBlazor services
            _ = services.AddMudServices();

            // Register authentication state provider for Blazor client
            _ = services.AddOptions();
            _ = services.AddScoped<ClientAuthenticationStateProvider>();
            _ = services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<ClientAuthenticationStateProvider>());
            _ = services.AddAuthorizationCore();

            _ = services.AddScoped<IClientAuthenticationService, ClientAuthenticationService>();
            _ = services.AddScoped<IUserService, UserService>();
            _ = services.AddScoped<IStationService, ClientStationService>();
            _ = services.AddScoped<IClientStationService, ClientStationService>();
            _ = services.AddScoped<ITripService, TripService>();
            _ = services.AddScoped<IClientReservationService, ClientReservationService>();

            // Register fluent validators
            _ = services.AddFluentValidators();

            return services;
        }
    }
}
