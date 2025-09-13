using GeneralReservationSystem.Application.Repositories.Interfaces;
using GeneralReservationSystem.Application.Repositories.Interfaces.Authentication;
using GeneralReservationSystem.Infrastructure;
using GeneralReservationSystem.Infrastructure.Middleware;
using GeneralReservationSystem.Infrastructure.Repositories.DefaultImplementations;
using GeneralReservationSystem.Infrastructure.Repositories.DefaultImplementations.Authentication;
using GeneralReservationSystem.Web.Components;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddMudServices();

builder.Services.AddInfrastructure(builder.Configuration);

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
