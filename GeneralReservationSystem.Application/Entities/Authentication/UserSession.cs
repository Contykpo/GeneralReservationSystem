using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneralReservationSystem.Application.Entities.Authentication
{
	public class UserSession
	{
		public Guid SessionId { get; set; }
		public Guid UserId { get; set; }
		public DateTimeOffset CreatedAt { get; set; }
		public DateTimeOffset? ExpiresAt { get; set; }
		public string? SessionInfo { get; set; } 
	}
}
