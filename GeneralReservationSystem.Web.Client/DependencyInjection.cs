using GeneralReservationSystem.Application;
using GeneralReservationSystem.Web.Client.Services.Implementations;
using GeneralReservationSystem.Web.Client.Services.Implementations.Authentication;
using GeneralReservationSystem.Web.Client.Services.Interfaces;
using GeneralReservationSystem.Web.Client.Services.Interfaces.Authentication;
using MudBlazor.Services;
using MudBlazor.Translations;

namespace GeneralReservationSystem.Web.Client
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddClientServices(this IServiceCollection services)
        {
            _ = services.AddLocalization();

            _ = services.AddMudServices();
            _ = services.AddMudTranslations();

            _ = services.AddScoped<IClientAuthenticationService, ClientAuthenticationService>();
            _ = services.AddScoped<IClientUserService, ClientUserService>();
            _ = services.AddScoped<IClientStationService, ClientStationService>();
            _ = services.AddScoped<IClientTripService, ClientTripService>();
            _ = services.AddScoped<IClientReservationService, ClientReservationService>();
            _ = services.AddScoped<IClientEmailService, ClientEmailService>();

            _ = services.AddFluentValidators();

            return services;
        }
    }
}
