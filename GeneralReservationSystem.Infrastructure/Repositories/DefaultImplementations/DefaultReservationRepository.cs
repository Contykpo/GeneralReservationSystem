using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities.Authentication;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static GeneralReservationSystem.Application.Common.OperationResult;
using static GeneralReservationSystem.Application.Common.OptionalResult<object>;

namespace GeneralReservationSystem.Infrastructure.Repositories.DefaultImplementations
{
    public class DefaultReservationRepository : IReservationRepository
    {
        private readonly DbConnectionHelper _dbConnection;
        private readonly ILogger<DefaultReservationRepository> _logger;

        public DefaultReservationRepository(DbConnectionHelper dbConnection, ILogger<DefaultReservationRepository> logger)
        {
            _dbConnection = dbConnection;
            _logger = logger;
        }

        public async Task<OptionalResult<IList<AvailableSeatDto>>> GetAvailablePagedAsync(int pageIndex, int pageSize, int tripId)
        {
            var sql = "SELECT * FROM TripAvailableSeats WHERE TripId = @TripId ORDER BY SeatId OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            var parameters = new Dictionary<string, object>
            {
                { "@TripId", tripId },
                { "@Offset", pageIndex * pageSize },
                { "@PageSize", pageSize }
            };
            return await _dbConnection.ExecuteReaderAsync<AvailableSeatDto>(
                sql: sql,
                converter: reader => new AvailableSeatDto
                {
                    TripId = reader.GetInt32(reader.GetOrdinal("TripId")),
                    SeatId = reader.GetInt32(reader.GetOrdinal("SeatId")),
                    SeatRow = reader.GetInt32(reader.GetOrdinal("SeatRow")),
                    SeatColumn = reader.GetInt32(reader.GetOrdinal("SeatColumn")),
                    IsAtWindow = reader.GetBoolean(reader.GetOrdinal("IsAtWindow")),
                    IsAtAisle = reader.GetBoolean(reader.GetOrdinal("IsAtAisle")),
                    IsInFront = reader.GetBoolean(reader.GetOrdinal("IsInFront")),
                    IsInBack = reader.GetBoolean(reader.GetOrdinal("IsInBack")),
                    IsAccessible = reader.GetBoolean(reader.GetOrdinal("IsAccessible")),
                    VehicleModelId = reader.GetInt32(reader.GetOrdinal("VehicleModelId"))
                },
                parameters: parameters
            );
        }

        public async Task<OptionalResult<IList<SeatReservationDto>>> GetReservedSeatsForTripPagedAsync(int pageIndex, int pageSize, int tripId)
        {
            var sql = "SELECT * FROM Reservation WHERE TripId = @TripId OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            var parameters = new Dictionary<string, object>
            {
                { "@TripId", tripId },
                { "@Offset", pageIndex * pageSize },
                { "@PageSize", pageSize }
            };
            return await _dbConnection.ExecuteReaderAsync<SeatReservationDto>(
                sql: sql,
                converter: reader => new SeatReservationDto
                {
                    ReservationId = reader.GetInt32(reader.GetOrdinal("ReservationId")),
                    TripId = reader.GetInt32(reader.GetOrdinal("TripId")),
                    SeatId = reader.GetInt32(reader.GetOrdinal("SeatId")),
                    UserId = reader.GetGuid(reader.GetOrdinal("UserId")),
                    SeatRow = reader.GetInt32(reader.GetOrdinal("SeatRow")),
                    SeatColumn = reader.GetInt32(reader.GetOrdinal("SeatColumn")),
                    IsAtWindow = reader.GetBoolean(reader.GetOrdinal("IsAtWindow")),
                    IsAtAisle = reader.GetBoolean(reader.GetOrdinal("IsAtAisle")),
                    IsInFront = reader.GetBoolean(reader.GetOrdinal("IsInFront")),
                    IsInBack = reader.GetBoolean(reader.GetOrdinal("IsInBack")),
                    IsAccessible = reader.GetBoolean(reader.GetOrdinal("IsAccessible")),
                    ReservedAt = reader.GetDateTime(reader.GetOrdinal("ReservedAt"))
                },
                parameters: parameters
            );
        }

        public async Task<OptionalResult<IList<SeatReservationDto>>> GetReservedSeatsForUserPagedAsync(int pageIndex, int pageSize, Guid userId, int? tripId)
        {
            var sql = "SELECT * FROM Reservation WHERE UserId = @UserId";
            var parameters = new Dictionary<string, object>
            {
                { "@UserId", userId },
                { "@Offset", pageIndex * pageSize },
                { "@PageSize", pageSize }
            };
            if (tripId.HasValue) { sql += " AND TripId = @TripId"; parameters.Add("@TripId", tripId.Value); }
            sql += " OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            return await _dbConnection.ExecuteReaderAsync<SeatReservationDto>(
                sql: sql,
                converter: reader => new SeatReservationDto
                {
                    ReservationId = reader.GetInt32(reader.GetOrdinal("ReservationId")),
                    TripId = reader.GetInt32(reader.GetOrdinal("TripId")),
                    SeatId = reader.GetInt32(reader.GetOrdinal("SeatId")),
                    UserId = reader.GetGuid(reader.GetOrdinal("UserId")),
                    SeatRow = reader.GetInt32(reader.GetOrdinal("SeatRow")),
                    SeatColumn = reader.GetInt32(reader.GetOrdinal("SeatColumn")),
                    IsAtWindow = reader.GetBoolean(reader.GetOrdinal("IsAtWindow")),
                    IsAtAisle = reader.GetBoolean(reader.GetOrdinal("IsAtAisle")),
                    IsInFront = reader.GetBoolean(reader.GetOrdinal("IsInFront")),
                    IsInBack = reader.GetBoolean(reader.GetOrdinal("IsInBack")),
                    IsAccessible = reader.GetBoolean(reader.GetOrdinal("IsAccessible")),
                    ReservedAt = reader.GetDateTime(reader.GetOrdinal("ReservedAt"))
                },
                parameters: parameters
            );
        }

        public async Task<OperationResult> AddAsync(Reservation reservation)
        {
            return (await _dbConnection.ExecuteAsync(
                sql: "INSERT INTO Reservations (SeatId, TripId, UserId, ReservedAt) VALUES (@SeatId, @TripId, @UserId, @ReservedAt);",
                parameters: new Dictionary<string, object>
                {
                    { "@SeatId", reservation.SeatId },
                    { "@TripId", reservation.TripId },
                    { "@UserId", reservation.UserId },
                    { "@ReservedAt", DateTime.UtcNow }
                }
            )).Match<OperationResult>(
                onValue: rowsAffected => rowsAffected > 0 ? Success() : Failure("No se realizaron cambios"),
                onError: error => Failure(error)
            );
        }

        public async Task<OperationResult> DeleteAsync(Reservation reservation)
        {
            return (await _dbConnection.ExecuteAsync(
                sql: "DELETE FROM Reservations WHERE SeatId = @SeatId AND TripId = @TripId AND UserId = @UserId;",
                parameters: new Dictionary<string, object>
                {
                    { "@SeatId", reservation.SeatId },
                    { "@TripId", reservation.TripId },
                    { "@UserId", reservation.UserId }
                }
            )).Match<OperationResult>(
                onValue: rowsAffected => rowsAffected > 0 ? Success() : Failure("No se eliminaron entradas"),
                onError: error => Failure(error)
            );
        }
    }
}
