using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.DTOs.Authentication;
using GeneralReservationSystem.Application.Entities.Authentication;
using GeneralReservationSystem.Application.Repositories.Interfaces.Authentication;
using GeneralReservationSystem.Infrastructure.Helpers;
using System.Data.Common;
using System.Text;

namespace GeneralReservationSystem.Infrastructure.Repositories.Sql.Authentication
{
    public class UserRepository(Func<DbConnection> connectionFactory) : Repository<User>(connectionFactory), IUserRepository
    {
        private readonly Func<DbConnection> connectionFactory = connectionFactory;

        public async Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken)
        {
            using DbConnection conn = await SqlCommandHelper.CreateAndOpenConnectionAsync(connectionFactory, cancellationToken);
            using DbCommand cmd = SqlCommandHelper.CreateCommand(conn);
            cmd.CommandText = $"SELECT * FROM grsdb.\"{_tableName}\" WHERE \"UserId\" = @id";
            SqlCommandHelper.AddParameter(cmd, "@id", userId, typeof(int));
            using DbDataReader reader = await SqlCommandHelper.ExecuteReaderAsync(cmd, cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? DataReaderMapper.MapReaderToEntity<User>(reader, _properties) : null;
        }

        public async Task<User?> GetByUserNameOrEmailAsync(string normalizedInput, CancellationToken cancellationToken)
        {
            using DbConnection conn = await SqlCommandHelper.CreateAndOpenConnectionAsync(connectionFactory, cancellationToken);
            using DbCommand cmd = SqlCommandHelper.CreateCommand(conn);
            cmd.CommandText = $"SELECT * FROM grsdb.\"{_tableName}\" WHERE \"NormalizedUserName\" = @input OR \"NormalizedEmail\" = @input";
            SqlCommandHelper.AddParameter(cmd, "@input", normalizedInput, typeof(string));
            using DbDataReader reader = await SqlCommandHelper.ExecuteReaderAsync(cmd, cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? DataReaderMapper.MapReaderToEntity<User>(reader, _properties) : null;
        }

        public async Task<PagedResult<UserInfo>> SearchWithInfoAsync(PagedSearchRequestDto searchDto, CancellationToken cancellationToken)
        {
            using DbConnection conn = await SqlCommandHelper.CreateAndOpenConnectionAsync(connectionFactory, cancellationToken);
            using DbTransaction transaction = await SqlCommandHelper.CreateTransactionAsync(conn, cancellationToken);
            try
            {
                using DbCommand cmd = SqlCommandHelper.CreateCommand(conn, transaction);

                string filterClause = SqlCommandHelper.BuildFiltersClauses<UserInfo>(searchDto.FilterClauses);
                string orderByClause = SqlCommandHelper.BuildOrderByClauseWithDefault<UserInfo>(searchDto.Orders);
                bool hasFilter = !string.IsNullOrEmpty(filterClause);
                int page = searchDto.Page > 0 ? searchDto.Page : 1;
                int pageSize = searchDto.PageSize > 0 ? searchDto.PageSize : 10;
                int offset = (page - 1) * pageSize;
                
                StringBuilder sql = new();
                _ = sql.Append("SELECT * FROM (");
                _ = sql.Append($"SELECT \"UserId\", \"UserName\", \"Email\", \"IsAdmin\" FROM grsdb.\"{_tableName}\"");
                _ = sql.Append(") subquery");
                
                if (hasFilter)
                {
                    _ = sql.Append($" WHERE {filterClause}");
                }
                
                _ = sql.Append($" ORDER BY {orderByClause}");
                _ = sql.Append($" LIMIT {pageSize} OFFSET {offset}");
                
                cmd.CommandText = sql.ToString();
                SqlCommandHelper.AddFilterParameters<UserInfo>(cmd, searchDto.FilterClauses);

                using DbCommand countCmd = SqlCommandHelper.CreateCommand(conn, transaction);
                StringBuilder countSql = new();
                _ = countSql.Append("SELECT COUNT(*) FROM (");
                if (hasFilter)
                {
                    _ = countSql.Append("SELECT * FROM (");
                }
                _ = countSql.Append($"SELECT \"UserId\", \"UserName\", \"Email\", \"IsAdmin\" FROM grsdb.\"{_tableName}\"");
                if (hasFilter)
                {
                    _ = countSql.Append($") subquery WHERE {filterClause}");
                }
                _ = countSql.Append(')');
                countCmd.CommandText = countSql.ToString();
                SqlCommandHelper.AddFilterParameters<UserInfo>(countCmd, searchDto.FilterClauses);

                static UserInfo mapFunc(DbDataReader reader)
                {
                    return new()
                    {
                        UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                        UserName = reader.GetString(reader.GetOrdinal("UserName")),
                        Email = reader.GetString(reader.GetOrdinal("Email")),
                        IsAdmin = reader.GetBoolean(reader.GetOrdinal("IsAdmin"))
                    };
                }

                PagedResult<UserInfo> result = await SqlCommandHelper.MapPagedResultAsync(cmd, countCmd, mapFunc, page, pageSize, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}
