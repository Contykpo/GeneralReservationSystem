using GeneralReservationSystem.Infrastructure.Helpers;
using GeneralReservationSystem.Web.Authentication;
using GeneralReservationSystem.Web.Client;
using GeneralReservationSystem.Web.Client.Authentication;
using GeneralReservationSystem.Web.Client.Helpers;
using GeneralReservationSystem.Web.Components;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

string clientApiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "localhost";

string serverApiBaseUrl = builder.Configuration["ApiBaseUrlServer"] ?? clientApiBaseUrl;

Console.WriteLine($"Configured API Base URL for client: {clientApiBaseUrl}");
Console.WriteLine($"Configured API Base URL for server: {serverApiBaseUrl}");

// Register API base URL provider as singleton
_ = builder.Services.AddSingleton<IApiBaseUrlProvider>(new ApiBaseUrlProvider(serverApiBaseUrl));

// HttpClient for API calls
// Credentials (cookies) are configured per-request in ApiServiceBase
_ = builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(serverApiBaseUrl) });

builder.Services.AddClientServices();

builder.Services.AddHttpContextAccessor();
builder.Services.AddOptions();
builder.Services.AddScoped<ServerAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<ServerAuthenticationStateProvider>());

// Testing API URL connection
try
{
    using HttpClient testClient = new() { BaseAddress = new Uri(serverApiBaseUrl) };
    testClient.Timeout = TimeSpan.FromSeconds(5);
    HttpResponseMessage response = await testClient.GetAsync("/health");
    if (response.IsSuccessStatusCode)
    {
        Console.WriteLine($"Successfully connected to API at {serverApiBaseUrl}");
    }
    else
    {
        Console.WriteLine($"API responded with status code: {response.StatusCode} at {serverApiBaseUrl}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Failed to connect to API at {serverApiBaseUrl}: {ex.Message}");
}

builder.Services.AddAuthentication(ApiAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, ApiAuthenticationHandler>(
        ApiAuthenticationHandler.SchemeName, 
        options => { });

builder.Services.AddAuthorization();

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

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

// Map the config endpoint to provide the API base URL to the Blazor WebAssembly client. Seems hacky, but it works.
app.MapGet("/config.json", () => new { ApiBaseUrl = clientApiBaseUrl });

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(GeneralReservationSystem.Web.Client._Imports).Assembly);

app.Run();
