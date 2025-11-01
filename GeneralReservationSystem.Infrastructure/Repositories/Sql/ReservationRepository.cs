using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using GeneralReservationSystem.Infrastructure.Helpers;
using System.Data.Common;
using System.Text;

namespace GeneralReservationSystem.Infrastructure.Repositories.Sql
{
    public class ReservationRepository(Func<DbConnection> connectionFactory) : Repository<Reservation>(connectionFactory), IReservationRepository
    {
        private readonly Func<DbConnection> connectionFactory = connectionFactory;

        public async Task<Reservation?> GetByKeyAsync(int tripId, int seat, CancellationToken cancellationToken)
        {
            using DbConnection conn = await SqlCommandHelper.CreateAndOpenConnectionAsync(connectionFactory, cancellationToken);
            using DbCommand cmd = SqlCommandHelper.CreateCommand(conn);
            cmd.CommandText = $"SELECT * FROM grsdb.\"{_tableName}\" WHERE \"TripId\" = @tripId AND \"Seat\" = @seat";
            SqlCommandHelper.AddParameter(cmd, "@tripId", tripId, typeof(int));
            SqlCommandHelper.AddParameter(cmd, "@seat", seat, typeof(int));
            using DbDataReader reader = await SqlCommandHelper.ExecuteReaderAsync(cmd, cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? DataReaderMapper.MapReaderToEntity<Reservation>(reader, _properties) : null;
        }

        public async Task<IEnumerable<UserReservationDetailsDto>> GetByUserIdWithDetailsAsync(int userId, CancellationToken cancellationToken)
        {
            using DbConnection conn = await SqlCommandHelper.CreateAndOpenConnectionAsync(connectionFactory, cancellationToken);
            using DbCommand cmd = SqlCommandHelper.CreateCommand(conn);
            cmd.CommandText =
                $"SELECT r.\"TripId\", t.\"DepartureStationId\", dst.\"StationName\" AS \"DepartureStationName\", dst.\"City\" AS \"DepartureCity\", dst.\"Province\" AS \"DepartureProvince\", dst.\"Country\" AS \"DepartureCountry\", t.\"DepartureTime\", t.\"ArrivalStationId\", ast.\"StationName\" AS \"ArrivalStationName\", ast.\"City\" AS \"ArrivalCity\", ast.\"Province\" AS \"ArrivalProvince\", ast.\"Country\" AS \"ArrivalCountry\", t.\"ArrivalTime\", r.\"Seat\" " +
                $"FROM grsdb.\"{_tableName}\" r " +
                $"JOIN grsdb.\"Trip\" t ON r.\"TripId\" = t.\"TripId\" " +
                $"JOIN grsdb.\"Station\" dst ON t.\"DepartureStationId\" = dst.\"StationId\" " +
                $"JOIN grsdb.\"Station\" ast ON t.\"ArrivalStationId\" = ast.\"StationId\" " +
                $"WHERE r.\"UserId\" = @userId";
            SqlCommandHelper.AddParameter(cmd, "@userId", userId, typeof(int));
            List<UserReservationDetailsDto> items = [];
            using (DbDataReader reader = await SqlCommandHelper.ExecuteReaderAsync(cmd, cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    items.Add(new UserReservationDetailsDto
                    {
                        TripId = reader.GetInt32(reader.GetOrdinal("TripId")),
                        DepartureStationId = reader.GetInt32(reader.GetOrdinal("DepartureStationId")),
                        DepartureStationName = reader.GetString(reader.GetOrdinal("DepartureStationName")),
                        DepartureCity = reader.GetString(reader.GetOrdinal("DepartureCity")),
                        DepartureProvince = reader.GetString(reader.GetOrdinal("DepartureProvince")),
                        DepartureCountry = reader.GetString(reader.GetOrdinal("DepartureCountry")),
                        DepartureTime = reader.GetDateTime(reader.GetOrdinal("DepartureTime")),
                        ArrivalStationId = reader.GetInt32(reader.GetOrdinal("ArrivalStationId")),
                        ArrivalStationName = reader.GetString(reader.GetOrdinal("ArrivalStationName")),
                        ArrivalCity = reader.GetString(reader.GetOrdinal("ArrivalCity")),
                        ArrivalProvince = reader.GetString(reader.GetOrdinal("ArrivalProvince")),
                        ArrivalCountry = reader.GetString(reader.GetOrdinal("ArrivalCountry")),
                        ArrivalTime = reader.GetDateTime(reader.GetOrdinal("ArrivalTime")),
                        Seat = reader.GetInt32(reader.GetOrdinal("Seat"))
                    });
                }
            }
            return items;
        }

        public async Task<PagedResult<UserReservationDetailsDto>> SearchForUserIdWithDetailsAsync(int userId, PagedSearchRequestDto searchDto, CancellationToken cancellationToken)
        {
            using DbConnection conn = await SqlCommandHelper.CreateAndOpenConnectionAsync(connectionFactory, cancellationToken);
            using DbTransaction transaction = await SqlCommandHelper.CreateTransactionAsync(conn, cancellationToken);
            try
            {
                using DbCommand cmd = SqlCommandHelper.CreateCommand(conn, transaction);

                string filterClause = SqlCommandHelper.BuildFiltersClause<UserReservationDetailsDto>(searchDto.Filters);
                string orderByClause = SqlCommandHelper.BuildOrderByClause<UserReservationDetailsDto>(searchDto.Orders);
                bool hasFilter = !string.IsNullOrEmpty(filterClause);
                bool hasOrder = !string.IsNullOrEmpty(orderByClause);
                bool hasFilterOrOrder = hasFilter || hasOrder;
                int page = searchDto.Page > 0 ? searchDto.Page : 1;
                int pageSize = searchDto.PageSize > 0 ? searchDto.PageSize : 10;
                int offset = (page - 1) * pageSize;
                StringBuilder sql = new();
                if (hasFilterOrOrder)
                {
                    _ = sql.Append("SELECT * FROM (");
                }
                _ = sql.Append($"SELECT r.\"TripId\", t.\"DepartureStationId\", dst.\"StationName\" AS \"DepartureStationName\", dst.\"City\" AS \"DepartureCity\", dst.\"Province\" AS \"DepartureProvince\", dst.\"Country\" AS \"DepartureCountry\", t.\"DepartureTime\", t.\"ArrivalStationId\", ast.\"StationName\" AS \"ArrivalStationName\", ast.\"City\" AS \"ArrivalCity\", ast.\"Province\" AS \"ArrivalProvince\", ast.\"Country\" AS \"ArrivalCountry\", t.\"ArrivalTime\", r.\"Seat\" " +
                $"FROM grsdb.\"{_tableName}\" r " +
                $"JOIN grsdb.\"Trip\" t ON r.\"TripId\" = t.\"TripId\" " +
                $"JOIN grsdb.\"Station\" dst ON t.\"DepartureStationId\" = dst.\"StationId\" " +
                $"JOIN grsdb.\"Station\" ast ON t.\"ArrivalStationId\" = ast.\"StationId\"");
                if (hasFilterOrOrder)
                {
                    _ = sql.Append(") subquery");
                    _ = sql.Append($" WHERE r.\"UserId\" = @userId");
                    if (hasFilter)
                    {
                        _ = sql.Append($" AND {filterClause}");
                    }
                    if (hasOrder)
                    {
                        _ = sql.Append($" ORDER BY {orderByClause}");
                    }
                }
                else
                {
                    _ = sql.Append($" WHERE r.\"UserId\" = @userId");
                }
                _ = sql.Append($" LIMIT {pageSize} OFFSET {offset}");
                cmd.CommandText = sql.ToString();
                SqlCommandHelper.AddParameter(cmd, "@userId", userId, typeof(int));
                AddFilterParameters<UserReservationDetailsDto>(cmd, searchDto.Filters);

                using DbCommand countCmd = SqlCommandHelper.CreateCommand(conn, transaction);
                StringBuilder countSql = new();
                _ = countSql.Append("SELECT COUNT(*) FROM (");
                if (hasFilter)
                {
                    _ = countSql.Append("SELECT * FROM (");
                }
                _ = countSql.Append($"SELECT r.\"TripId\", t.\"DepartureStationId\", dst.\"StationName\" AS \"DepartureStationName\", dst.\"City\" AS \"DepartureCity\", dst.\"Province\" AS \"DepartureProvince\", dst.\"Country\" AS \"DepartureCountry\", t.\"DepartureTime\", t.\"ArrivalStationId\", ast.\"StationName\" AS \"ArrivalStationName\", ast.\"City\" AS \"ArrivalCity\", ast.\"Province\" AS \"ArrivalProvince\", ast.\"Country\" AS \"ArrivalCountry\", t.\"ArrivalTime\", r.\"Seat\" FROM grsdb.\"{_tableName}\" r JOIN grsdb.\"Trip\" t ON r.\"TripId\" = t.\"TripId\" JOIN grsdb.\"Station\" dst ON t.\"DepartureStationId\" = dst.\"StationId\" JOIN grsdb.\"Station\" ast ON t.\"ArrivalStationId\" = ast.\"StationId\" WHERE r.\"UserId\" = @userId");
                if (hasFilter)
                {
                    _ = countSql.Append($" AND {filterClause}");
                    _ = countSql.Append(")");
                }
                _ = countSql.Append(")");
                countCmd.CommandText = countSql.ToString();
                SqlCommandHelper.AddParameter(countCmd, "@userId", userId, typeof(int));
                AddFilterParameters<UserReservationDetailsDto>(countCmd, searchDto.Filters);

                static UserReservationDetailsDto mapFunc(DbDataReader reader)
                {
                    return new()
                    {
                        TripId = reader.GetInt32(reader.GetOrdinal("TripId")),
                        DepartureStationId = reader.GetInt32(reader.GetOrdinal("DepartureStationId")),
                        DepartureStationName = reader.GetString(reader.GetOrdinal("DepartureStationName")),
                        DepartureCity = reader.GetString(reader.GetOrdinal("DepartureCity")),
                        DepartureProvince = reader.GetString(reader.GetOrdinal("DepartureProvince")),
                        DepartureCountry = reader.GetString(reader.GetOrdinal("DepartureCountry")),
                        DepartureTime = reader.GetDateTime(reader.GetOrdinal("DepartureTime")),
                        ArrivalStationId = reader.GetInt32(reader.GetOrdinal("ArrivalStationId")),
                        ArrivalStationName = reader.GetString(reader.GetOrdinal("ArrivalStationName")),
                        ArrivalCity = reader.GetString(reader.GetOrdinal("ArrivalCity")),
                        ArrivalProvince = reader.GetString(reader.GetOrdinal("ArrivalProvince")),
                        ArrivalCountry = reader.GetString(reader.GetOrdinal("ArrivalCountry")),
                        ArrivalTime = reader.GetDateTime(reader.GetOrdinal("ArrivalTime")),
                        Seat = reader.GetInt32(reader.GetOrdinal("Seat"))
                    };
                }

                PagedResult<UserReservationDetailsDto> result = await MapPagedResultAsync(cmd, countCmd, mapFunc, page, pageSize, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<PagedResult<ReservationDetailsDto>> SearchWithDetailsAsync(PagedSearchRequestDto searchDto, CancellationToken cancellationToken)
        {
            using DbConnection conn = await SqlCommandHelper.CreateAndOpenConnectionAsync(connectionFactory, cancellationToken);
            using DbTransaction transaction = await SqlCommandHelper.CreateTransactionAsync(conn, cancellationToken);
            try
            {
                using DbCommand cmd = SqlCommandHelper.CreateCommand(conn, transaction);

                string filterClause = SqlCommandHelper.BuildFiltersClause<ReservationDetailsDto>(searchDto.Filters);
                string orderByClause = SqlCommandHelper.BuildOrderByClause<ReservationDetailsDto>(searchDto.Orders);
                bool hasFilter = !string.IsNullOrEmpty(filterClause);
                bool hasOrder = !string.IsNullOrEmpty(orderByClause);
                bool hasFilterOrOrder = hasFilter || hasOrder;
                int page = searchDto.Page > 0 ? searchDto.Page : 1;
                int pageSize = searchDto.PageSize > 0 ? searchDto.PageSize : 10;
                int offset = (page - 1) * pageSize;
                StringBuilder sql = new();
                if (hasFilterOrOrder)
                {
                    _ = sql.Append("SELECT * FROM (");
                }
                _ = sql.Append($"SELECT r.\"TripId\", t.\"DepartureStationId\", dst.\"StationName\" AS \"DepartureStationName\", dst.\"City\" AS \"DepartureCity\", dst.\"Province\" AS \"DepartureProvince\", dst.\"Country\" AS \"DepartureCountry\", t.\"DepartureTime\", t.\"ArrivalStationId\", ast.\"StationName\" AS \"ArrivalStationName\", ast.\"City\" AS \"ArrivalCity\", ast.\"Province\" AS \"ArrivalProvince\", ast.\"Country\" AS \"ArrivalCountry\", t.\"ArrivalTime\", u.\"UserId\", u.\"UserName\", u.\"Email\", r.\"Seat\" " +
                $"FROM grsdb.\"{_tableName}\" r " +
                $"JOIN grsdb.\"ApplicationUser\" u ON r.\"UserId\" = u.\"UserId\" " +
                $"JOIN grsdb.\"Trip\" t ON r.\"TripId\" = t.\"TripId\" " +
                $"JOIN grsdb.\"Station\" dst ON t.\"DepartureStationId\" = dst.\"StationId\" " +
                $"JOIN grsdb.\"Station\" ast ON t.\"ArrivalStationId\" = ast.\"StationId\"");
                if (hasFilterOrOrder)
                {
                    _ = sql.Append(") subquery");
                    if (hasFilter)
                    {
                        _ = sql.Append($" WHERE {filterClause}");
                    }
                    if (hasOrder)
                    {
                        _ = sql.Append($" ORDER BY {orderByClause}");
                    }
                }
                _ = sql.Append($" LIMIT {pageSize} OFFSET {offset}");
                cmd.CommandText = sql.ToString();
                AddFilterParameters<ReservationDetailsDto>(cmd, searchDto.Filters);

                using DbCommand countCmd = SqlCommandHelper.CreateCommand(conn, transaction);
                StringBuilder countSql = new();
                _ = countSql.Append("SELECT COUNT(*) FROM (");
                if (hasFilter)
                {
                    _ = countSql.Append("SELECT * FROM (");
                }
                _ = countSql.Append($"SELECT r.\"TripId\", t.\"DepartureStationId\", dst.\"StationName\" AS \"DepartureStationName\", dst.\"City\" AS \"ArrivalCity\", dst.\"Province\" AS \"ArrivalProvince\", dst.\"Country\" AS \"ArrivalCountry\", t.\"DepartureTime\", t.\"ArrivalStationId\", ast.\"StationName\" AS \"ArrivalStationName\", ast.\"City\" AS \"ArrivalCity\", ast.\"Province\" AS \"ArrivalProvince\", ast.\"Country\" AS \"ArrivalCountry\", t.\"ArrivalTime\", u.\"UserId\", u.\"UserName\", u.\"Email\", r.\"Seat\" FROM grsdb.\"{_tableName}\" r JOIN grsdb.\"ApplicationUser\" u ON r.\"UserId\" = u.\"UserId\" JOIN grsdb.\"Trip\" t ON r.\"TripId\" = t.\"TripId\" JOIN grsdb.\"Station\" dst ON t.\"DepartureStationId\" = dst.\"StationId\" JOIN grsdb.\"Station\" ast ON t.\"ArrivalStationId\" = ast.\"StationId\"");
                if (hasFilter)
                {
                    _ = countSql.Append($") WHERE {filterClause}");
                }
                _ = countSql.Append(")");
                countCmd.CommandText = countSql.ToString();
                AddFilterParameters<ReservationDetailsDto>(countCmd, searchDto.Filters);

                static ReservationDetailsDto mapFunc(DbDataReader reader)
                {
                    return new()
                    {
                        TripId = reader.GetInt32(reader.GetOrdinal("TripId")),
                        DepartureStationId = reader.GetInt32(reader.GetOrdinal("DepartureStationId")),
                        DepartureStationName = reader.GetString(reader.GetOrdinal("DepartureStationName")),
                        DepartureCity = reader.GetString(reader.GetOrdinal("DepartureCity")),
                        DepartureProvince = reader.GetString(reader.GetOrdinal("DepartureProvince")),
                        DepartureCountry = reader.GetString(reader.GetOrdinal("DepartureCountry")),
                        DepartureTime = reader.GetDateTime(reader.GetOrdinal("DepartureTime")),
                        ArrivalStationId = reader.GetInt32(reader.GetOrdinal("ArrivalStationId")),
                        ArrivalStationName = reader.GetString(reader.GetOrdinal("ArrivalStationName")),
                        ArrivalCity = reader.GetString(reader.GetOrdinal("ArrivalCity")),
                        ArrivalProvince = reader.GetString(reader.GetOrdinal("ArrivalProvince")),
                        ArrivalCountry = reader.GetString(reader.GetOrdinal("ArrivalCountry")),
                        ArrivalTime = reader.GetDateTime(reader.GetOrdinal("ArrivalTime")),
                        UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                        UserName = reader.GetString(reader.GetOrdinal("UserName")),
                        Email = reader.GetString(reader.GetOrdinal("Email")),
                        Seat = reader.GetInt32(reader.GetOrdinal("Seat"))
                    };
                }

                PagedResult<ReservationDetailsDto> result = await MapPagedResultAsync(cmd, countCmd, mapFunc, page, pageSize, cancellationToken);
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
