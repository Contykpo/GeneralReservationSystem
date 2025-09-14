using GeneralReservationSystem.Application.Repositories.Interfaces;
using GeneralReservationSystem.Application.Repositories.Interfaces.Authentication;
using GeneralReservationSystem.Infrastructure.Repositories.DefaultImplementations;
using GeneralReservationSystem.Infrastructure.Repositories.DefaultImplementations.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GeneralReservationSystem.Infrastructure
{
    public static class DependencyInjection
    {
        public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // TODO: Implement infrastructure services registration

            //builder.Services.AddCascadingAuthenticationState();
            //builder.Services.AddScoped<IdentityUserAccessor>();
            //builder.Services.AddScoped<IdentityRedirectManager>();
            //builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

            //builder.Services.AddAuthentication(options =>
            //    {
            //        options.DefaultScheme = IdentityConstants.ApplicationScheme;
            //        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            //    })
            //    .AddIdentityCookies();

            //builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
            //    .AddEntityFrameworkStores<ApplicationDbContext>()
            //    .AddSignInManager()
            //    .AddDefaultTokenProviders();

            //builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

            services.AddSingleton<DbConnectionHelper>();

            services.AddScoped<IUserRepository, DefaultUserRepository>();
            services.AddScoped<IRoleRepository, DefaultRoleRepository>();
            services.AddScoped<ISessionRepository, DefaultSessionRepository>();
        }
    }
}