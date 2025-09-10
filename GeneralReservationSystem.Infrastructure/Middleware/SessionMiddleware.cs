using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

using GeneralReservationSystem.Application.Entities.Authentication;
using System.Diagnostics;

namespace GeneralReservationSystem.Infrastructure.Middleware
{
	public class SessionMiddleware
	{
		private readonly DbConnectionHelper db;
		private readonly RequestDelegate next;

		public SessionMiddleware(RequestDelegate _next, DbConnectionHelper _db)
		{
			db = _db;
			next = _next;
		}

		public async Task InvokeAsync(HttpContext context)
		{
			if(context.Request.Cookies.TryGetValue(Constants.CookieNames.SessionID, out var sessionIDCookieValue)
				&& Guid.TryParse(sessionIDCookieValue, out Guid sessionId))
			{
				//TODO: Query a la base de datos para obtener la sesion, el usuario y sus roles

				//TODO: Validar sesion valida

				Debug.Assert(false);

				ApplicationUser user = new();

				List<ApplicationRole> roles = new();

				var claims = new List<Claim>
				{
					new Claim(ClaimTypes.Name, user.UserName),
					new Claim(ClaimTypes.NameIdentifier, user.Id)
				};

				foreach (var role in roles)
					claims.Add(new Claim(ClaimTypes.Role, role.Name));

				var userClaims		= new ClaimsIdentity(claims, "Session");
				var userIdentity	= new ClaimsPrincipal(userClaims);

				context.User = userIdentity;				
			}

			await next(context);
		}
	}
}
