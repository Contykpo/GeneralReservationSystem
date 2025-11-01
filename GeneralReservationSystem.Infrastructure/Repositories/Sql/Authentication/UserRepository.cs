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

                string filterClause = SqlCommandHelper.BuildFiltersClause<User>(searchDto.Filters);
                string whereClause = string.IsNullOrEmpty(filterClause) ? "" : $"WHERE {filterClause}";
                string baseQuery = $"FROM grsdb.\"{_tableName}\" {whereClause}";

                int page = searchDto.Page > 0 ? searchDto.Page : 1;
                int pageSize = searchDto.PageSize > 0 ? searchDto.PageSize : 10;
                int offset = (page - 1) * pageSize;
                StringBuilder sql = new();
                _ = sql.Append($"SELECT \"UserId\", \"UserName\", \"Email\", \"IsAdmin\" {baseQuery}");
                if (searchDto.Orders != null && searchDto.Orders.Count > 0)
                {
                    string orderByClause = SqlCommandHelper.BuildOrderByClause<User>(searchDto.Orders);
                    _ = sql.Append($" ORDER BY {orderByClause}");
                }
                _ = sql.Append($" LIMIT {pageSize} OFFSET {offset}");
                cmd.CommandText = sql.ToString();
                AddFilterParameters(cmd, searchDto.Filters);

                using DbCommand countCmd = SqlCommandHelper.CreateCommand(conn, transaction);
                countCmd.CommandText = $"SELECT COUNT(*) {baseQuery}";
                AddFilterParameters(countCmd, searchDto.Filters);

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

                PagedResult<UserInfo> result = await MapPagedResultAsync(cmd, countCmd, mapFunc, page, pageSize, cancellationToken);
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
