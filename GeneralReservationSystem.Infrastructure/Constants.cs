using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneralReservationSystem.Infrastructure
{
	public static class Constants
	{
		public static class CookieNames
		{
			public static readonly string SessionIDCookieName = "grs_session_id";
		} 
		
		public static class RoleNames
		{
			public static readonly string User = "customer";
		}
	}
}
