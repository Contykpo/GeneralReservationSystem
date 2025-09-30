using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using static GeneralReservationSystem.Application.Common.OperationResult;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GeneralReservationSystem.Infrastructure.Repositories.DefaultImplementations
{
    public class DefaultVehicleRepository : IVehicleRepository
    {
        private readonly DbConnectionHelper _dbConnection;
        private readonly ILogger<DefaultVehicleRepository> _logger;

        public DefaultVehicleRepository(DbConnectionHelper dbConnection, ILogger<DefaultVehicleRepository> logger)
        {
            _dbConnection = dbConnection;
            _logger = logger;
        }

        public async Task<OptionalResult<Vehicle>> GetByIdAsync(int id)
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

        public async Task<OptionalResult<VehicleModel>> GetModelByIdAsync(int id)
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

        public async Task<OptionalResult<IList<VehicleDetailsDto>>> SearchPagedAsync(int pageIndex, int pageSize, string? modelName = null, string? manufacturer = null, string? licensePlate = null, VehicleSearchSortBy? sortBy = null, bool descending = false)
        {
            var sql = "SELECT v.VehicleId, v.VehicleModelId, v.LicensePlate, v.Status, vm.Name AS ModelName, vm.Manufacturer FROM Vehicle v INNER JOIN VehicleModel vm ON v.VehicleModelId = vm.VehicleModelId WHERE 1=1";
            var parameters = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(modelName)) { sql += " AND vm.Name LIKE @ModelName"; parameters.Add("@ModelName", $"%{modelName}%"); }
            if (!string.IsNullOrEmpty(manufacturer)) { sql += " AND vm.Manufacturer LIKE @Manufacturer"; parameters.Add("@Manufacturer", $"%{manufacturer}%"); }
            if (!string.IsNullOrEmpty(licensePlate)) { sql += " AND v.LicensePlate LIKE @LicensePlate"; parameters.Add("@LicensePlate", $"%{licensePlate}%"); }
            if (sortBy.HasValue) { 
                sql += $" ORDER BY {sortBy.Value}{(descending ? " DESC" : " ASC")}"; 
            } else
            {
                sql += "ORDER BY VehicleId ASC";
            }
            sql += " OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            parameters.Add("@Offset", pageIndex * pageSize);
            parameters.Add("@PageSize", pageSize);
            return await _dbConnection.ExecuteReaderAsync<VehicleDetailsDto>(
                sql: sql,
                converter: reader => new VehicleDetailsDto
                {
                    VehicleId = reader.GetInt32(reader.GetOrdinal("VehicleId")),
                    VehicleModelId = reader.GetInt32(reader.GetOrdinal("VehicleModelId")),
                    LicensePlate = reader.GetString(reader.GetOrdinal("LicensePlate")),
                    Status = reader.GetString(reader.GetOrdinal("Status")),
                    ModelName = reader.GetString(reader.GetOrdinal("ModelName")),
                    Manufacturer = reader.GetString(reader.GetOrdinal("Manufacturer"))
                },
                parameters: parameters
            );
        }

        public async Task<OperationResult> AddAsync(Vehicle vehicle)
        {
            return (await _dbConnection.ExecuteAsync(
                sql: "INSERT INTO Vehicle (VehicleModelId, LicensePlate, Status) VALUES (@VehicleModelId, @LicensePlate, @Status);",
                parameters: new Dictionary<string, object>
                {
                    { "@VehicleModelId", vehicle.VehicleModelId },
                    { "@LicensePlate", vehicle.LicensePlate },
                    { "@Status", vehicle.Status }
                }
            )).Match<OperationResult>(
                onValue: rowsAffected => rowsAffected > 0 ? Success() : Failure("No se realizaron cambios"),
                onError: error => Failure(error)
            );
        }

        public async Task<OperationResult> UpdateAsync(Vehicle vehicle)
        {
            return (await _dbConnection.ExecuteAsync(
                sql: "UPDATE Vehicle SET VehicleModelId = @VehicleModelId, LicensePlate = @LicensePlate, Status = @Status WHERE VehicleId = @VehicleId;",
                parameters: new Dictionary<string, object>
                {
                    { "@VehicleId", vehicle.VehicleId },
                    { "@VehicleModelId", vehicle.VehicleModelId },
                    { "@LicensePlate", vehicle.LicensePlate },
                    { "@Status", vehicle.Status }
                }
            )).Match<OperationResult>(
                onValue: rowsAffected => rowsAffected > 0 ? Success() : Failure("No se realizaron cambios"),
                onError: error => Failure(error)
            );
        }

        public async Task<OperationResult> DeleteAsync(int id)
        {
            return (await _dbConnection.ExecuteAsync(
                sql: "DELETE FROM Vehicle WHERE VehicleId = @VehicleId;",
                parameters: new Dictionary<string, object> { { "@VehicleId", id } }
            )).Match<OperationResult>(
                onValue: rowsAffected => rowsAffected > 0 ? Success() : Failure("No se eliminaron entradas"),
                onError: error => Failure(error)
            );
        }

        public async Task<OptionalResult<IList<Trip>>> GetTripsByVehicleIdAsync(int id)
        {
            return await _dbConnection.ExecuteReaderAsync<Trip>(
                sql: "SELECT * FROM Trip WHERE VehicleId = @VehicleId;",
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
                parameters: new Dictionary<string, object> { { "@VehicleId", id } }
            );
        }
    }
}
