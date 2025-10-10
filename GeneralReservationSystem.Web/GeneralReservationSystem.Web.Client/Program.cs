using GeneralReservationSystem.Web.Client;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddMudServices();

// IMPORTANT NOTE: This CANNOT be supplied via environment variables, as Blazor WebAssembly runs in the browser. So
// it has to be supplied via appsettings.json or overridden in code here. It is baked into the client at build time.
var apiBaseUrl = builder.Configuration["API_BASE_URL"] ?? "https://localhost:5003";
builder.Services.AddClientServices(apiBaseUrl);

await builder.Build().RunAsync();
