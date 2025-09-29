using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using static GeneralReservationSystem.Application.Common.OperationResult;

namespace GeneralReservationSystem.Infrastructure.Repositories.DefaultImplementations
{
    public class DefaultSeatRepository : ISeatRepository
    {
        private readonly DbConnectionHelper _dbConnection;
        private readonly ILogger<DefaultSeatRepository> _logger;

        public DefaultSeatRepository(DbConnectionHelper dbConnection, ILogger<DefaultSeatRepository> logger)
        {
            _dbConnection = dbConnection;
            _logger = logger;
        }

        public async Task<OptionalResult<Seat>> GetByIdAsync(int id)
        {
            return await _dbConnection.ExecuteReaderSingleAsync<Seat>(
                sql: "SELECT * FROM Seats WHERE SeatId = @SeatId;",
                converter: reader => new Seat
                {
                    SeatId = reader.GetInt32(reader.GetOrdinal("SeatId")),
                    VehicleModelId = reader.GetInt32(reader.GetOrdinal("VehicleModelId")),
                    Row = reader.GetInt32(reader.GetOrdinal("Row")),
                    Column = reader.GetInt32(reader.GetOrdinal("Column")),
                    IsAtWindow = reader.GetBoolean(reader.GetOrdinal("IsAtWindow")),
                    IsAtAisle = reader.GetBoolean(reader.GetOrdinal("IsAtAisle")),
                    IsInFront = reader.GetBoolean(reader.GetOrdinal("IsInFront")),
                    IsInBack = reader.GetBoolean(reader.GetOrdinal("IsInBack")),
                    IsAccessible = reader.GetBoolean(reader.GetOrdinal("IsAccessible"))
                },
                parameters: new Dictionary<string, object> { { "@SeatId", id } }
            );
        }

        public async Task<OperationResult> AddAsync(Seat seat)
        {
            return (await _dbConnection.ExecuteAsync(
                sql: "INSERT INTO Seats (VehicleModelId, Row, Column, IsAtWindow, IsAtAisle, IsInFront, IsInBack, IsAccessible) VALUES (@VehicleModelId, @Row, @Column, @IsAtWindow, @IsAtAisle, @IsInFront, @IsInBack, @IsAccessible);",
                parameters: new Dictionary<string, object>
                {
                    { "@VehicleModelId", seat.VehicleModelId },
                    { "@Row", seat.Row },
                    { "@Column", seat.Column },
                    { "@IsAtWindow", seat.IsAtWindow },
                    { "@IsAtAisle", seat.IsAtAisle },
                    { "@IsInFront", seat.IsInFront },
                    { "@IsInBack", seat.IsInBack },
                    { "@IsAccessible", seat.IsAccessible }
                }
            )).Match<OperationResult>(
                onValue: rowsAffected => rowsAffected > 0 ? Success() : Failure("No changes were made"),
                onError: error => Failure(error)
            );
        }

        public async Task<OperationResult> AddMultipleAsync(IEnumerable<Seat> seats)
        {
            int totalAffected = 0;
            foreach (var seat in seats)
            {
                var result = await AddAsync(seat);
                if (result is Success) totalAffected++;
            }
            return totalAffected > 0 ? Success() : Failure("No seats were added");
        }

        public async Task<OperationResult> UpdateAsync(Seat seat)
        {
            return (await _dbConnection.ExecuteAsync(
                sql: "UPDATE Seats SET VehicleModelId = @VehicleModelId, Row = @Row, Column = @Column, IsAtWindow = @IsAtWindow, IsAtAisle = @IsAtAisle, IsInFront = @IsInFront, IsInBack = @IsInBack, IsAccessible = @IsAccessible WHERE SeatId = @SeatId;",
                parameters: new Dictionary<string, object>
                {
                    { "@SeatId", seat.SeatId },
                    { "@VehicleModelId", seat.VehicleModelId },
                    { "@Row", seat.Row },
                    { "@Column", seat.Column },
                    { "@IsAtWindow", seat.IsAtWindow },
                    { "@IsAtAisle", seat.IsAtAisle },
                    { "@IsInFront", seat.IsInFront },
                    { "@IsInBack", seat.IsInBack },
                    { "@IsAccessible", seat.IsAccessible }
                }
            )).Match<OperationResult>(
                onValue: rowsAffected => rowsAffected > 0 ? Success() : Failure("No changes were made"),
                onError: error => Failure(error)
            );
        }

        public async Task<OperationResult> UpdateMultipleAsync(IEnumerable<Seat> seats)
        {
            int totalAffected = 0;
            foreach (var seat in seats)
            {
                var result = await UpdateAsync(seat);
                if (result is Success) totalAffected++;
            }
            return totalAffected > 0 ? Success() : Failure("No seats were updated");
        }

        public async Task<OperationResult> DeleteAsync(int id)
        {
            return (await _dbConnection.ExecuteAsync(
                sql: "DELETE FROM Seats WHERE SeatId = @SeatId;",
                parameters: new Dictionary<string, object> { { "@SeatId", id } }
            )).Match<OperationResult>(
                onValue: rowsAffected => rowsAffected > 0 ? Success() : Failure("No entries were deleted"),
                onError: error => Failure(error)
            );
        }

        public async Task<OperationResult> DeleteMultipleAsync(IEnumerable<int> ids)
        {
            int totalAffected = 0;
            foreach (var id in ids)
            {
                var result = await DeleteAsync(id);
                if (result is Success) totalAffected++;
            }
            return totalAffected > 0 ? Success() : Failure("No seats were deleted");
        }
    }
}
