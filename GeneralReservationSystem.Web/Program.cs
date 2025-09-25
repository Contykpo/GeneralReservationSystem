using GeneralReservationSystem.Web.Components;
using GeneralReservationSystem.Web.Components.Account;
using GeneralReservationSystem.Web.Data;
using GeneralReservationSystem.Infrastructure;
using GeneralReservationSystem.Infrastructure.Middleware;
using GeneralReservationSystem.Infrastructure.Repositories.DefaultImplementations;
using GeneralReservationSystem.Infrastructure.Repositories.Interfaces;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;

using MudBlazor.Services;

using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddMudServices();

//builder.Services.AddCascadingAuthenticationState();
//builder.Services.AddScoped<IdentityUserAccessor>();
//builder.Services.AddScoped<IdentityRedirectManager>();
//builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

//builder.Services.AddAuthentication(options =>
//    {
//        options.DefaultScheme = IdentityConstants.ApplicationScheme;
//        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
//    })
//    .AddIdentityCookies();

builder.Services.AddSingleton<DbConnectionHelper>();
builder.Services.AddScoped<IUserRepository, DefaultUserRepository>();
builder.Services.AddScoped<ISessionRepository, DefaultSessionRepository>();

builder.Services.AddAuthentication(Constants.AuthenticationScheme)
    .AddCookie(Constants.AuthenticationScheme, options =>
    {
        options.LoginPath           = "/";
        options.LogoutPath          = "/";
        options.ExpireTimeSpan      = TimeSpan.FromHours(1);
        options.Cookie.SameSite     = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
        options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
        options.Cookie.Name         = Constants.CookieNames.SessionID;
	});

builder.Services.AddAuthorization();

//builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
//    .AddEntityFrameworkStores<ApplicationDbContext>()
//    .AddSignInManager()
//    .AddDefaultTokenProviders();

//builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.WebHost.UseKestrel(o =>
        o.ListenAnyIP(5000)
    );

var app = builder.Build();

app.UseMiddleware<SessionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
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
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.MapGet("/teraLogin", async (httpContext) =>
{
    var sessionId = httpContext.Request.Query["sessionId"];
    
    if (Guid.TryParse(sessionId, out var sessionGuid))
    {
        httpContext.Response.Cookies.Append(
            Constants.CookieNames.SessionID,
            sessionGuid.ToString(),
            new CookieOptions
            {
                HttpOnly    = true,
                Secure      = true,
                SameSite    = SameSiteMode.Strict,
                Expires     = DateTimeOffset.UtcNow.AddHours(1)
            });

        httpContext.Response.Redirect("/");
	}
    else
    {
        httpContext.Response.StatusCode = 400;
	}
});

app.Run();