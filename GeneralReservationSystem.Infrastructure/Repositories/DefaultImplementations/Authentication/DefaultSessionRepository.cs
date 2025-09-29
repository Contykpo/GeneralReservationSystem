using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;

using GeneralReservationSystem.Application.Entities.Authentication;
using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.Repositories.Interfaces.Authentication;

using static GeneralReservationSystem.Infrastructure.Constants.Tables.UserSession;
using static GeneralReservationSystem.Application.Common.OperationResult;
using static GeneralReservationSystem.Application.Common.OptionalResult<object>;


namespace GeneralReservationSystem.Infrastructure.Repositories.DefaultImplementations.Authentication
{
	public class DefaultSessionRepository : ISessionRepository
	{
		private readonly DbConnectionHelper _dbConnection;
		private readonly ILogger<DefaultSessionRepository> _logger;

		public DefaultSessionRepository(DbConnectionHelper dbConnection, ILogger<DefaultSessionRepository> logger) 
		{
			//TODO: Agregar asserts a todos los execute para verificar que no se pasen variables nulas

			_dbConnection = dbConnection;
			_logger			= logger;
		}

		public static UserSession ConvertReaderToUserSession(SqlDataReader reader) => new UserSession
		{
			SessionId	= reader.GetGuid(reader.GetOrdinal(IdColumnName)),
			UserId		= reader.GetGuid(reader.GetOrdinal(UserIdColumnName)),
			CreatedAt	= reader.GetDateTimeOffset(reader.GetOrdinal(CreatedAtColumnName)),
			ExpiresAt	= reader.GetDateTimeOffset(reader.GetOrdinal(ExpiresAtColumnName)),
			SessionInfo = reader.IsDBNull(reader.GetOrdinal(SessionInfoColumnName)) ? null : reader.GetString(reader.GetOrdinal(SessionInfoColumnName))
		};

		public async Task<OperationResult> CreateSessionAsync(UserSession newSession)
		{
			return (await _dbConnection.ExecuteAsync(
					sql: @$"INSERT INTO {TableName} ({IdColumnName}, {UserIdColumnName}, {CreatedAtColumnName}, {ExpiresAtColumnName}, {SessionInfoColumnName})
						VALUES (@SessionId, @UserId, @CreatedAt, @ExpiresAt, @SessionInfo)",
					parameters: new Dictionary<string, object?>
						{
							{ "@SessionId",		newSession.SessionId   },
							{ "@UserId",        newSession.UserId},
							{ "@CreatedAt",		newSession.CreatedAt},
							{ "@ExpiresAt",		newSession.ExpiresAt},
							{ "@SessionInfo",   newSession.SessionInfo}
						}
					)).Match<OperationResult>(
						onValue: (rowsAffected) => rowsAffected > 0 ? Success() : Failure("No changes were made"),
						onError: (error) => Failure(error)
					);
		}

		public async Task<OptionalResult<IList<UserSession>>> GetActiveSessionsForUserAsync(Guid userId)
		{
			return (await _dbConnection.ExecuteReaderAsync(
					sql: @$"SELECT * FROM {TableName} AS s
							WHERE s.{UserIdColumnName} = @UserId 
							AND s.{ExpiresAtColumnName} > GETUTCDATE()",
					converter: ConvertReaderToUserSession,
					parameters: new Dictionary<string, object>
					{
						{ "@UserId", userId }
					}
				)).Match<OptionalResult<IList<UserSession>>>(
					onValue: (sessions) => sessions.Any() ? Value<IList<UserSession>>(sessions) : NoValue<IList<UserSession>>(),
					onError: (error) => Error<IList<UserSession>>(error)
				);
		}

		public async Task<OptionalResult<IList<UserSession>>> GetAllSessionsForUserAsync(Guid userId)
		{
			return (await _dbConnection.ExecuteReaderAsync(
				sql: @$"SELECT * FROM {TableName} AS s
						WHERE s.{UserIdColumnName} = @UserId",
				converter: ConvertReaderToUserSession,
				parameters: new Dictionary<string, object>
					{
						{ "@UserId", userId }
					}
				)).Match<OptionalResult<IList<UserSession>>>(
					onValue: (sessions) => sessions.Any() ? Value<IList<UserSession>>(sessions) : NoValue<IList<UserSession>>(),
					onError: (error) => Error<IList<UserSession>>(error)
				);
		}

		public async Task<OptionalResult<(UserSession session, ApplicationUser user)>> GetSessionAsync(Guid sessionId)
		{
			ApplicationUser? user	= null;
			UserSession? session	= null;

			var result = await _dbConnection.ExecuteReaderSingleAsync(
				sql: $@"SELECT  s.{IdColumnName}, 
								s.{UserIdColumnName}, 
								s.{CreatedAtColumnName}, 
								s.{ExpiresAtColumnName}, 
								s.{SessionInfoColumnName},
								u.{Constants.Tables.ApplicationUser.UserIdColumnName}, 
								u.{Constants.Tables.ApplicationUser.NameColumnName}, 
								u.{Constants.Tables.ApplicationUser.NormalizedNameColumnName}, 
								u.{Constants.Tables.ApplicationUser.EmailColumnName}, 
								u.{Constants.Tables.ApplicationUser.NormalizedEmailColumnName}, 
								u.{Constants.Tables.ApplicationUser.EmailConfirmedColumnName}, 
								u.{Constants.Tables.ApplicationUser.PasswordHashColumnName},
								u.{Constants.Tables.ApplicationUser.SecurityStampColumnName}
					FROM {TableName} AS s
					INNER JOIN {Constants.Tables.ApplicationUser.TableName} AS u ON u.{Constants.Tables.ApplicationUser.UserIdColumnName} = s.{UserIdColumnName}
					WHERE s.{IdColumnName} = @SessionId",
				converter: (reader) =>
				{
					user	= DefaultUserRepository.ConvertReaderToUser(reader);
					session = ConvertReaderToUserSession(reader);
					return true;
				},
				parameters: new Dictionary<string, object>
				{
					{ "@SessionId", sessionId   }
				});

			return result.Match<OptionalResult<(UserSession session, ApplicationUser user)>>(
				onValue: (_) =>		Value((session!, user!)),
				onError: (error) => Error<(UserSession session, ApplicationUser user)>(error),
				onEmpty: () =>		NoValue<(UserSession session, ApplicationUser user)>()
			);
		}

		public async Task<OperationResult> UpdateSessionAsync(UserSession session)
		{
			return (await _dbConnection.ExecuteAsync(
					sql: @$"UPDATE {TableName} 
							SET {ExpiresAtColumnName}	= @ExpiresAt, 
								{SessionInfoColumnName} = @SessionInfo
							WHERE {IdColumnName} = @SessionId",
					parameters: new Dictionary<string, object?>
						{
							{ "@SessionId",		session.SessionId   },
							{ "@ExpiresAt",		session.ExpiresAt},
							{ "@SessionInfo",   session.SessionInfo}
						}
					)).Match<OperationResult>(
						onValue: (rowsAffected) => rowsAffected > 0 ? Success() : Failure("No changes were made"),
						onError: (error) => Failure(error)
					);
		}

		//TODO: Implementar funciones de revocacion de sesiones
		public Task<OperationResult> RevokeAllSessionsAsync(Guid userId)
		{
			throw new NotImplementedException();
		}

		public Task<OperationResult> RevokeSessionAsync(Guid sessionId)
		{
			throw new NotImplementedException();
		}
	}
}