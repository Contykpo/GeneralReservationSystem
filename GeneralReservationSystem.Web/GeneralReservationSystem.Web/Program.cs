using GeneralReservationSystem.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

builder.WebHost.UseKestrel(k =>
{
	var httpPort    = int.Parse(Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS") ?? "8080");
	var httpsPort   = int.Parse(Environment.GetEnvironmentVariable("ASPNETCORE_HTTPS_PORTS") ?? "8081");

	k.ListenAnyIP(httpPort);
    k.ListenAnyIP(httpsPort, c => c.UseHttps());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
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
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(GeneralReservationSystem.Web.Client._Imports).Assembly);

app.Run();
