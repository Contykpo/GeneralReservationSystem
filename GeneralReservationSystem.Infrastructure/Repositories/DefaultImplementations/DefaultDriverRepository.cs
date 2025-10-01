using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using static GeneralReservationSystem.Application.Common.OperationResult;

namespace GeneralReservationSystem.Infrastructure.Repositories.DefaultImplementations
{
    public class DefaultDriverRepository : IDriverRepository
    {
        private readonly DbConnectionHelper _dbConnection;
        private readonly ILogger<DefaultDriverRepository> _logger;

        public DefaultDriverRepository(DbConnectionHelper dbConnection, ILogger<DefaultDriverRepository> logger)
        {
            _dbConnection = dbConnection;
            _logger = logger;
        }

        public async Task<OptionalResult<PagedResult<Driver>>> SearchPagedAsync(int pageIndex, int pageSize, string? firstName = null, string? lastName = null, string? licenseNumber = null, string? phoneNumber = null, DriverSearchSortBy? sortBy = null, bool descending = false)
        {
            var baseSql = "FROM Driver WHERE 1=1";
            var parameters = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(firstName)) { baseSql += " AND FirstName LIKE @FirstName"; parameters.Add("@FirstName", $"%{firstName}%"); }
            if (!string.IsNullOrEmpty(lastName)) { baseSql += " AND LastName LIKE @LastName"; parameters.Add("@LastName", $"%{lastName}%"); }
            if (!string.IsNullOrEmpty(licenseNumber)) { baseSql += " AND LicenseNumber LIKE @LicenseNumber"; parameters.Add("@LicenseNumber", $"%{licenseNumber}%"); }
            // phoneNumber is not used in the table, but you can add a filter if needed

            // 1. Get total count
            var countSql = $"SELECT COUNT(*) {baseSql}";
            int totalCount = 0;
            bool errorOccurred = false;
            string? errorMsg = null;
            (await _dbConnection.ExecuteScalarAsync<int>(countSql, parameters)).Match(
                onValue: v => totalCount = v,
                onEmpty: () => { totalCount = 0; },
                onError: err => { errorOccurred = true; errorMsg = err; }
            );
            if (errorOccurred)
                return OptionalResult<PagedResult<Driver>>.Error<PagedResult<Driver>>(errorMsg);

            // 2. Get paged items
            var selectSql = $"SELECT * {baseSql}";
            if (sortBy.HasValue)
                selectSql += $" ORDER BY {sortBy.Value}{(descending ? " DESC" : " ASC")}";
            else
                selectSql += " ORDER BY DriverId ASC";
            selectSql += " OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            parameters["@Offset"] = pageIndex * pageSize;
            parameters["@PageSize"] = pageSize;

            var itemsResult = await _dbConnection.ExecuteReaderAsync<Driver>(
                sql: selectSql,
                converter: reader => new Driver
                {
                    DriverId = reader.GetInt32(reader.GetOrdinal("DriverId")),
                    FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                    LastName = reader.GetString(reader.GetOrdinal("LastName")),
                    LicenseNumber = reader.GetString(reader.GetOrdinal("LicenseNumber")),
                    IdentificationNumber = reader.GetInt32(reader.GetOrdinal("IdentificationNumber")),
                    LicenseExpiryDate = reader.GetDateTime(reader.GetOrdinal("LicenseExpiryDate"))
                },
                parameters: parameters
            );

            return itemsResult.Match<OptionalResult<PagedResult<Driver>>>(
                onValue: items => OptionalResult<PagedResult<Driver>>.Value(new PagedResult<Driver>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = pageIndex,
                    PageSize = pageSize
                }),
                onEmpty: () => OptionalResult<PagedResult<Driver>>.Value(new PagedResult<Driver>
                {
                    Items = new List<Driver>(),
                    TotalCount = totalCount,
                    PageNumber = pageIndex,
                    PageSize = pageSize
                }),
                onError: err => OptionalResult<PagedResult<Driver>>.Error<PagedResult<Driver>>(err)
            );
        }

        public async Task<OptionalResult<Driver>> GetByIdAsync(int id)
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

        public async Task<OptionalResult<Driver>> GetByLicenseNumberAsync(string licenseNumber)
        {
            return await _dbConnection.ExecuteReaderSingleAsync<Driver>(
                sql: "SELECT * FROM Driver WHERE LicenseNumber = @LicenseNumber;",
                converter: reader => new Driver
                {
                    DriverId = reader.GetInt32(reader.GetOrdinal("DriverId")),
                    FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                    LastName = reader.GetString(reader.GetOrdinal("LastName")),
                    LicenseNumber = reader.GetString(reader.GetOrdinal("LicenseNumber")),
                    IdentificationNumber = reader.GetInt32(reader.GetOrdinal("IdentificationNumber")),
                    LicenseExpiryDate = reader.GetDateTime(reader.GetOrdinal("LicenseExpiryDate"))
                },
                parameters: new Dictionary<string, object> { { "@LicenseNumber", licenseNumber } }
            );
        }

        public async Task<OptionalResult<Driver>> GetByIdentificationNumberAsync(int identificationNumber)
        {
            return await _dbConnection.ExecuteReaderSingleAsync<Driver>(
                sql: "SELECT * FROM Driver WHERE IdentificationNumber = @IdentificationNumber;",
                converter: reader => new Driver
                {
                    DriverId = reader.GetInt32(reader.GetOrdinal("DriverId")),
                    FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                    LastName = reader.GetString(reader.GetOrdinal("LastName")),
                    LicenseNumber = reader.GetString(reader.GetOrdinal("LicenseNumber")),
                    IdentificationNumber = reader.GetInt32(reader.GetOrdinal("IdentificationNumber")),
                    LicenseExpiryDate = reader.GetDateTime(reader.GetOrdinal("LicenseExpiryDate"))
                },
                parameters: new Dictionary<string, object> { { "@IdentificationNumber", identificationNumber } }
            );
        }

        public async Task<OptionalResult<IList<Trip>>> GetTripsByDriverIdAsync(int id)
        {
            return await _dbConnection.ExecuteReaderAsync<Trip>(
                sql: "SELECT * FROM Trip WHERE DriverId = @DriverId;",
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
                parameters: new Dictionary<string, object> { { "@DriverId", id } }
            );
        }

        public async Task<OperationResult> AddAsync(Driver driver)
        {
            return (await _dbConnection.ExecuteAsync(
                sql: "INSERT INTO Driver (FirstName, LastName, LicenseNumber, IdentificationNumber, LicenseExpiryDate) VALUES (@FirstName, @LastName, @LicenseNumber, @IdentificationNumber, @LicenseExpiryDate);",
                parameters: new Dictionary<string, object>
                {
                    { "@FirstName", driver.FirstName },
                    { "@LastName", driver.LastName },
                    { "@LicenseNumber", driver.LicenseNumber },
                    { "@IdentificationNumber", driver.IdentificationNumber },
                    { "@LicenseExpiryDate", driver.LicenseExpiryDate }
                }
            )).Match<OperationResult>(
                onValue: rowsAffected => rowsAffected > 0 ? Success() : Failure("No se realizaron cambios"),
                onError: error => Failure(error)
            );
        }

        public async Task<OperationResult> UpdateAsync(Driver driver)
        {
            return (await _dbConnection.ExecuteAsync(
                sql: "UPDATE Driver SET FirstName = @FirstName, LastName = @LastName, LicenseNumber = @LicenseNumber, IdentificationNumber = @IdentificationNumber, LicenseExpiryDate = @LicenseExpiryDate WHERE DriverId = @DriverId;",
                parameters: new Dictionary<string, object>
                {
                    { "@DriverId", driver.DriverId },
                    { "@FirstName", driver.FirstName },
                    { "@LastName", driver.LastName },
                    { "@LicenseNumber", driver.LicenseNumber },
                    { "@IdentificationNumber", driver.IdentificationNumber },
                    { "@LicenseExpiryDate", driver.LicenseExpiryDate }
                }
            )).Match<OperationResult>(
                onValue: rowsAffected => rowsAffected > 0 ? Success() : Failure("No se realizaron cambios"),
                onError: error => Failure(error)
            );
        }

        public async Task<OperationResult> DeleteAsync(int id)
        {
            return (await _dbConnection.ExecuteAsync(
                sql: "DELETE FROM Driver WHERE DriverId = @DriverId;",
                parameters: new Dictionary<string, object> { { "@DriverId", id } }
            )).Match<OperationResult>(
                onValue: rowsAffected => rowsAffected > 0 ? Success() : Failure("No se eliminaron entradas"),
                onError: error => Failure(error)
            );
        }
    }
}
