using GeneralReservationSystem.Application.Repositories.Interfaces;
using GeneralReservationSystem.Application.Repositories.Interfaces.Authentication;
using GeneralReservationSystem.Application.Repositories.Util.Interfaces;
using GeneralReservationSystem.Application.Services.DefaultImplementations;
using GeneralReservationSystem.Application.Services.DefaultImplementations.Authentication;
using GeneralReservationSystem.Application.Services.Interfaces;
using GeneralReservationSystem.Application.Services.Interfaces.Authentication;
using GeneralReservationSystem.Infrastructure.Repositories.Sql;
using GeneralReservationSystem.Infrastructure.Repositories.Sql.Authentication;
using GeneralReservationSystem.Infrastructure.Repositories.Util.Sql;
using GeneralReservationSystem.Infrastructure.Repositories.Util.Sql.Query;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GeneralReservationSystem.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureRepositories(this IServiceCollection services)
        {
            // Register default DbConnection factory
            _ = services.AddScoped(sp =>
                DbConnectionFactory.CreateFactory<SqlConnection>(
                    sp.GetRequiredService<IConfiguration>(),
                    "DefaultConnection"));

            // Register default query provider
            _ = services.AddScoped<RepositoryQueryProvider, SqlQueryProvider>();

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

            // Register UnitOfWork
            _ = services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }
    }
}
