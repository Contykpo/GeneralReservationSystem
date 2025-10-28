using GeneralReservationSystem.Web.Client;
using GeneralReservationSystem.Web.Components;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// IMPORTANT NOTE: This CANNOT be supplied via environment variables, as Blazor WebAssembly runs in the browser. So
// it has to be supplied via appsettings.json or overridden in code here. It is baked into the client at build time.
string apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "localhost";
builder.Services.AddClientServices(apiBaseUrl);

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


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(GeneralReservationSystem.Web.Client._Imports).Assembly);

app.Run();
