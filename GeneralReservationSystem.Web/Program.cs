using GeneralReservationSystem.Web.Components;
using GeneralReservationSystem.Web.Components.Account;
using GeneralReservationSystem.Web.Data;

using GeneralReservationSystem.Infrastructure;
using GeneralReservationSystem.Infrastructure.Middleware;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;

using MudBlazor.Services;

using GeneralReservationSystem.Infrastructure.Repositories.DefaultImplementations;
using GeneralReservationSystem.Infrastructure.Repositories.Interfaces;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddMudServices();

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

builder.Services.AddSingleton<DbConnectionHelper>();
builder.Services.AddScoped<IUserRepository, DefaultUserRepository>();

//builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
//    .AddEntityFrameworkStores<ApplicationDbContext>()
//    .AddSignInManager()
//    .AddDefaultTokenProviders();

//builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.WebHost.UseKestrel(options =>
{
    var httpPort = Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS") ?? "8080";
    var httpsPort = Environment.GetEnvironmentVariable("ASPNETCORE_HTTPS_PORTS") ?? "8081";

    options.ListenAnyIP(int.Parse(httpPort)); // HTTP
    options.ListenAnyIP(int.Parse(httpsPort), listenOptions =>
    {
        listenOptions.UseHttps(); // HTTPS
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    // TODO: Se debería configurar un certificado SSL válido en producción. También sería útil redirigir HTTP a HTTPS en desarrollo.
    // Se puede crear un certificado autofirmado para pruebas (y demos de producción).
    app.UseHttpsRedirection();

    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.UseMiddleware<SessionMiddleware>();

app.Run();
