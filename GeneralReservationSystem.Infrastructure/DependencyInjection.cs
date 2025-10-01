using Microsoft.Extensions.DependencyInjection;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using GeneralReservationSystem.Application.Repositories.Interfaces.Authentication;
using GeneralReservationSystem.Infrastructure.Repositories.DefaultImplementations;
using GeneralReservationSystem.Infrastructure.Repositories.DefaultImplementations.Authentication;
using GeneralReservationSystem.Application.Services.Interfaces;
using GeneralReservationSystem.Application.Services.DefaultImplementations;

namespace GeneralReservationSystem.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddSingleton<DbConnectionHelper>();

            // Register all default repository implementations
            services.AddScoped<IDestinationRepository, DefaultDestinationRepository>();
            services.AddScoped<IDriverRepository, DefaultDriverRepository>();
            services.AddScoped<IReservationRepository, DefaultReservationRepository>();
            services.AddScoped<ISeatRepository, DefaultSeatRepository>();
            services.AddScoped<ITripRepository, DefaultTripRepository>();
            services.AddScoped<IVehicleModelRepository, DefaultVehicleModelRepository>();
            services.AddScoped<IVehicleRepository, DefaultVehicleRepository>();
            services.AddScoped<IUserRepository, DefaultUserRepository>();
            services.AddScoped<IRoleRepository, DefaultRoleRepository>();
            services.AddScoped<ISessionRepository, DefaultSessionRepository>();

            // Register all default service implementations
            services.AddScoped<IDestinationService, DefaultDestinationService>();
            services.AddScoped<IDriverService, DefaultDriverService>();
            services.AddScoped<IReservationService, DefaultReservationService>();
            services.AddScoped<ISeatService, DefaultSeatService>();
            services.AddScoped<ITripService, DefaultTripService>();
            services.AddScoped<IVehicleModelService, DefaultVehicleModelService>();
            services.AddScoped<IVehicleService, DefaultVehicleService>();

            return services;
        }
    }
}
