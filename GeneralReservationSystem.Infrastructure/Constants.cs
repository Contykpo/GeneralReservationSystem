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
			public const string SessionID = "grs_session_id";
		} 
		
		public static class RoleNames
		{
			public const string User = "customer";
		}

		public static class Tables
		{
			public static class ApplicationUser
			{
				public const string TableName					= "ApplicationUser";
				public const string UserIdColumnName			= "Id";
				public const string NameColumnName				= "UserName";
				public const string NormalizedNameColumnName	= "NormalizedUserName";
				public const string EmailColumnName				= "Email";
				public const string NormalizedEmailColumnName	= "NormalizedEmail";
				public const string EmailConfirmedColumnName	= "EmailConfirmed";
				public const string PasswordHashColumnName		= "PasswordHash";
				public const string PasswordSaltColumnName		= "PasswordSalt";
				public const string SecurityStampColumnName		= "SecurityStamp";
			}

			public static class ApplicationRole
			{
				public const string TableName					= "ApplicationRole";
				public const string NameColumnName				= "ApplicationRole";
				public const string NormalizedNameColumnName	= "ApplicationRole";
				public const string RoleIdColumnName			= "Id";
			}

			public static class UserSession
			{
				public const string TableName				= "UserSession";
				public const string IdColumnName			= "Id";
				public const string UserIdColumnName		= "UserId";
				public const string CreatedAtColumnName		= "CreatedAt";
				public const string ExpiresAtColumnName		= "ExpiresAt";
				public const string SessionInfoColumnName	= "SessionInfo";
			}

			public static class UserRole
			{
				public const string TableName			= "UserRole";
				public const string UserIdColumnName	= "UserId";
				public const string RoleIdColumnName	= "RoleId";
			}
		}
	}
}
