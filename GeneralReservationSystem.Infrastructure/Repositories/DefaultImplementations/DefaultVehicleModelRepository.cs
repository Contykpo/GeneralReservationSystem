using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text;
using static GeneralReservationSystem.Application.Common.OperationResult;

namespace GeneralReservationSystem.Infrastructure.Repositories.DefaultImplementations
{
    public class DefaultVehicleModelRepository : IVehicleModelRepository
    {
        private readonly DbConnectionHelper _dbConnection;
        private readonly ILogger<DefaultVehicleModelRepository> _logger;

        public DefaultVehicleModelRepository(DbConnectionHelper dbConnection, ILogger<DefaultVehicleModelRepository> logger)
        {
            _dbConnection = dbConnection;
            _logger = logger;
        }

        public async Task<OptionalResult<IList<VehicleModel>>> GetAllAsync()
        {
            return (await _dbConnection.ExecuteReaderAsync<VehicleModel>(
                sql: "SELECT * FROM VehicleModels;",
                converter: reader => new VehicleModel
                {
                    VehicleModelId = reader.GetInt32(reader.GetOrdinal("VehicleModelId")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Manufacturer = reader.GetString(reader.GetOrdinal("Manufacturer"))
                }
            ));
        }

        public async Task<OptionalResult<IList<VehicleModel>>> SearchPagedAsync(int pageIndex, int pageSize, string? name = null, string? manufacturer = null, VehicleModelSearchSortBy? sortBy = null, bool descending = false)
        {
            var sql = "SELECT * FROM VehicleModels WHERE 1=1";
            var parameters = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(name)) { sql += " AND Name LIKE @Name"; parameters.Add("@Name", $"%{name}%"); }
            if (!string.IsNullOrEmpty(manufacturer)) { sql += " AND Manufacturer LIKE @Manufacturer"; parameters.Add("@Manufacturer", $"%{manufacturer}%"); }
            if (sortBy.HasValue) { sql += $" ORDER BY {sortBy.Value}{(descending ? " DESC" : " ASC")}"; }
            sql += " OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            parameters.Add("@Offset", pageIndex * pageSize);
            parameters.Add("@PageSize", pageSize);
            return (await _dbConnection.ExecuteReaderAsync<VehicleModel>(
                sql: sql,
                converter: reader => new VehicleModel
                {
                    VehicleModelId = reader.GetInt32(reader.GetOrdinal("VehicleModelId")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Manufacturer = reader.GetString(reader.GetOrdinal("Manufacturer"))
                },
                parameters: parameters
            ));
        }

        public async Task<OptionalResult<VehicleModel>> GetByIdAsync(int id)
        {
            return await _dbConnection.ExecuteReaderSingleAsync<VehicleModel>(
                sql: "SELECT * FROM VehicleModels WHERE VehicleModelId = @VehicleModelId;",
                converter: reader => new VehicleModel
                {
                    VehicleModelId = reader.GetInt32(reader.GetOrdinal("VehicleModelId")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Manufacturer = reader.GetString(reader.GetOrdinal("Manufacturer"))
                },
                parameters: new Dictionary<string, object> { { "@VehicleModelId", id } }
            );
        }

        public async Task<OperationResult> AddAsync(VehicleModel vehicleModel, IEnumerable<Seat> seats)
        {
            // Insert VehicleModel and related Seats in a transaction
            return await _dbConnection.ExecuteInTransactionAsync(async (connection, transaction) =>
            {
                var insertModelSql = "INSERT INTO VehicleModels (Name, Manufacturer) OUTPUT INSERTED.VehicleModelId VALUES (@Name, @Manufacturer);";
                var modelParams = new Dictionary<string, object>
                {
                    { "@Name", vehicleModel.Name },
                    { "@Manufacturer", vehicleModel.Manufacturer }
                };
                int modelId = (await _dbConnection.ExecuteScalarAsync<int>(insertModelSql, connection, modelParams, transaction)).Match(
                    onValue: id => id,
                    onEmpty: () => throw new InvalidOperationException("Failed to insert VehicleModel: No ID returned."),
                    onError: error => throw new InvalidOperationException($"Failed to insert VehicleModel: {error}")
                );

                // Bulk insert seats
                if (seats.Any())
                {
                    var insertSeatSql = new StringBuilder();
                    var seatParams = new Dictionary<string, object>();
                    int i = 0;
                    insertSeatSql.Append("INSERT INTO Seats (VehicleModelId, Row, Column, IsAtWindow, IsAtAisle, IsInFront, IsInBack, IsAccessible) VALUES ");
                    foreach (var seat in seats)
                    {
                        if (i > 0) insertSeatSql.Append(", ");
                        insertSeatSql.Append($"(@VehicleModelId{i}, @Row{i}, @Column{i}, @IsAtWindow{i}, @IsAtAisle{i}, @IsInFront{i}, @IsInBack{i}, @IsAccessible{i})");
                        seatParams.Add($"@VehicleModelId{i}", modelId);
                        seatParams.Add($"@Row{i}", seat.Row);
                        seatParams.Add($"@Column{i}", seat.Column);
                        seatParams.Add($"@IsAtWindow{i}", seat.IsAtWindow);
                        seatParams.Add($"@IsAtAisle{i}", seat.IsAtAisle);
                        seatParams.Add($"@IsInFront{i}", seat.IsInFront);
                        seatParams.Add($"@IsInBack{i}", seat.IsInBack);
                        seatParams.Add($"@IsAccessible{i}", seat.IsAccessible);
                        i++;
                    }
                    (await _dbConnection.ExecuteAsync(insertSeatSql.ToString(), connection, seatParams, transaction)).Match(
                        onValue: rowsAffected => { if (rowsAffected < seats.Count()) throw new InvalidOperationException("Failed to insert all Seats."); },
                        onEmpty: () => throw new InvalidOperationException("Failed to insert Seats: No rows affected."),
                        onError: error => throw new InvalidOperationException($"Failed to insert Seats: {error}")
                    );
                }
            });
        }

        public async Task<OperationResult> UpdateAsync(VehicleModel vehicleModel)
        {
            return (await _dbConnection.ExecuteAsync(
                sql: "UPDATE VehicleModels SET Name = @Name, Manufacturer = @Manufacturer WHERE VehicleModelId = @VehicleModelId;",
                parameters: new Dictionary<string, object>
                {
                    { "@VehicleModelId", vehicleModel.VehicleModelId },
                    { "@Name", vehicleModel.Name },
                    { "@Manufacturer", vehicleModel.Manufacturer }
                }
            )).Match<OperationResult>(
                onValue: rowsAffected => rowsAffected > 0 ? Success() : Failure("No changes were made"),
                onError: error => Failure(error)
            );
        }

        public async Task<OperationResult> DeleteAsync(int id)
        {
            return (await _dbConnection.ExecuteAsync(
                sql: "DELETE FROM VehicleModels WHERE VehicleModelId = @VehicleModelId;",
                parameters: new Dictionary<string, object> { { "@VehicleModelId", id } }
            )).Match<OperationResult>(
                onValue: rowsAffected => rowsAffected > 0 ? Success() : Failure("No entries were deleted"),
                onError: error => Failure(error)
            );
        }
    }
}
