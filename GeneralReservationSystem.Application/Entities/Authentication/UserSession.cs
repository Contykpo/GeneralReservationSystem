using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneralReservationSystem.Application.Entities.Authentication
{
	public class UserSession
	{
		public Guid SessionId { get; set; } = Guid.NewGuid();

		/// <summary>
		/// ID del usuario al que pertenece esta sesion
		/// </summary>
		public Guid UserId { get; set; }

		/// <summary>
		/// Fecha en la que se creo esta sesion
		/// </summary>
		public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

		/// <summary>
		/// Fecha opcional en la que expira esta sesion
		/// </summary>
		public DateTimeOffset? ExpiresAt { get; set; }

		/// <summary>
		/// Informacion opcional sobre la sesion
		/// </summary>
		public string? SessionInfo { get; set; } 
	}
}
