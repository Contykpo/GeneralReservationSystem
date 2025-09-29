using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.Entities.Authentication;
using GeneralReservationSystem.Application.Repositories.Interfaces.Authentication;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

using static GeneralReservationSystem.Application.Common.OperationResult;

namespace GeneralReservationSystem.Infrastructure.Repositories.DefaultImplementations.Authentication
{
    public class DefaultRoleRepository : IRoleRepository
    {
        private readonly DbConnectionHelper _dbConnection;
        private readonly ILogger<DefaultUserRepository> _logger;

        public DefaultRoleRepository(DbConnectionHelper dbConnection, ILogger<DefaultUserRepository> logger)
        {
            _dbConnection = dbConnection;
            _logger = logger;
        }

        public static ApplicationRole ConvertReaderToRole(SqlDataReader reader)
        {
            return new ApplicationRole
            {
                RoleId = reader.GetGuid(reader.GetOrdinal(Constants.Tables.ApplicationRole.RoleIdColumnName)),
                Name = reader.GetString(reader.GetOrdinal(Constants.Tables.ApplicationRole.NameColumnName)),
                NormalizedName = reader.GetString(reader.GetOrdinal(Constants.Tables.ApplicationRole.NormalizedNameColumnName))
            };
        }

        public async Task<OperationResult> CreateRoleAsync(ApplicationRole role)
        {
            // Insert the new role - capture the new RoleId created at the database
            var insertRoleSql = $@"
					INSERT INTO {Constants.Tables.ApplicationRole.TableName} 
					(	{Constants.Tables.ApplicationRole.RoleIdColumnName}, 
						{Constants.Tables.ApplicationRole.NameColumnName}, 
						{Constants.Tables.ApplicationRole.NormalizedNameColumnName}
					)
					OUTPUT INSERTED.{Constants.Tables.ApplicationRole.RoleIdColumnName}
					VALUES 
					(NEWID(), @Name, @NormalizedName);";

            var roleParameters = new Dictionary<string, object>
            {
                { "@Name", role.Name },
                { "@NormalizedName", role.NormalizedName }
            };

            var roleCreationResultId = (await _dbConnection.ExecuteScalarAsync<Guid>(insertRoleSql, roleParameters))
                .Match(
                    onValue: (newId) =>
                    {
                        return newId;
                    },
                    onEmpty: () =>
                    {
                        _logger.LogError($"Role creation returned no result for role {role}.");
                        throw new Exception("Role creation returned no result.");
                    },
                    onError: (error) =>
                    {
                        _logger.LogError(error, $"Error creating role {role}.");
                        throw new Exception($"Error creating role. {error}");
                    });

            role.RoleId = roleCreationResultId;

            return Success();
        }

        public async Task<OptionalResult<ApplicationRole>> GetByGuidAsync(Guid guid)
        {
            return await _dbConnection.ExecuteReaderSingleAsync(
                sql: $"SELECT * FROM {Constants.Tables.ApplicationRole.TableName} WHERE {Constants.Tables.ApplicationRole.RoleIdColumnName} = @RoleId;",
                converter: ConvertReaderToRole,
                parameters: new Dictionary<string, object>
                {
                    { "@RoleId", guid }
                });
        }

        public async Task<OptionalResult<ApplicationRole>> GetByNameAsync(string roleName)
        {
            return await _dbConnection.ExecuteReaderSingleAsync(
                sql: $"SELECT * FROM {Constants.Tables.ApplicationRole.TableName} WHERE {Constants.Tables.ApplicationRole.NormalizedNameColumnName} = @Name;",
                converter: ConvertReaderToRole,
                parameters: new Dictionary<string, object>
                {
                    { "@Name", roleName.ToUpperInvariant() }
                });
        }

        public async Task<OperationResult> UpdateRoleAsync(ApplicationRole role)
        {
            return (await _dbConnection.ExecuteAsync(
                sql: $@"
					UPDATE {Constants.Tables.ApplicationRole.TableName}
					SET 
						{Constants.Tables.ApplicationRole.NameColumnName}				= @Name,
						{Constants.Tables.ApplicationRole.NormalizedNameColumnName}		= @NormalizedName
					WHERE {Constants.Tables.ApplicationRole.RoleIdColumnName}			= @RoleId;",
                parameters: new Dictionary<string, object>
                {
                    { "@RoleId",                role.RoleId },
                    { "@Name",                  role.Name },
                    { "@NormalizedName",        role.NormalizedName }
                })).Match<OperationResult>(
                    onValue: (rowsAffected) => rowsAffected > 0 ? Success() : Failure("No entries were updated"),
                    onEmpty: () => Failure(),
                    onError: (error) => Failure($"Unable to update role. {error}")
                );
        }

        public async Task<OperationResult> DeleteRoleAsync(Guid roleId)
        {
            return (await _dbConnection.ExecuteAsync(
                sql: $@"
					DELETE FROM {Constants.Tables.ApplicationRole.TableName}
					WHERE {Constants.Tables.ApplicationRole.RoleIdColumnName} = @RoleId;",
                parameters: new Dictionary<string, object>
                {
                    { "@RoleId", roleId }
                })).Match<OperationResult>(
                    onValue: (rowsAffected) => rowsAffected > 0 ? Success() : Failure("No entries were deleted"),
                    onEmpty: () => Failure(),
                    onError: (error) => Failure($"Unable to delete role. {error}")
                );
        }
    }
}