using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using GeneralReservationSystem.Infrastructure.Helpers;
using System.Data.Common;
using System.Text;

namespace GeneralReservationSystem.Infrastructure.Repositories.Sql
{
    public class TripRepository(Func<DbConnection> connectionFactory) : Repository<Trip>(connectionFactory), ITripRepository
    {
        private readonly Func<DbConnection> connectionFactory = connectionFactory;

        public async Task<Trip?> GetByIdAsync(int tripId, CancellationToken cancellationToken)
        {
            using DbConnection conn = await SqlCommandHelper.CreateAndOpenConnectionAsync(connectionFactory, cancellationToken);
            using DbCommand cmd = SqlCommandHelper.CreateCommand(conn);
            cmd.CommandText = $"SELECT * FROM \"{_tableName}\" WHERE \"TripId\" = @id";
            SqlCommandHelper.AddParameter(cmd, "@id", tripId, typeof(int));
            using DbDataReader reader = await SqlCommandHelper.ExecuteReaderAsync(cmd, cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? DataReaderMapper.MapReaderToEntity<Trip>(reader, _properties) : null;
        }

        public async Task<IEnumerable<int>> GetFreeSeatsAsync(int tripId, CancellationToken cancellationToken)
        {
            using DbConnection conn = await SqlCommandHelper.CreateAndOpenConnectionAsync(connectionFactory, cancellationToken);
            using DbCommand cmd = SqlCommandHelper.CreateCommand(conn);
            cmd.CommandText =
                $"SELECT seat FROM (" +
                $"SELECT generate_series(1, t.\"AvailableSeats\") AS seat " +
                $"FROM \"{_tableName}\" t " +
                $"WHERE t.\"TripId\" = @id" +
                ") s " +
                $"WHERE seat NOT IN (" +
                $"SELECT r.\"Seat\" FROM \"Reservation\" r WHERE r.\"TripId\" = @id" +
                ")";
            SqlCommandHelper.AddParameter(cmd, "@id", tripId, typeof(int));
            List<int> seats = [];
            using (DbDataReader reader = await SqlCommandHelper.ExecuteReaderAsync(cmd, cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    seats.Add(reader.GetInt32(0));
                }
            }
            return seats;
        }

        public async Task<TripWithDetailsDto?> GetTripWithDetailsAsync(int tripId, CancellationToken cancellationToken)
        {
            using DbConnection conn = await SqlCommandHelper.CreateAndOpenConnectionAsync(connectionFactory, cancellationToken);
            using DbCommand cmd = SqlCommandHelper.CreateCommand(conn);
            cmd.CommandText =
                $"SELECT t.\"TripId\", t.\"DepartureStationId\", dst.\"StationName\" AS \"DepartureStationName\", dst.\"City\" AS \"DepartureCity\", dst.\"Province\" AS \"DepartureProvince\", dst.\"Country\" AS \"DepartureCountry\", t.\"DepartureTime\", t.\"ArrivalStationId\", ast.\"StationName\" AS \"ArrivalStationName\", ast.\"City\" AS \"ArrivalCity\", ast.\"Province\" AS \"ArrivalProvince\", ast.\"Country\" AS \"ArrivalCountry\", t.\"ArrivalTime\", t.\"AvailableSeats\", COALESCE(r.reserved, 0) AS \"ReservedSeats\" " +
                $"FROM \"{_tableName}\" t " +
                $"JOIN \"Station\" dst ON t.\"DepartureStationId\" = dst.\"StationId\" " +
                $"JOIN \"Station\" ast ON t.\"ArrivalStationId\" = ast.\"StationId\" " +
                $"LEFT JOIN (" +
                $"SELECT \"TripId\", COUNT(*) AS reserved FROM \"Reservation\" GROUP BY \"TripId\"" +
                ") r ON t.\"TripId\" = r.\"TripId\" " +
                $"WHERE t.\"TripId\" = @id";
            SqlCommandHelper.AddParameter(cmd, "@id", tripId, typeof(int));
            using DbDataReader reader = await SqlCommandHelper.ExecuteReaderAsync(cmd, cancellationToken);
            return await reader.ReadAsync(cancellationToken)
                ? new TripWithDetailsDto
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
                    AvailableSeats = reader.GetInt32(reader.GetOrdinal("AvailableSeats")),
                    ReservedSeats = reader.GetInt32(reader.GetOrdinal("ReservedSeats"))
                }
                : null;
        }

        public async Task<PagedResult<TripWithDetailsDto>> SearchWithDetailsAsync(PagedSearchRequestDto searchDto, CancellationToken cancellationToken)
        {
            using DbConnection conn = await SqlCommandHelper.CreateAndOpenConnectionAsync(connectionFactory, cancellationToken);
            using DbTransaction transaction = await SqlCommandHelper.CreateTransactionAsync(conn, cancellationToken);
            try
            {
                using DbCommand cmd = SqlCommandHelper.CreateCommand(conn, transaction);

                string filterClause = SqlCommandHelper.BuildFiltersClause<Trip>(searchDto.Filters);
                string whereClause = string.IsNullOrEmpty(filterClause) ? "" : $"WHERE {filterClause}";
                int page = searchDto.Page > 0 ? searchDto.Page : 1;
                int pageSize = searchDto.PageSize > 0 ? searchDto.PageSize : 10;
                int offset = (page - 1) * pageSize;
                StringBuilder sql = new();
                _ = sql.Append($"SELECT t.\"TripId\", t.\"DepartureStationId\", dst.\"StationName\" AS \"DepartureStationName\", dst.\"City\" AS \"DepartureCity\", dst.\"Province\" AS \"DepartureProvince\", dst.\"Country\" AS \"DepartureCountry\", t.\"DepartureTime\", t.\"ArrivalStationId\", ast.\"StationName\" AS \"ArrivalStationName\", ast.\"City\" AS \"ArrivalCity\", ast.\"Province\" AS \"ArrivalProvince\", ast.\"Country\" AS \"ArrivalCountry\", t.\"ArrivalTime\", t.\"AvailableSeats\", COALESCE(r.reserved, 0) AS \"ReservedSeats\" " +
                    $"FROM \"{_tableName}\" t " +
                    $"JOIN \"Station\" dst ON t.\"DepartureStationId\" = dst.\"StationId\" " +
                    $"JOIN \"Station\" ast ON t.\"ArrivalStationId\" = ast.\"StationId\" " +
                    $"LEFT JOIN (" +
                    $"SELECT \"TripId\", COUNT(*) AS reserved FROM \"Reservation\" GROUP BY \"TripId\"" +
                    ") r ON t.\"TripId\" = r.\"TripId\" " +
                    whereClause);
                if (searchDto.Orders != null && searchDto.Orders.Count > 0)
                {
                    string orderByClause = SqlCommandHelper.BuildOrderByClause<Trip>(searchDto.Orders);
                    if (!string.IsNullOrEmpty(orderByClause))
                    {
                        _ = sql.Append($" ORDER BY {orderByClause}");
                    }
                }
                _ = sql.Append($" LIMIT {pageSize} OFFSET {offset}");
                cmd.CommandText = sql.ToString();
                AddFilterParameters(cmd, searchDto.Filters);

                using DbCommand countCmd = SqlCommandHelper.CreateCommand(conn, transaction);
                countCmd.CommandText = $"SELECT COUNT(*) FROM \"{_tableName}\" t {whereClause}";
                AddFilterParameters(countCmd, searchDto.Filters);

                static TripWithDetailsDto mapFunc(DbDataReader reader)
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
                        AvailableSeats = reader.GetInt32(reader.GetOrdinal("AvailableSeats")),
                        ReservedSeats = reader.GetInt32(reader.GetOrdinal("ReservedSeats"))
                    };
                }

                PagedResult<TripWithDetailsDto> result = await MapPagedResultAsync(cmd, countCmd, mapFunc, page, pageSize, cancellationToken);
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
