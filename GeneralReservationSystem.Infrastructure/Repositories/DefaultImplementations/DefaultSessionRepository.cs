using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.Entities.Authentication;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using GeneralReservationSystem.Infrastructure.Repositories.DefaultImplementations.Authentication;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static GeneralReservationSystem.Application.Common.OperationResult;

namespace GeneralReservationSystem.Infrastructure.Repositories.DefaultImplementations
{
    public class DefaultSessionRepository : ISessionRepository
    {
        private readonly DbConnectionHelper _dbConnection;
        private readonly ILogger<DefaultUserRepository> _logger;

        public DefaultSessionRepository(DbConnectionHelper dbConnection, ILogger<DefaultUserRepository> logger)
        {
            _dbConnection = dbConnection;
            _logger = logger;
        }

        public static UserSession ConvertReaderToSession(SqlDataReader reader)
        {
            return new UserSession
            {
                SessionID = reader.GetGuid(reader.GetOrdinal(Constants.Tables.UserSession.IdColumnName)),
                UserID = reader.GetGuid(reader.GetOrdinal(Constants.Tables.UserSession.UserIdColumnName)),
                CreatedAt = reader.GetDateTimeOffset(reader.GetOrdinal(Constants.Tables.UserSession.CreatedAtColumnName)),
                ExpiresAt = reader.IsDBNull(reader.GetOrdinal(Constants.Tables.UserSession.ExpiresAtColumnName)) ? null : reader.GetDateTimeOffset(reader.GetOrdinal(Constants.Tables.UserSession.ExpiresAtColumnName)),
                SessionInfo = reader.IsDBNull(reader.GetOrdinal(Constants.Tables.UserSession.SessionInfoColumnName)) ? null : reader.GetString(reader.GetOrdinal(Constants.Tables.UserSession.SessionInfoColumnName))
            };
        }

        public async Task<OperationResult> CreateSessionAsync(UserSession newSession)
        {
            // Build the insert statement dynamically based on non-null values
            var columns = new List<string>
            {
                Constants.Tables.UserSession.IdColumnName,
                Constants.Tables.UserSession.UserIdColumnName,
                Constants.Tables.UserSession.CreatedAtColumnName
            };
            var values = new List<string>
            {
                "NEWID()",
                "@UserId",
                "@CreatedAt"
            };
            var sessionParameters = new Dictionary<string, object>
            {
                { "@UserId", newSession.UserID },
                { "@CreatedAt", newSession.CreatedAt }
            };

            if (newSession.ExpiresAt != null)
            {
                columns.Add(Constants.Tables.UserSession.ExpiresAtColumnName);
                values.Add("@ExpiresAt");
                sessionParameters.Add("@ExpiresAt", newSession.ExpiresAt);
            }
            if (newSession.SessionInfo != null)
            {
                columns.Add(Constants.Tables.UserSession.SessionInfoColumnName);
                values.Add("@SessionInfo");
                sessionParameters.Add("@SessionInfo", newSession.SessionInfo);
            }

            // Insert the new session - capture the new SessionId created at the database
            var insertSessionSql = $@"
                INSERT INTO {Constants.Tables.UserSession.TableName} 
                ({string.Join(", ", columns)})
                OUTPUT INSERTED.{Constants.Tables.UserSession.IdColumnName}
                VALUES 
                ({string.Join(", ", values)});";

            var sessionCreationResultId = (await _dbConnection.ExecuteScalarAsync<Guid>(insertSessionSql, sessionParameters))
                .Match(
                    onValue: (newId) => {
                        return newId;
                    },
                    onEmpty: () => {
                        _logger.LogError($"Session creation returned no result for session {newSession}.");
                        throw new Exception("Session creation returned no result.");
                    },
                    onError: (error) => {
                        _logger.LogError(error, $"Error creating session {newSession}.");
                        throw new Exception($"Error creating session. {error}");
                    });

            newSession.SessionID = sessionCreationResultId;

            return Success();
        }

        public async Task<OptionalResult<IList<UserSession>>> GetActiveSessionsByUserIdAsync(Guid userId)
        {
            // TODO: El tiempo actual deberia ser pasado como parametro? Actualmente se compara 
            // con SYSDATETIMEOFFSET() del servidor SQL.
            return await _dbConnection.ExecuteReaderAsync(
                sql: $@"
					SELECT *
					FROM {Constants.Tables.UserSession.TableName}
					WHERE {Constants.Tables.UserSession.UserIdColumnName} = @UserId
					  AND ({Constants.Tables.UserSession.ExpiresAtColumnName} IS NULL OR {Constants.Tables.UserSession.ExpiresAtColumnName} > SYSDATETIMEOFFSET());",
                converter: ConvertReaderToSession,
                parameters: new Dictionary<string, object>
                {
                    { "@UserId", userId }
                });
        }

        public async Task<OptionalResult<UserSession>> GetSessionByIdAsync(Guid sessionId)
        {
            return await _dbConnection.ExecuteReaderSingleAsync(
                sql: $"SELECT * FROM {Constants.Tables.UserSession.TableName} WHERE {Constants.Tables.UserSession.IdColumnName} = @SessionId;",
                converter: ConvertReaderToSession,
                parameters: new Dictionary<string, object>
                {
                    { "@SessionId", sessionId }
                });
        }

        public async Task<OperationResult> UpdateSessionAsync(UserSession updatedSession)
        {
            // Only ExpiresAt and SessionInfo can/should be updated
            var setClauses = new List<string>();
            var parameters = new Dictionary<string, object>
            {
                { "@SessionId", updatedSession.SessionID }
            };

            setClauses.Add($"{Constants.Tables.UserSession.ExpiresAtColumnName} = @ExpiresAt");
            parameters.Add("@ExpiresAt", (object?)updatedSession.ExpiresAt ?? DBNull.Value);

            setClauses.Add($"{Constants.Tables.UserSession.SessionInfoColumnName} = @SessionInfo");
            parameters.Add("@SessionInfo", (object?)updatedSession.SessionInfo ?? DBNull.Value);

            var sql = $@"
                UPDATE {Constants.Tables.UserSession.TableName}
                SET {string.Join(", ", setClauses)}
                WHERE {Constants.Tables.UserSession.IdColumnName} = @SessionId;";

            return (await _dbConnection.ExecuteAsync(sql, parameters)).Match<OperationResult>(
                onValue: rowsAffected => rowsAffected > 0 ? Success() : Failure("No session found to update."),
                onEmpty: () => Failure("No session found to update."),
                onError: error => Failure(error)
            );
        }

        public async Task<OperationResult> DeleteSessionAsync(Guid sessionId)
        {
            return (await _dbConnection.ExecuteAsync(
                sql: $@"
					DELETE FROM {Constants.Tables.UserSession.TableName}
					WHERE {Constants.Tables.UserSession.IdColumnName} = @SessionId;",
                parameters: new Dictionary<string, object>
                {
                    { "@SessionId", sessionId }
                })).Match<OperationResult>(
                    onValue: (rowsAffected) => rowsAffected > 0 ? Success() : Failure("No entries were deleted"),
                    onEmpty: () => Failure(),
                    onError: (error) => Failure($"Unable to delete session. {error}")
                );
        }
    }
}
