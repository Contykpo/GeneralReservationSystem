using GeneralReservationSystem.Infrastructure;
using GeneralReservationSystem.Infrastructure.Middleware;
using GeneralReservationSystem.Web.Components;
using MudBlazor;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
});

// Register all default repositories via DI extension
builder.Services.AddInfrastructureRepositories();

builder.Services.AddAuthentication(Constants.AuthenticationScheme)
    .AddCookie(Constants.AuthenticationScheme, options =>
    {
        options.LoginPath = "/";
        options.LogoutPath = "/";
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.Name = Constants.CookieNames.SessionID;
    });

builder.Services.AddAuthorization();

// Explicitly configure antiforgery cookie options to match authentication cookie
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddHttpContextAccessor();

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

app.UseMiddleware<SessionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.MapGet("/teraLogin", httpContext =>
{
    var sessionId = httpContext.Request.Query["sessionId"];

    if (Guid.TryParse(sessionId, out var sessionGuid))
    {
        httpContext.Response.Cookies.Append(
            Constants.CookieNames.SessionID,
            sessionGuid.ToString(),
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddHours(1)
            });

        httpContext.Response.Redirect("/");
    }
    else
    {
        httpContext.Response.StatusCode = 400;
    }

    return Task.CompletedTask;
});

app.Run();