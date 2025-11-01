using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using GeneralReservationSystem.Infrastructure.Helpers;
using System.Data.Common;
using System.Text;

namespace GeneralReservationSystem.Infrastructure.Repositories.Sql
{
    public class StationRepository(Func<DbConnection> connectionFactory) : Repository<Station>(connectionFactory), IStationRepository
    {
        private readonly Func<DbConnection> connectionFactory = connectionFactory;

        public async Task<Station?> GetByIdAsync(int stationId, CancellationToken cancellationToken = default)
        {
            using DbConnection conn = await SqlCommandHelper.CreateAndOpenConnectionAsync(connectionFactory, cancellationToken);
            using DbCommand cmd = SqlCommandHelper.CreateCommand(conn);
            cmd.CommandText = $"SELECT * FROM grsdb.\"{_tableName}\" WHERE \"StationId\" = @id";
            SqlCommandHelper.AddParameter(cmd, "@id", stationId, typeof(int));
            using DbDataReader reader = await SqlCommandHelper.ExecuteReaderAsync(cmd, cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? DataReaderMapper.MapReaderToEntity<Station>(reader, _properties) : null;
        }

        public async Task<PagedResult<Station>> SearchAsync(PagedSearchRequestDto searchDto, CancellationToken cancellationToken = default)
        {
            using DbConnection conn = await SqlCommandHelper.CreateAndOpenConnectionAsync(connectionFactory, cancellationToken);
            using DbTransaction transaction = await SqlCommandHelper.CreateTransactionAsync(conn, cancellationToken);
            try
            {
                using DbCommand cmd = SqlCommandHelper.CreateCommand(conn, transaction);

                string filterClause = SqlCommandHelper.BuildFiltersClause<Station>(searchDto.Filters);
                string whereClause = string.IsNullOrEmpty(filterClause) ? "" : $"WHERE {filterClause}";
                string baseQuery = $"FROM grsdb.\"{_tableName}\" {whereClause}";

                int page = searchDto.Page > 0 ? searchDto.Page : 1;
                int pageSize = searchDto.PageSize > 0 ? searchDto.PageSize : 10;
                int offset = (page - 1) * pageSize;
                StringBuilder sql = new();
                _ = sql.Append($"SELECT * {baseQuery}");
                if (searchDto.Orders.Count > 0)
                {
                    string orderByClause = SqlCommandHelper.BuildOrderByClause<Station>(searchDto.Orders);
                    _ = sql.Append($" ORDER BY {orderByClause}");
                }
                _ = sql.Append($" LIMIT {pageSize} OFFSET {offset}");
                cmd.CommandText = sql.ToString();
                AddFilterParameters<Station>(cmd, searchDto.Filters);

                using DbCommand countCmd = SqlCommandHelper.CreateCommand(conn, transaction);
                countCmd.CommandText = $"SELECT COUNT(*) {baseQuery}";
                AddFilterParameters<Station>(countCmd, searchDto.Filters);

                PagedResult<Station> result = await MapPagedResultAsync(
                    cmd,
                    countCmd,
                    (reader) => DataReaderMapper.MapReaderToEntity<Station>(reader, _properties),
                    page,
                    pageSize,
                    cancellationToken
                );
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
