using GeneralReservationSystem.Infrastructure.Helpers;
using GeneralReservationSystem.Web.Authentication;
using GeneralReservationSystem.Web.Client;
using GeneralReservationSystem.Web.Components;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// IMPORTANT NOTE: This CANNOT be supplied via environment variables, as Blazor WebAssembly runs in the browser. So
// it has to be supplied via appsettings.json or overridden in code here. It is baked into the client at build time.
string apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "localhost";
builder.Services.AddClientServices(apiBaseUrl);

JwtSettings jwtSettings = new()
{
    SecretKey = builder.Configuration["Jwt:SecretKey"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    Issuer = builder.Configuration["Jwt:Issuer"] ?? "GeneralReservationSystemAPI",
    Audience = builder.Configuration["Jwt:Audience"] ?? "GeneralReservationSystemClient",
    ExpirationDays = int.Parse(builder.Configuration["Jwt:ExpirationDays"] ?? "7")
};
builder.Services.AddSingleton(jwtSettings);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = JwtHelper.GetIssuerSigningKeyFromString(jwtSettings.SecretKey),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true,
        RoleClaimType = ClaimTypes.Role,
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            context.Token = context.Request.Cookies[JwtHelper.CookieName];
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            context.Response.Redirect("/login");
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            if (!context.Response.HasStarted)
            {
                context.Response.Redirect("/login");
            }
            context.HandleResponse();
            return Task.CompletedTask;
        },
        OnForbidden = context =>
        {
            if (!context.Response.HasStarted)
            {
                context.Response.Redirect("/status/403");
            }
            return Task.CompletedTask;
        }
    };
});
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ServerAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<ServerAuthenticationStateProvider>());

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    _ = app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    _ = app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(GeneralReservationSystem.Web.Client._Imports).Assembly);

app.Run();
