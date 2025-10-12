using GeneralReservationSystem.Web;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();

// IMPORTANT NOTE: This CANNOT be supplied via environment variables, as Blazor WebAssembly runs in the browser. So
// it has to be supplied via appsettings.json or overridden in code here. It is baked into the client at build time.
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;
builder.Services.AddClientServices(apiBaseUrl);

await builder.Build().RunAsync();
