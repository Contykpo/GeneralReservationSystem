using Microsoft.Data.SqlClient;

using GeneralReservationSystem.Application.Entities.Authentication;
using GeneralReservationSystem.Infrastructure.Repositories.Interfaces;

using GeneralReservationSystem.Infrastructure.Common;

using Microsoft.Extensions.Logging;

using static GeneralReservationSystem.Infrastructure.Common.OperationResult;
using static GeneralReservationSystem.Infrastructure.Common.OptionalResult<object>;

using static GeneralReservationSystem.Infrastructure.Constants.Tables.ApplicationUser;

namespace GeneralReservationSystem.Infrastructure.Repositories.DefaultImplementations
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
				UserId				= reader.GetGuid(reader.GetOrdinal(UserIdColumnName)),
				SecurityStamp		= reader.GetGuid(reader.GetOrdinal(SecurityStampColumnName)),

				UserName			= reader.GetString(reader.GetOrdinal(NameColumnName)),
				NormalizedUserName	= reader.GetString(reader.GetOrdinal(NormalizedNameColumnName)),
				Email				= reader.GetString(reader.GetOrdinal(EmailColumnName)),
				NormalizedEmail		= reader.GetString(reader.GetOrdinal(NormalizedEmailColumnName)),

				EmailConfirmed		= reader.GetBoolean(reader.GetOrdinal(EmailConfirmedColumnName)),
				
				PasswordHash		= (byte[])reader[PasswordHashColumnName],
				PasswordSalt		= (byte[])reader[PasswordSaltColumnName],
			};
		}

		//TODO: Mover a un repositorio de roles
		public static ApplicationRole ConvertReaderToRole(SqlDataReader reader)
		{
			return new ApplicationRole
			{
				Name			= reader.GetString(reader.GetOrdinal(Constants.Tables.ApplicationRole.NameColumnName)),
				NormalizedName	= reader.GetString(reader.GetOrdinal(Constants.Tables.ApplicationRole.NormalizedNameColumnName))
			};
		}

		public async Task<OptionalResult<IList<ApplicationUser>>> GetAllAsync()
		{
			return await _dbConnection.ExecuteReaderAsync<ApplicationUser>(
				sql: $"SELECT * FROM {TableName};",
				converter: ConvertReaderToUser);
		}

		public async Task<OptionalResult<ApplicationUser>> GetByEmailAsync(string email)
		{
			return await _dbConnection.ExecuteReaderSingleAsync<ApplicationUser>(
				sql: $"SELECT * FROM {TableName} WHERE {NormalizedEmailColumnName} = @Email;",
				converter: ConvertReaderToUser,
				parameters: new Dictionary<string, object>
				{
					{ "@Email", email.ToUpperInvariant() }
				});
		}

		public async Task<OptionalResult<ApplicationUser>> GetByGuidAsync(Guid guid)
		{
			return await _dbConnection.ExecuteReaderSingleAsync<ApplicationUser>(
				sql:		$"SELECT * FROM {TableName} WHERE {UserIdColumnName} = @UserId;",
				converter:	ConvertReaderToUser,
				parameters: new Dictionary<string, object>
				{
					{ "@UserId", guid }
				});
		}

		public async Task<OptionalResult<IList<ApplicationRole>>> GetUserRolesAsync(Guid userGuid)
		{
			return await _dbConnection.ExecuteReaderAsync<ApplicationRole>(
				sql: $@"
					SELECT r.{Constants.Tables.ApplicationRole.NameColumnName}, r.{Constants.Tables.ApplicationRole.NormalizedNameColumnName}
					FROM {Constants.Tables.ApplicationRole.TableName} AS r
					INNER JOIN {Constants.Tables.UserRole.TableName} AS ur ON r.{Constants.Tables.ApplicationRole.RoleIdColumnName} = ur.{Constants.Tables.UserRole.RoleIdColumnName}
					WHERE ur.{Constants.Tables.UserRole.UserIdColumnName} = @UserId;",
				converter: ConvertReaderToRole,
				parameters: new Dictionary<string, object>
				{
					{ "@UserId", userGuid }
				});
		}

		public async Task<OptionalResult<IList<ApplicationUser>>> GetWithRoleAsync(string roleName)
		{
			return await _dbConnection.ExecuteReaderAsync<ApplicationUser>(
				sql: $@"
					SELECT u.*
					FROM {TableName} AS u
					INNER JOIN {Constants.Tables.UserRole.TableName} AS ur ON u.{UserIdColumnName} = ur.{Constants.Tables.UserRole.UserIdColumnName}
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

				}))	.IfValue((rowsAffected) => result = rowsAffected > 0 ? Success() : Failure())
					.IfEmpty(() => result = Success())
					.IfError((error) => result = Failure(error));

			return result;
		}

		public async Task<OperationResult> CreateUserAsync(ApplicationUser newUser, IEnumerable<ApplicationRole> userRoles)
		{
			return (await _dbConnection.ExecuteInTransactionAsync(async (connection, transaction) =>
			{
				//TODO: Considerar si insertar id del usuario directamente o si dejar que la base de datos lo genere
				// Insert the new user
				var insertUserSql = $@"
					INSERT INTO {TableName} 
					(	{UserIdColumnName}, 
						{NameColumnName}, 
						{NormalizedNameColumnName}, 
						{EmailColumnName}, 
						{NormalizedEmailColumnName}, 
						{EmailConfirmedColumnName}, 
						{PasswordHashColumnName}, 
						{PasswordSaltColumnName}, 
						{SecurityStampColumnName}
					)
					VALUES 
					(@UserId, @UserName, @NormalizedUserName, @Email, @NormalizedEmail, @EmailConfirmed, @PasswordHash, @PasswordSalt, @SecurityStamp);";

				var userParameters = new Dictionary<string, object>
				{
					{ "@UserId",                newUser.UserId },
					{ "@UserName",              newUser.UserName },
					{ "@NormalizedUserName",    newUser.NormalizedUserName },
					{ "@Email",                 newUser.Email },
					{ "@NormalizedEmail",       newUser.NormalizedEmail },
					{ "@EmailConfirmed",        newUser.EmailConfirmed },
					{ "@PasswordHash",          newUser.PasswordHash },
					{ "@PasswordSalt",          newUser.PasswordSalt },
					{ "@SecurityStamp",         newUser.SecurityStamp }
				};

				int rowsAffected = 0;

				var userCreationResult = (await _dbConnection.ExecuteAsync(insertUserSql, connection, userParameters, transaction))
					.Match<int>(
						onValue: (rows) => {						
							if (rows == 0)
							{
								_logger.LogError($"User creation did not affect any rows for user {newUser}");
								throw new Exception("User creation did not affect any rows.");
							}
							else
								return rows;
						},
						onEmpty: () => {
							_logger.LogError($"User creation returned no result for user {newUser}.");
							throw new Exception("User creation returned no result.");
						},
						onError: (error) => {
							_logger.LogError(error, $"Error creating user {newUser}.");
							throw new Exception($"Error creating user. {error}");
					});

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

					rowsAffected = 0;

					(await _dbConnection.ExecuteAsync(assignRoleSql, connection, roleParameters, transaction)).Match(
						onValue: (rows) => Value<int>(rows),
						onEmpty: () => {
							_logger.LogError($"Role assignment returned no result for role {role} and user {newUser.UserName}.");
							throw new Exception("Role assignment returned no result.");
						},
						onError: (error) => {
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

			}));
		}

		public async Task<OptionalResult<bool>> ExistsWithEmailAsync(string email)
		{
			return (await _dbConnection.ExecuteReaderSingleAsync<int>(
				sql:		$"SELECT COUNT(1) FROM {TableName} WHERE {NormalizedEmailColumnName} = @Email;",
				converter:	reader => reader.GetInt32(0),
				parameters: new Dictionary<string, object>
				{
					{ "@Email", email }
				})).Match<OptionalResult<bool>>(
					onValue: (matchingUserCount) => Value(matchingUserCount > 0),
					onEmpty: () =>  NoValue<bool>(),
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
					UPDATE {TableName}
					SET 
						{NameColumnName}				= @UserName,
						{NormalizedNameColumnName}		= @NormalizedUserName,
						{EmailColumnName}				= @Email,
						{NormalizedEmailColumnName}		= @NormalizedEmail,
						{EmailConfirmedColumnName}		= @EmailConfirmed,
						{PasswordHashColumnName}		= @PasswordHash,
						{PasswordSaltColumnName}		= @PasswordSalt,
						{SecurityStampColumnName}		= @SecurityStamp
					WHERE {UserIdColumnName}			= @UserId;",
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
