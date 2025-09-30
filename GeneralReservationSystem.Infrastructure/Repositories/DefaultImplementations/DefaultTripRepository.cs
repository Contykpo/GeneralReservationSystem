using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using static GeneralReservationSystem.Application.Common.OperationResult;

namespace GeneralReservationSystem.Infrastructure.Repositories.DefaultImplementations
{
    public class DefaultTripRepository : ITripRepository
    {
        private readonly DbConnectionHelper _dbConnection;
        private readonly ILogger<DefaultTripRepository> _logger;

        public DefaultTripRepository(DbConnectionHelper dbConnection, ILogger<DefaultTripRepository> logger)
        {
            _dbConnection = dbConnection;
            _logger = logger;
        }

        public async Task<OptionalResult<Trip>> GetByIdAsync(int id)
        {
            return await _dbConnection.ExecuteReaderSingleAsync<Trip>(
                sql: "SELECT * FROM Trip WHERE TripId = @TripId;",
                converter: reader => new Trip
                {
                    TripId = reader.GetInt32(reader.GetOrdinal("TripId")),
                    VehicleId = reader.GetInt32(reader.GetOrdinal("VehicleId")),
                    DepartureId = reader.GetInt32(reader.GetOrdinal("DepartureId")),
                    DestinationId = reader.GetInt32(reader.GetOrdinal("DestinationId")),
                    DriverId = reader.GetInt32(reader.GetOrdinal("DriverId")),
                    DepartureTime = reader.GetDateTime(reader.GetOrdinal("DepartureTime")),
                    ArrivalTime = reader.GetDateTime(reader.GetOrdinal("ArrivalTime"))
                },
                parameters: new Dictionary<string, object> { { "@TripId", id } }
            );
        }

        public async Task<OptionalResult<IList<TripDetailsDto>>> SearchPagedAsync(int pageIndex, int pageSize, string? DepartureName = null, string? DepartureCity = null, string? destinationName = null, string? destinationCity = null, DateTime? startDate = null, DateTime? endDate = null, bool onlyWithAvailableSeat = true, TripSearchSortBy? sortBy = null, bool descending = false)
        {
            // TODO: This 1=1 trick is a bit hacky, consider using a proper management of optional filters.
            var sql = "SELECT * FROM TripDetailsView WHERE 1=1";
            var parameters = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(DepartureName)) { sql += " AND DepartureName LIKE @DepartureName"; parameters.Add("@DepartureName", $"%{DepartureName}%"); }
            if (!string.IsNullOrEmpty(DepartureCity)) { sql += " AND DepartureCity LIKE @DepartureCity"; parameters.Add("@DepartureCity", $"%{DepartureCity}%"); }
            if (!string.IsNullOrEmpty(destinationName)) { sql += " AND DestinationName LIKE @DestinationName"; parameters.Add("@DestinationName", $"%{destinationName}%"); }
            if (!string.IsNullOrEmpty(destinationCity)) { sql += " AND DestinationCity LIKE @DestinationCity"; parameters.Add("@DestinationCity", $"%{destinationCity}%"); }
            if (startDate.HasValue) { sql += " AND DepartureTime >= @StartDate"; parameters.Add("@StartDate", startDate.Value); }
            if (endDate.HasValue) { sql += " AND ArrivalTime <= @EndDate"; parameters.Add("@EndDate", endDate.Value); }
            if (sortBy.HasValue)
            {
                sql += $" ORDER BY {sortBy.Value}{(descending ? " DESC" : " ASC")}";
            }
            else
            {
                sql += " ORDER BY TripId ASC";
            }
            sql += " OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            parameters.Add("@Offset", pageIndex * pageSize);
            parameters.Add("@PageSize", pageSize);
            return await _dbConnection.ExecuteReaderAsync<TripDetailsDto>(
                sql: sql,
                converter: reader => new TripDetailsDto
                {
                    TripId = reader.GetInt32(reader.GetOrdinal("TripId")),
                    VehicleId = reader.GetInt32(reader.GetOrdinal("VehicleId")),
                    DepartureId = reader.GetInt32(reader.GetOrdinal("DepartureId")),
                    DestinationId = reader.GetInt32(reader.GetOrdinal("DestinationId")),
                    DriverId = reader.GetInt32(reader.GetOrdinal("DriverId")),
                    DepartureTime = reader.GetDateTime(reader.GetOrdinal("DepartureTime")),
                    ArrivalTime = reader.GetDateTime(reader.GetOrdinal("ArrivalTime")),
                    DepartureName = reader.GetString(reader.GetOrdinal("DepartureName")),
                    DepartureCity = reader.GetString(reader.GetOrdinal("DepartureCity")),
                    DepartureRegion = reader.GetString(reader.GetOrdinal("DepartureRegion")),
                    DepartureCountry = reader.GetString(reader.GetOrdinal("DepartureCountry")),
                    DepartureTimeZone = TimeZoneInfo.FindSystemTimeZoneById(reader.GetString(reader.GetOrdinal("DepartureTimeZone"))),
                    DestinationName = reader.GetString(reader.GetOrdinal("DestinationName")),
                    DestinationCity = reader.GetString(reader.GetOrdinal("DestinationCity")),
                    DestinationRegion = reader.GetString(reader.GetOrdinal("DestinationRegion")),
                    DestinationCountry = reader.GetString(reader.GetOrdinal("DestinationCountry")),
                    DestinationTimeZone = TimeZoneInfo.FindSystemTimeZoneById(reader.GetString(reader.GetOrdinal("DestinationTimeZone"))),
                    TotalSeats = reader.GetInt32(reader.GetOrdinal("TotalSeats")),
                    AvailableSeats = reader.GetInt32(reader.GetOrdinal("AvailableSeats"))
                },
                parameters: parameters
            );
        }

        public async Task<OperationResult> AddAsync(Trip trip)
        {
            return (await _dbConnection.ExecuteAsync(
                sql: "INSERT INTO Trip (VehicleId, DepartureId, DestinationId, DriverId, DepartureTime, ArrivalTime) VALUES (@VehicleId, @DepartureId, @DestinationId, @DriverId, @DepartureTime, @ArrivalTime);",
                parameters: new Dictionary<string, object>
                {
                    { "@VehicleId", trip.VehicleId },
                    { "@DepartureId", trip.DepartureId },
                    { "@DestinationId", trip.DestinationId },
                    { "@DriverId", trip.DriverId },
                    { "@DepartureTime", trip.DepartureTime },
                    { "@ArrivalTime", trip.ArrivalTime }
                }
            )).Match<OperationResult>(
                onValue: rowsAffected => rowsAffected > 0 ? Success() : Failure("No changes were made"),
                onError: error => Failure(error)
            );
        }

        public async Task<OperationResult> UpdateAsync(Trip trip)
        {
            return (await _dbConnection.ExecuteAsync(
                sql: "UPDATE Trip SET VehicleId = @VehicleId, DepartureId = @DepartureId, DestinationId = @DestinationId, DriverId = @DriverId, DepartureTime = @DepartureTime, ArrivalTime = @ArrivalTime WHERE TripId = @TripId;",
                parameters: new Dictionary<string, object>
                {
                    { "@TripId", trip.TripId },
                    { "@VehicleId", trip.VehicleId },
                    { "@DepartureId", trip.DepartureId },
                    { "@DestinationId", trip.DestinationId },
                    { "@DriverId", trip.DriverId },
                    { "@DepartureTime", trip.DepartureTime },
                    { "@ArrivalTime", trip.ArrivalTime }
                }
            )).Match<OperationResult>(
                onValue: rowsAffected => rowsAffected > 0 ? Success() : Failure("No changes were made"),
                onError: error => Failure(error)
            );
        }

        public async Task<OperationResult> DeleteAsync(int id)
        {
            return (await _dbConnection.ExecuteAsync(
                sql: "DELETE FROM Trip WHERE TripId = @TripId;",
                parameters: new Dictionary<string, object> { { "@TripId", id } }
            )).Match<OperationResult>(
                onValue: rowsAffected => rowsAffected > 0 ? Success() : Failure("No entries were deleted"),
                onError: error => Failure(error)
            );
        }

        public async Task<OptionalResult<Driver>> GetDriverByIdAsync(int id)
        {
            return await _dbConnection.ExecuteReaderSingleAsync<Driver>(
                sql: "SELECT * FROM Driver WHERE DriverId = @DriverId;",
                converter: reader => new Driver
                {
                    DriverId = reader.GetInt32(reader.GetOrdinal("DriverId")),
                    FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                    LastName = reader.GetString(reader.GetOrdinal("LastName")),
                    LicenseNumber = reader.GetString(reader.GetOrdinal("LicenseNumber")),
                    IdentificationNumber = reader.GetInt32(reader.GetOrdinal("IdentificationNumber")),
                    LicenseExpiryDate = reader.GetDateTime(reader.GetOrdinal("LicenseExpiryDate"))
                },
                parameters: new Dictionary<string, object> { { "@DriverId", id } }
            );
        }

        public async Task<OptionalResult<Vehicle>> GetVehicleByIdAsync(int id)
        {
            return await _dbConnection.ExecuteReaderSingleAsync<Vehicle>(
                sql: "SELECT * FROM Vehicle WHERE VehicleId = @VehicleId;",
                converter: reader => new Vehicle
                {
                    VehicleId = reader.GetInt32(reader.GetOrdinal("VehicleId")),
                    VehicleModelId = reader.GetInt32(reader.GetOrdinal("VehicleModelId")),
                    LicensePlate = reader.GetString(reader.GetOrdinal("LicensePlate")),
                    Status = reader.GetString(reader.GetOrdinal("Status"))
                },
                parameters: new Dictionary<string, object> { { "@VehicleId", id } }
            );
        }
    }
}
