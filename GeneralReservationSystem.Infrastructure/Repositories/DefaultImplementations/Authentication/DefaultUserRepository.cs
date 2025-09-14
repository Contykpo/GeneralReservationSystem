using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.Entities.Authentication;
using GeneralReservationSystem.Application.Repositories.Interfaces.Authentication;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using static GeneralReservationSystem.Application.Common.OperationResult;
using static GeneralReservationSystem.Application.Common.OptionalResult<object>;

namespace GeneralReservationSystem.Infrastructure.Repositories.DefaultImplementations.Authentication
{
    public class DefaultUserRepository : IUserRepository
    {
        private readonly DbConnectionHelper _dbConnection;
        private readonly ILogger<DefaultUserRepository> _logger;

        public DefaultUserRepository(DbConnectionHelper dbConnection, ILogger<DefaultUserRepository> logger)
        {
            _dbConnection = dbConnection;
            _logger = logger;
        }

        public static ApplicationUser ConvertReaderToUser(SqlDataReader reader)
        {
            return new ApplicationUser
            {
                UserId = reader.GetGuid(reader.GetOrdinal(Constants.Tables.ApplicationUser.UserIdColumnName)),
                SecurityStamp = reader.GetGuid(reader.GetOrdinal(Constants.Tables.ApplicationUser.SecurityStampColumnName)),

                UserName = reader.GetString(reader.GetOrdinal(Constants.Tables.ApplicationUser.NameColumnName)),
                NormalizedUserName = reader.GetString(reader.GetOrdinal(Constants.Tables.ApplicationUser.NormalizedNameColumnName)),
                Email = reader.GetString(reader.GetOrdinal(Constants.Tables.ApplicationUser.EmailColumnName)),
                NormalizedEmail = reader.GetString(reader.GetOrdinal(Constants.Tables.ApplicationUser.NormalizedEmailColumnName)),

                EmailConfirmed = reader.GetBoolean(reader.GetOrdinal(Constants.Tables.ApplicationUser.EmailConfirmedColumnName)),

                PasswordHash = (byte[])reader[Constants.Tables.ApplicationUser.PasswordHashColumnName],
                PasswordSalt = (byte[])reader[Constants.Tables.ApplicationUser.PasswordSaltColumnName],
            };
        }

        public async Task<OptionalResult<IList<ApplicationUser>>> GetAllAsync()
        {
            return await _dbConnection.ExecuteReaderAsync(
                sql: $"SELECT * FROM {Constants.Tables.ApplicationUser.TableName};",
                converter: ConvertReaderToUser);
        }

        public async Task<OptionalResult<ApplicationUser>> GetByEmailAsync(string email)
        {
            return await _dbConnection.ExecuteReaderSingleAsync(
                sql: $"SELECT * FROM {Constants.Tables.ApplicationUser.TableName} WHERE {Constants.Tables.ApplicationUser.NormalizedEmailColumnName} = @Email;",
                converter: ConvertReaderToUser,
                parameters: new Dictionary<string, object>
                {
                    { "@Email", email.ToUpperInvariant() }
                });
        }

        public async Task<OptionalResult<ApplicationUser>> GetByGuidAsync(Guid guid)
        {
            return await _dbConnection.ExecuteReaderSingleAsync(
                sql: $"SELECT * FROM {Constants.Tables.ApplicationUser.TableName} WHERE {Constants.Tables.ApplicationUser.UserIdColumnName} = @UserId;",
                converter: ConvertReaderToUser,
                parameters: new Dictionary<string, object>
                {
                    { "@UserId", guid }
                });
        }

        public async Task<OptionalResult<IList<ApplicationRole>>> GetUserRolesAsync(Guid userGuid)
        {
            return await _dbConnection.ExecuteReaderAsync(
                sql: $@"
					SELECT r.{Constants.Tables.ApplicationRole.NameColumnName}, r.{Constants.Tables.ApplicationRole.NormalizedNameColumnName}
					FROM {Constants.Tables.ApplicationRole.TableName} AS r
					INNER JOIN {Constants.Tables.UserRole.TableName} AS ur ON r.{Constants.Tables.ApplicationRole.RoleIdColumnName} = ur.{Constants.Tables.UserRole.RoleIdColumnName}
					WHERE ur.{Constants.Tables.UserRole.UserIdColumnName} = @UserId;",
                converter: DefaultRoleRepository.ConvertReaderToRole,
                parameters: new Dictionary<string, object>
                {
                    { "@UserId", userGuid }
                });
        }

        public async Task<OptionalResult<IList<ApplicationUser>>> GetWithRoleAsync(string roleName)
        {
            return await _dbConnection.ExecuteReaderAsync(
                sql: $@"
					SELECT u.*
					FROM {Constants.Tables.ApplicationUser.TableName} AS u
					INNER JOIN {Constants.Tables.UserRole.TableName} AS ur ON u.{Constants.Tables.ApplicationUser.UserIdColumnName} = ur.{Constants.Tables.UserRole.UserIdColumnName}
					INNER JOIN {Constants.Tables.ApplicationRole.TableName} AS r ON ur.{Constants.Tables.UserRole.RoleIdColumnName} = r.{Constants.Tables.ApplicationRole.RoleIdColumnName}
					WHERE r.{Constants.Tables.ApplicationRole.NormalizedNameColumnName} = @RoleName;",
                converter: ConvertReaderToUser,
                parameters: new Dictionary<string, object>
                {
                    { "@RoleName", roleName }
                });
        }

        public async Task<OperationResult> AddRoleAsync(Guid userGuid, string roleName)
        {
            OperationResult result = Failure();

            (await _dbConnection.ExecuteAsync(
                sql: $@"
					INSERT INTO {Constants.Tables.UserRole.TableName} ({Constants.Tables.UserRole.UserIdColumnName}, {Constants.Tables.UserRole.RoleIdColumnName})
					SELECT @UserId, r.{Constants.Tables.ApplicationRole.RoleIdColumnName}
					FROM {Constants.Tables.ApplicationRole.TableName} AS r
					WHERE r.{Constants.Tables.ApplicationRole.NormalizedNameColumnName} = @RoleName;",
                parameters: new Dictionary<string, object>
                {
                    { "@UserId", userGuid },
                    { "@RoleName", roleName }

                })).IfValue((rowsAffected) => result = rowsAffected > 0 ? Success() : Failure())
                    .IfEmpty(() => result = Success())
                    .IfError((error) => result = Failure(error));

            return result;
        }

        public async Task<OperationResult> CreateUserAsync(ApplicationUser newUser, IEnumerable<ApplicationRole> userRoles)
        {
            return await _dbConnection.ExecuteInTransactionAsync(async (connection, transaction) =>
            {
                // Insert the new user - capture the new UserId created at the database
                var insertUserSql = $@"
					INSERT INTO {Constants.Tables.ApplicationUser.TableName} 
					(	{Constants.Tables.ApplicationUser.UserIdColumnName}, 
						{Constants.Tables.ApplicationUser.NameColumnName}, 
						{Constants.Tables.ApplicationUser.NormalizedNameColumnName}, 
						{Constants.Tables.ApplicationUser.EmailColumnName}, 
						{Constants.Tables.ApplicationUser.NormalizedEmailColumnName}, 
						{Constants.Tables.ApplicationUser.EmailConfirmedColumnName}, 
						{Constants.Tables.ApplicationUser.PasswordHashColumnName}, 
						{Constants.Tables.ApplicationUser.PasswordSaltColumnName}, 
						{Constants.Tables.ApplicationUser.SecurityStampColumnName}
					)
					OUTPUT INSERTED.{Constants.Tables.ApplicationUser.UserIdColumnName}
					VALUES 
					(NEWID(), @UserName, @NormalizedUserName, @Email, @NormalizedEmail, @EmailConfirmed, @PasswordHash, @PasswordSalt, @SecurityStamp);";

                var userParameters = new Dictionary<string, object>
                {
                    { "@UserName",              newUser.UserName },
                    { "@NormalizedUserName",    newUser.NormalizedUserName },
                    { "@Email",                 newUser.Email },
                    { "@NormalizedEmail",       newUser.NormalizedEmail },
                    { "@EmailConfirmed",        newUser.EmailConfirmed },
                    { "@PasswordHash",          newUser.PasswordHash },
                    { "@PasswordSalt",          newUser.PasswordSalt },
                    { "@SecurityStamp",         newUser.SecurityStamp }
                };

                var userCreationResultId = (await _dbConnection.ExecuteScalarAsync<Guid>(insertUserSql, connection, userParameters, transaction))
                    .Match(
                        onValue: (newId) =>
                        {
                            return newId;
                        },
                        onEmpty: () =>
                        {
                            _logger.LogError($"User creation returned no result for user {newUser}.");
                            throw new Exception("User creation returned no result.");
                        },
                        onError: (error) =>
                        {
                            _logger.LogError(error, $"Error creating user {newUser}.");
                            throw new Exception($"Error creating user. {error}");
                        });

                newUser.UserId = userCreationResultId;

                // Assign roles to the new user
                foreach (var role in userRoles)
                {
                    var assignRoleSql = $@"
						INSERT INTO {Constants.Tables.UserRole.TableName} ({Constants.Tables.UserRole.UserIdColumnName}, {Constants.Tables.UserRole.RoleIdColumnName})
						SELECT @UserId, r.{Constants.Tables.ApplicationRole.RoleIdColumnName}
						FROM {Constants.Tables.ApplicationRole.TableName} AS r
						WHERE r.{Constants.Tables.ApplicationRole.NormalizedNameColumnName} = @RoleName;";
                    var roleParameters = new Dictionary<string, object>
                    {
                        { "@UserId", newUser.UserId },
                        { "@RoleName", role.NormalizedName }
                    };

                    int rowsAffected = 0;

                    (await _dbConnection.ExecuteAsync(assignRoleSql, connection, roleParameters, transaction)).Match(
                        onValue: (rows) => Value(rows),
                        onEmpty: () =>
                        {
                            _logger.LogError($"Role assignment returned no result for role {role} and user {newUser.UserName}.");
                            throw new Exception("Role assignment returned no result.");
                        },
                        onError: (error) =>
                        {
                            _logger.LogError(error, $"Error assigning role {role} to user {newUser.UserName}.");
                            throw new Exception($"Error assigning role {role.Name} to user {newUser.UserName}. {error}");
                        });

                    if (rowsAffected == 0)
                    {
                        _logger.LogError($"Failed to assign role {role} to user {newUser.UserName}. Rolling back transaction.");
                        throw new Exception($"Failed to assign role {role.Name} to user {newUser.UserName}");
                    }
                    else
                    {
                        _logger.LogDebug($"Role {role.Name} assigned to user {newUser} successfully.");
                    }
                }

                _logger.LogDebug($"All roles assigned to user {newUser} successfully.");

            });
        }

        public async Task<OptionalResult<bool>> ExistsWithEmailAsync(string email)
        {
            return (await _dbConnection.ExecuteReaderSingleAsync(
                sql: $"SELECT COUNT(1) FROM {Constants.Tables.ApplicationUser.TableName} WHERE {Constants.Tables.ApplicationUser.NormalizedEmailColumnName} = @Email;",
                converter: reader => reader.GetInt32(0),
                parameters: new Dictionary<string, object>
                {
                    { "@Email", email }
                })).Match<OptionalResult<bool>>(
                    onValue: (matchingUserCount) => Value(matchingUserCount > 0),
                    onEmpty: () => NoValue<bool>(),
                    onError: (error) => Error<bool>(error));
        }

        public async Task<OptionalResult<IList<ApplicationUser>>> FilterAsync(Func<ApplicationUser, bool> predicate)
        {
            return (await GetAllAsync()).Match<OptionalResult<IList<ApplicationUser>>>(
                onValue: (allUsers) => Value<IList<ApplicationUser>>(allUsers.Where(predicate).ToList()),
                onEmpty: () => NoValue<IList<ApplicationUser>>(),
                onError: (error) => Error<IList<ApplicationUser>>(error));
        }

        public async Task<OperationResult> RemoveRoleAsync(Guid userGuid, string roleName)
        {
            return (await _dbConnection.ExecuteAsync(
                sql: $@"
					DELETE ur
					FROM {Constants.Tables.UserRole.TableName} AS ur
					INNER JOIN {Constants.Tables.ApplicationRole.TableName} AS r ON ur.{Constants.Tables.UserRole.RoleIdColumnName} = r.{Constants.Tables.ApplicationRole.RoleIdColumnName}
					WHERE ur.{Constants.Tables.UserRole.UserIdColumnName} = @UserId
					AND r.{Constants.Tables.ApplicationRole.NormalizedNameColumnName} = @RoleName;",
                parameters: new Dictionary<string, object>
                {
                    { "@UserId", userGuid },
                    { "@RoleName", roleName.ToUpperInvariant() }

                })).Match<OperationResult>(
                    onValue: (rowsAffected) => rowsAffected > 0 ? Success() : Failure("No entries were updated"),
                    onEmpty: () => Failure(),
                    onError: (error) => Failure($"Unable ro remove role from user. {error}"));
        }

        public async Task<OperationResult> UpdateUserAsync(ApplicationUser user)
        {
            return (await _dbConnection.ExecuteAsync(
                sql: $@"
					UPDATE {Constants.Tables.ApplicationUser.TableName}
					SET 
						{Constants.Tables.ApplicationUser.NameColumnName}				= @UserName,
						{Constants.Tables.ApplicationUser.NormalizedNameColumnName}		= @NormalizedUserName,
						{Constants.Tables.ApplicationUser.EmailColumnName}				= @Email,
						{Constants.Tables.ApplicationUser.NormalizedEmailColumnName}	= @NormalizedEmail,
						{Constants.Tables.ApplicationUser.EmailConfirmedColumnName}		= @EmailConfirmed,
						{Constants.Tables.ApplicationUser.PasswordHashColumnName}		= @PasswordHash,
						{Constants.Tables.ApplicationUser.PasswordSaltColumnName}		= @PasswordSalt,
						{Constants.Tables.ApplicationUser.SecurityStampColumnName}		= @SecurityStamp
					WHERE {Constants.Tables.ApplicationUser.UserIdColumnName}			= @UserId;",
                parameters: new Dictionary<string, object>
                {
                    { "@UserId",                user.UserId },
                    { "@UserName",              user.UserName },
                    { "@NormalizedUserName",    user.NormalizedUserName },
                    { "@Email",                 user.Email },
                    { "@NormalizedEmail",       user.NormalizedEmail },
                    { "@EmailConfirmed",        user.EmailConfirmed },
                    { "@PasswordHash",          user.PasswordHash },
                    { "@PasswordSalt",          user.PasswordSalt },
                    { "@SecurityStamp",         user.SecurityStamp }

                })).Match<OperationResult>(
                    onValue: (rowsAffected) => rowsAffected > 0 ? Success() : Failure("No entries were updated"),
                    onEmpty: () => Failure(),
                    onError: (error) => Failure($"Unable to update user. {error}")
                );
        }
    }
}
