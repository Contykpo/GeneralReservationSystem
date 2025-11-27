using GeneralReservationSystem.Application;
using GeneralReservationSystem.Server.Services.Implementations;
using GeneralReservationSystem.Server.Services.Implementations.Authentication;
using GeneralReservationSystem.Server.Services.Interfaces;
using GeneralReservationSystem.Web.Client.Services.Interfaces;
using GeneralReservationSystem.Web.Client.Services.Interfaces.Authentication;
using MudBlazor.Services;
using MudBlazor.Translations;

namespace GeneralReservationSystem.Server
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddServerServices(this IServiceCollection services)
        {
            _ = services.AddLocalization();

            _ = services.AddMudServices();
            _ = services.AddMudTranslations();

            _ = services.AddScoped<IClientAuthenticationService, WebAuthenticationService>();
            _ = services.AddScoped<IClientUserService, WebUserService>();
            _ = services.AddScoped<IClientStationService, WebStationService>();
            _ = services.AddScoped<IClientTripService, WebTripService>();
            _ = services.AddScoped<IClientReservationService, WebReservationService>();
            _ = services.AddScoped<IClientEmailService, WebEmailService>();

            _ = services.AddScoped<IApiStationService, ApiStationService>();

            _ = services.AddFluentValidators();

            return services;
        }
    }
}
