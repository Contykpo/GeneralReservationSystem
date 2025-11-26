using GeneralReservationSystem.Application;
using GeneralReservationSystem.Infrastructure;
using GeneralReservationSystem.Infrastructure.Helpers;
using GeneralReservationSystem.Server;
using GeneralReservationSystem.Server.Components;
using GeneralReservationSystem.Server.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddControllers();
builder.Services.AddInfrastructureRepositories();
builder.Services.AddServerServices();
builder.Services.AddFluentValidators();
builder.Services.AddHttpContextAccessor();

// Configure JWT settings
JwtSettings jwtSettings = new()
{
    SecretKey = builder.Configuration["Jwt:SecretKey"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    Issuer = builder.Configuration["Jwt:Issuer"] ?? "GeneralReservationSystemServer",
    Audience = builder.Configuration["Jwt:Audience"] ?? "GeneralReservationSystemWebClient",
    ExpirationDays = int.Parse(builder.Configuration["Jwt:ExpirationDays"] ?? "7"),
    Domain = builder.Configuration["Jwt:Domain"]
};
builder.Services.AddSingleton(jwtSettings);

// Configure JWT Bearer authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,

        IssuerSigningKey = JwtHelper.GetIssuerSigningKeyFromString(jwtSettings.SecretKey),

        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (context.Request.Cookies.TryGetValue(JwtHelper.CookieName, out string? token))
            {
                context.Token = token;
            }
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            context.HandleResponse();

            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsJsonAsync(new { error = "No está autorizado para realizar esta acción." });
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status302Found;
                context.Response.Headers.Location = "/login";
                return Task.CompletedTask;
            }
        },
        OnForbidden = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsJsonAsync(new { error = "No tiene permisos para realizar esta acción." });
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status302Found;
                context.Response.Headers.Location = "/status/403";
                return Task.CompletedTask;
            }
        }
    };
});
builder.Services.AddAuthorization();

builder.Services.AddHealthChecks();

WebApplication app = builder.Build();

app.UseHttpsRedirection();

if (!app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    _ = app.UseHsts();
}

app.MapWhen(ctx => ctx.Request.Path.StartsWithSegments("/api"), apiApp =>
{
    _ = apiApp.UseRouting();

    _ = apiApp.UseAntiforgery();

    _ = apiApp.UseAuthentication();
    _ = apiApp.UseAuthorization();

    _ = apiApp.UseEndpoints(endpoints =>
    {
        _ = endpoints.MapControllers();
        _ = endpoints.MapHealthChecks("/api/health");
    });
});

app.MapWhen(ctx => !ctx.Request.Path.StartsWithSegments("/api"), webApp =>
{
    _ = webApp.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(GlobalExceptionHandler.HandleAsync);
    });

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        webApp.UseWebAssemblyDebugging();
    }
    else
    {
        _ = webApp.UseExceptionHandler("/Error", createScopeForErrors: true);
    }

    _ = webApp.UseStaticFiles();
    _ = webApp.UseRouting();

    _ = webApp.UseAntiforgery();

    _ = webApp.UseAuthentication();
    _ = webApp.UseAuthorization();
    _ = webApp.UseEndpoints(endpoints =>
    {
        _ = endpoints.MapStaticAssets();
        _ = endpoints.MapRazorComponents<App>()
            .AddInteractiveWebAssemblyRenderMode()
            .AddAdditionalAssemblies(typeof(GeneralReservationSystem.Web.Client._Imports).Assembly);
    });
});

app.Run();

// Make Program class accessible to tests
public partial class Program { }
