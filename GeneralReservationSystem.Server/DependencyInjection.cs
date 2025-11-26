using GeneralReservationSystem.Application;
using GeneralReservationSystem.Server.Services.Implementations;
using GeneralReservationSystem.Server.Services.Implementations.Authentication;
using GeneralReservationSystem.Server.Services.Interfaces;
using GeneralReservationSystem.Web.Client.Services.Interfaces;
using GeneralReservationSystem.Web.Client.Services.Interfaces.Authentication;
using MudBlazor.Services;

namespace GeneralReservationSystem.Server
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddServerServices(this IServiceCollection services)
        {
            // Register MudBlazor services
            _ = services.AddMudServices();

            _ = services.AddScoped<IClientAuthenticationService, WebAuthenticationService>();
            _ = services.AddScoped<IClientUserService, WebUserService>();
            _ = services.AddScoped<IClientStationService, WebStationService>();
            _ = services.AddScoped<IClientTripService, WebTripService>();
            _ = services.AddScoped<IClientReservationService, WebReservationService>();
            _ = services.AddScoped<IClientEmailService, WebEmailService>();

            _ = services.AddScoped<IApiStationService, ApiStationService>();

            // Register fluent validators
            _ = services.AddFluentValidators();

            return services;
        }
    }
}
