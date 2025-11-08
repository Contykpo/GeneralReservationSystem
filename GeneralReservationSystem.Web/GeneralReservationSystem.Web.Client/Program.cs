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
            _ = builder.Services.AddClientServices(config.ApiBaseUrl);

            await builder.Build().RunAsync();
        }
    }
}
