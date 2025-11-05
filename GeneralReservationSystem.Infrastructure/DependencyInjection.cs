using GeneralReservationSystem.Application.Repositories.Interfaces;
using GeneralReservationSystem.Application.Repositories.Interfaces.Authentication;
using GeneralReservationSystem.Application.Services.DefaultImplementations;
using GeneralReservationSystem.Application.Services.DefaultImplementations.Authentication;
using GeneralReservationSystem.Application.Services.Interfaces;
using GeneralReservationSystem.Application.Services.Interfaces.Authentication;
using GeneralReservationSystem.Infrastructure.Repositories.Sql;
using GeneralReservationSystem.Infrastructure.Repositories.Sql.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace GeneralReservationSystem.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureRepositories(this IServiceCollection services)
        {
            // Register default DbConnection factory
            _ = services.AddScoped(sp =>
                DbConnectionFactory.CreateFactory<NpgsqlConnection>(
                    sp.GetRequiredService<IConfiguration>(),
                    "DefaultConnection"));

            // Register all default repository implementations
            _ = services.AddScoped<IUserRepository, UserRepository>();
            _ = services.AddScoped<IStationRepository, StationRepository>();
            _ = services.AddScoped<IReservationRepository, ReservationRepository>();
            _ = services.AddScoped<ITripRepository, TripRepository>();

            // Register all default service implementations
            _ = services.AddScoped<IAuthenticationService, AuthenticationService>();
            _ = services.AddScoped<IUserService, UserService>();
            _ = services.AddScoped<IStationService, StationService>();
            _ = services.AddScoped<IReservationService, ReservationService>();
            _ = services.AddScoped<ITripService, TripService>();

            return services;
        }
    }
}
