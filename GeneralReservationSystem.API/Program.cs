using GeneralReservationSystem.API.Middleware;
using GeneralReservationSystem.API.Services.Implementations;
using GeneralReservationSystem.API.Services.Interfaces;
using GeneralReservationSystem.Application;
using GeneralReservationSystem.Application.Services.Interfaces;
using GeneralReservationSystem.Infrastructure;
using GeneralReservationSystem.Infrastructure.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddInfrastructureRepositories();
builder.Services.AddFluentValidators();
builder.Services.AddHttpContextAccessor();

// Override IStationService with API-specific implementation
builder.Services.AddScoped<IStationService, ApiStationService>();
builder.Services.AddScoped<IApiStationService, ApiStationService>();

// Configure JWT settings
JwtSettings jwtSettings = new()
{
    SecretKey = builder.Configuration["Jwt:SecretKey"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    Issuer = builder.Configuration["Jwt:Issuer"] ?? "GeneralReservationSystemAPI",
    Audience = builder.Configuration["Jwt:Audience"] ?? "GeneralReservationSystemClient",

    ExpirationDays = int.Parse(builder.Configuration["Jwt:ExpirationDays"] ?? "7"),
    Domain = builder.Configuration["Jwt:Domain"]
};
builder.Services.AddSingleton(jwtSettings);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        // Read allowed origins from environment variable (comma-separated)
        string[] allowedOrigins = builder.Configuration["CorsOrigins"]?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            ?? []; // No origins allowed

        _ = policy.WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .WithExposedHeaders("Content-Disposition");
    });
});

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

    // Read JWT token from cookie instead of Authorization header
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
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsJsonAsync(new { error = "No está autorizado para realizar esta acción." });
        },
        OnForbidden = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsJsonAsync(new { error = "No tiene permisos para realizar esta acción." });
        }
    };
});

builder.Services.AddAuthorization();
builder.Services.AddHealthChecks();

WebApplication app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(GlobalExceptionHandler.HandleAsync);
});

if (!app.Environment.IsDevelopment())
{
    // If in development, web and api containers can communicate via http, but in production they MUST use https
    _ = app.UseHttpsRedirection();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.MapOpenApi();
app.Run();

// Make Program class accessible to tests
public partial class Program { }
