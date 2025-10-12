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
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Data.Common;

namespace GeneralReservationSystem.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureRepositories(this IServiceCollection services)
        {
            // Register default DbConnection factory
            services.AddScoped<Func<DbConnection>>(sp =>
                DbConnectionFactory.CreateFactory<SqlConnection>(
                    sp.GetRequiredService<IConfiguration>(),
                    "DefaultConnection"));

            // Register all default repository implementations
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IStationRepository, StationRepository>();
            services.AddScoped<IReservationRepository, ReservationRepository>();
            services.AddScoped<ITripRepository, TripRepository>();

            // Register all default service implementations
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IStationService, StationService>();
            services.AddScoped<IReservationService, ReservationService>();
            services.AddScoped<ITripService, TripService>();

            // Register UnitOfWork
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }
    }
}
