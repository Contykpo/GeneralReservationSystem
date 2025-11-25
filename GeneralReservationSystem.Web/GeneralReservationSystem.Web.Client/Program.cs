using GeneralReservationSystem.Web.Client.Authentication;
using GeneralReservationSystem.Web.Client.Helpers;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Net.Http.Json;

namespace GeneralReservationSystem.Web.Client
{
    internal sealed record class ConfigData(string ApiBaseUrl);

    internal class Program
    {
        public static async Task Main(string[] args)
        {
            WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<HeadOutlet>("head::after");

            using HttpClient serverHttp = new() { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
            ConfigData config = await serverHttp.GetFromJsonAsync<ConfigData>("config.json") ?? new ConfigData(builder.HostEnvironment.BaseAddress);

            string apiBaseUrl = config.ApiBaseUrl;

            // Register API base URL provider as singleton
            _ = builder.Services.AddSingleton<IApiBaseUrlProvider>(new ApiBaseUrlProvider(apiBaseUrl));

            // HttpClient for API calls
            // Credentials (cookies) are configured per-request in ApiServiceBase
            _ = builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

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
