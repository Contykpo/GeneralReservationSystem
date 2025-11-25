using GeneralReservationSystem.Application;
using GeneralReservationSystem.Application.Services.Interfaces;
using GeneralReservationSystem.Application.Services.Interfaces.Authentication;
using GeneralReservationSystem.Web.Client.Services.Implementations;
using GeneralReservationSystem.Web.Client.Services.Implementations.Authentication;
using GeneralReservationSystem.Web.Client.Services.Interfaces;
using GeneralReservationSystem.Web.Client.Services.Interfaces.Authentication;
using MudBlazor.Services;

namespace GeneralReservationSystem.Web.Client
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddClientServices(this IServiceCollection services)
        {
            // Register MudBlazor services
            _ = services.AddMudServices();

            _ = services.AddScoped<IClientAuthenticationService, ClientAuthenticationService>();
            _ = services.AddScoped<IUserService, UserService>();
            _ = services.AddScoped<IStationService, ClientStationService>();
            _ = services.AddScoped<IClientStationService, ClientStationService>();
            _ = services.AddScoped<ITripService, TripService>();
            _ = services.AddScoped<IClientReservationService, ClientReservationService>();
            _ = services.AddScoped<IEmailService, EmailService>();

            // Register fluent validators
            _ = services.AddFluentValidators();

            return services;
        }
    }
}
