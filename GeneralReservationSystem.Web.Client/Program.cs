using GeneralReservationSystem.Web.Client.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace GeneralReservationSystem.Web.Client
{
    internal sealed record class ConfigData(string ApiBaseUrl);

    internal class Program
    {
        public static async Task Main(string[] args)
        {
            WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<HeadOutlet>("head::after");

            // HttpClient for API calls
            // Credentials (cookies) are configured per-request in ClientServiceBase
            _ = builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

            _ = builder.Services.AddClientServices();

            // Register authentication state provider for Blazor client
            _ = builder.Services.AddOptions();
            _ = builder.Services.AddScoped<ClientAuthenticationStateProvider>();
            _ = builder.Services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<ClientAuthenticationStateProvider>());
            _ = builder.Services.AddAuthorizationCore();

            await builder.Build().RunAsync();
        }
    }
}
