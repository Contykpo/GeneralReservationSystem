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
            cmd.CommandText = $"SELECT * FROM \"{_tableName}\" WHERE \"TripId\" = @tripId AND \"Seat\" = @seat";
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
                $"FROM \"{_tableName}\" r " +
                $"JOIN \"Trip\" t ON r.\"TripId\" = t.\"TripId\" " +
                $"JOIN \"Station\" dst ON t.\"DepartureStationId\" = dst.\"StationId\" " +
                $"JOIN \"Station\" ast ON t.\"ArrivalStationId\" = ast.\"StationId\" " +
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

                string filterClause = SqlCommandHelper.BuildFiltersClause<Reservation>(searchDto.Filters);
                string whereClause = string.IsNullOrEmpty(filterClause) ? $"WHERE r.\"UserId\" = @userId" : $"WHERE r.\"UserId\" = @userId AND {filterClause}";
                int page = searchDto.Page > 0 ? searchDto.Page : 1;
                int pageSize = searchDto.PageSize > 0 ? searchDto.PageSize : 10;
                int offset = (page - 1) * pageSize;
                StringBuilder sql = new();
                _ = sql.Append($"SELECT r.\"TripId\", t.\"DepartureStationId\", dst.\"StationName\" AS \"DepartureStationName\", dst.\"City\" AS \"DepartureCity\", dst.\"Province\" AS \"DepartureProvince\", dst.\"Country\" AS \"DepartureCountry\", t.\"DepartureTime\", t.\"ArrivalStationId\", ast.\"StationName\" AS \"ArrivalStationName\", ast.\"City\" AS \"ArrivalCity\", ast.\"Province\" AS \"ArrivalProvince\", ast.\"Country\" AS \"ArrivalCountry\", t.\"ArrivalTime\", r.\"Seat\" " +
                    $"FROM \"{_tableName}\" r " +
                    $"JOIN \"Trip\" t ON r.\"TripId\" = t.\"TripId\" " +
                    $"JOIN \"Station\" dst ON t.\"DepartureStationId\" = dst.\"StationId\" " +
                    $"JOIN \"Station\" ast ON t.\"ArrivalStationId\" = ast.\"StationId\" " +
                    whereClause);
                if (searchDto.Orders != null && searchDto.Orders.Count > 0)
                {
                    string orderByClause = SqlCommandHelper.BuildOrderByClause<Reservation>(searchDto.Orders);
                    if (!string.IsNullOrEmpty(orderByClause))
                    {
                        _ = sql.Append($" ORDER BY {orderByClause}");
                    }
                }
                _ = sql.Append($" LIMIT {pageSize} OFFSET {offset}");
                cmd.CommandText = sql.ToString();
                SqlCommandHelper.AddParameter(cmd, "@userId", userId, typeof(int));
                AddFilterParameters(cmd, searchDto.Filters);

                using DbCommand countCmd = SqlCommandHelper.CreateCommand(conn, transaction);
                countCmd.CommandText = $"SELECT COUNT(*) FROM \"{_tableName}\" r JOIN \"Trip\" t ON r.\"TripId\" = t.\"TripId\" JOIN \"Station\" dst ON t.\"DepartureStationId\" = dst.\"StationId\" JOIN \"Station\" ast ON t.\"ArrivalStationId\" = ast.\"StationId\" {whereClause}";
                SqlCommandHelper.AddParameter(countCmd, "@userId", userId, typeof(int));
                AddFilterParameters(countCmd, searchDto.Filters);

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

                string filterClause = SqlCommandHelper.BuildFiltersClause<Reservation>(searchDto.Filters);
                string whereClause = string.IsNullOrEmpty(filterClause) ? "" : $"WHERE {filterClause}";
                int page = searchDto.Page > 0 ? searchDto.Page : 1;
                int pageSize = searchDto.PageSize > 0 ? searchDto.PageSize : 10;
                int offset = (page - 1) * pageSize;
                StringBuilder sql = new();
                _ = sql.Append($"SELECT r.\"TripId\", t.\"DepartureStationId\", dst.\"StationName\" AS \"DepartureStationName\", dst.\"City\" AS \"DepartureCity\", dst.\"Province\" AS \"DepartureProvince\", dst.\"Country\" AS \"DepartureCountry\", t.\"DepartureTime\", t.\"ArrivalStationId\", ast.\"StationName\" AS \"ArrivalStationName\", ast.\"City\" AS \"ArrivalCity\", ast.\"Province\" AS \"ArrivalProvince\", ast.\"Country\" AS \"ArrivalCountry\", t.\"ArrivalTime\", u.\"UserId\", u.\"UserName\", u.\"Email\", r.\"Seat\" " +
                    $"FROM \"{_tableName}\" r " +
                    $"JOIN \"ApplicationUser\" u ON r.\"UserId\" = u.\"UserId\" " +
                    $"JOIN \"Trip\" t ON r.\"TripId\" = t.\"TripId\" " +
                    $"JOIN \"Station\" dst ON t.\"DepartureStationId\" = dst.\"StationId\" " +
                    $"JOIN \"Station\" ast ON t.\"ArrivalStationId\" = ast.\"StationId\" " +
                    whereClause);
                if (searchDto.Orders != null && searchDto.Orders.Count > 0)
                {
                    string orderByClause = SqlCommandHelper.BuildOrderByClause<Reservation>(searchDto.Orders);
                    if (!string.IsNullOrEmpty(orderByClause))
                    {
                        _ = sql.Append($" ORDER BY {orderByClause}");
                    }
                }
                _ = sql.Append($" LIMIT {pageSize} OFFSET {offset}");
                cmd.CommandText = sql.ToString();
                AddFilterParameters(cmd, searchDto.Filters);

                using DbCommand countCmd = SqlCommandHelper.CreateCommand(conn, transaction);
                countCmd.CommandText = $"SELECT COUNT(*) FROM \"{_tableName}\" r JOIN \"ApplicationUser\" u ON r.\"UserId\" = u.\"UserId\" JOIN \"Trip\" t ON r.\"TripId\" = t.\"TripId\" JOIN \"Station\" dst ON t.\"DepartureStationId\" = dst.\"StationId\" JOIN \"Station\" ast ON t.\"ArrivalStationId\" = ast.\"StationId\" {whereClause}";
                AddFilterParameters(countCmd, searchDto.Filters);

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
