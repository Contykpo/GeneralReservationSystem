using GeneralReservationSystem.Application.Entities.Authentication;
using GeneralReservationSystem.Application.Repositories;
using GeneralReservationSystem.Application.Repositories.Interfaces.Authentication;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication;

using System.Security.Claims;

namespace GeneralReservationSystem.Infrastructure.Middleware
{
	public class SessionMiddleware
	{
		private readonly RequestDelegate _next;

		public SessionMiddleware(RequestDelegate next)
		{
			_next = next;
		}

		public async Task InvokeAsync(HttpContext context, IServiceProvider serviceProvider)
		{
			var sessionRepository = serviceProvider.GetService<ISessionRepository>();
			var logger 			  = serviceProvider.GetService<ILogger<SessionMiddleware>>();

			if (sessionRepository == null)
			{
				throw new Exception($"Unable to get {nameof(ISessionRepository)} service");
			}

			if (context.Request.Cookies.TryGetValue(Constants.CookieNames.SessionID, out var sessionIDCookieValue)
				&& Guid.TryParse(sessionIDCookieValue, out Guid sessionId))
			{
				
				(await sessionRepository.GetSessionAsync(sessionId))
					.Match(
						onValue: async sessionWithUser =>
						{
							//TODO: Agregar roles del usuario

							var (session, user) = sessionWithUser;

							if (session.ExpiresAt < DateTimeOffset.UtcNow)
								return; //Sesion expirada, no autenticamos al usuario

							var claims = new List<Claim>
							{
								new Claim(ClaimTypes.Name, user.UserName),
								new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString())
							};

							var userClaims		= new ClaimsIdentity(claims, "Session");
							var userIdentity	= new ClaimsPrincipal(userClaims);

							await context.SignInAsync(
								Constants.AuthenticationScheme, 
								userIdentity, 
								new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(1) });

							logger.LogDebug($"User Authenticated: {user.UserName}");	
						},
						onEmpty: () => { /* No session found; proceed without setting user */ },
						onError: error => { /* Log the error if necessary */ }
					);			
			}

			await _next(context);
		}
	}
}