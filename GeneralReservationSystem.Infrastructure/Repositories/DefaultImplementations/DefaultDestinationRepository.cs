using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using static GeneralReservationSystem.Application.Common.OperationResult;

namespace GeneralReservationSystem.Infrastructure.Repositories.DefaultImplementations
{
    public class DefaultDestinationRepository : IDestinationRepository
    {
        private readonly DbConnectionHelper _dbConnection;
        private readonly ILogger<DefaultDestinationRepository> _logger;

        public DefaultDestinationRepository(DbConnectionHelper dbConnection, ILogger<DefaultDestinationRepository> logger)
        {
            _dbConnection = dbConnection;
            _logger = logger;
        }

        public async Task<OptionalResult<IList<Destination>>> SearchPagedAsync(int pageIndex, int pageSize, string? name = null, string? code = null, string? city = null, string? region = null, string? country = null, DestinationSearchSortBy? sortBy = null, bool descending = false)
        {
            var sql = "SELECT * FROM Destination WHERE 1=1";
            var parameters = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(name)) { sql += " AND Name LIKE @Name"; parameters.Add("@Name", $"%{name}%"); }
            if (!string.IsNullOrEmpty(code)) { sql += " AND Code LIKE @Code"; parameters.Add("@Code", $"%{code}%"); }
            if (!string.IsNullOrEmpty(city)) { sql += " AND City LIKE @City"; parameters.Add("@City", $"%{city}%"); }
            if (!string.IsNullOrEmpty(region)) { sql += " AND Region LIKE @Region"; parameters.Add("@Region", $"%{region}%"); }
            if (!string.IsNullOrEmpty(country)) { sql += " AND Country LIKE @Country"; parameters.Add("@Country", $"%{country}%"); }
            if (sortBy.HasValue)
            {
                sql += $" ORDER BY {sortBy.Value}{(descending ? " DESC" : " ASC")}";
            }
            else
            {
                sql += " ORDER BY DestinationId ASC";
            }
            sql += " OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            parameters.Add("@Offset", pageIndex * pageSize);
            parameters.Add("@PageSize", pageSize);
            return await _dbConnection.ExecuteReaderAsync<Destination>(
                sql: sql,
                converter: reader => new Destination
                {
                    DestinationId = reader.GetInt32(reader.GetOrdinal("DestinationId")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Code = reader.GetString(reader.GetOrdinal("Code")),
                    City = reader.GetString(reader.GetOrdinal("City")),
                    Region = reader.GetString(reader.GetOrdinal("Region")),
                    Country = reader.GetString(reader.GetOrdinal("Country")),
                    NormalizedName = reader.GetString(reader.GetOrdinal("NormalizedName")),
                    NormalizedCode = reader.GetString(reader.GetOrdinal("NormalizedCode")),
                    NormalizedCity = reader.GetString(reader.GetOrdinal("NormalizedCity")),
                    NormalizedRegion = reader.GetString(reader.GetOrdinal("NormalizedRegion")),
                    NormalizedCountry = reader.GetString(reader.GetOrdinal("NormalizedCountry")),
                    TimeZone = TimeZoneInfo.FindSystemTimeZoneById(reader.GetString(reader.GetOrdinal("TimeZone")))
                },
                parameters: parameters
            );
        }

        public async Task<OptionalResult<Destination>> GetByIdAsync(int id)
        {
            return await _dbConnection.ExecuteReaderSingleAsync<Destination>(
                sql: "SELECT * FROM Destination WHERE DestinationId = @DestinationId;",
                converter: reader => new Destination
                {
                    DestinationId = reader.GetInt32(reader.GetOrdinal("DestinationId")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Code = reader.GetString(reader.GetOrdinal("Code")),
                    City = reader.GetString(reader.GetOrdinal("City")),
                    Region = reader.GetString(reader.GetOrdinal("Region")),
                    Country = reader.GetString(reader.GetOrdinal("Country")),
                    NormalizedName = reader.GetString(reader.GetOrdinal("NormalizedName")),
                    NormalizedCode = reader.GetString(reader.GetOrdinal("NormalizedCode")),
                    NormalizedCity = reader.GetString(reader.GetOrdinal("NormalizedCity")),
                    NormalizedRegion = reader.GetString(reader.GetOrdinal("NormalizedRegion")),
                    NormalizedCountry = reader.GetString(reader.GetOrdinal("NormalizedCountry")),
                    TimeZone = TimeZoneInfo.FindSystemTimeZoneById(reader.GetString(reader.GetOrdinal("TimeZone")))
                },
                parameters: new Dictionary<string, object> { { "@DestinationId", id } }
            );
        }

        public async Task<OperationResult> AddAsync(Destination destination)
        {
            return (await _dbConnection.ExecuteAsync(
                sql: "INSERT INTO Destination (Name, Code, City, Region, Country, NormalizedName, NormalizedCode, NormalizedCity, NormalizedRegion, NormalizedCountry, TimeZone) VALUES (@Name, @Code, @City, @Region, @Country, @NormalizedName, @NormalizedCode, @NormalizedCity, @NormalizedRegion, @NormalizedCountry, @TimeZone);",
                parameters: new Dictionary<string, object>
                {
                    { "@Name", destination.Name },
                    { "@Code", destination.Code },
                    { "@City", destination.City },
                    { "@Region", destination.Region },
                    { "@Country", destination.Country },
                    { "@NormalizedName", destination.NormalizedName },
                    { "@NormalizedCode", destination.NormalizedCode },
                    { "@NormalizedCity", destination.NormalizedCity },
                    { "@NormalizedRegion", destination.NormalizedRegion },
                    { "@NormalizedCountry", destination.NormalizedCountry },
                    { "@TimeZone", destination.TimeZone.Id }
                }
            )).Match<OperationResult>(
                onValue: rowsAffected => rowsAffected > 0 ? Success() : Failure("No se realizaron cambios"),
                onError: error => Failure(error)
            );
        }

        public async Task<OperationResult> UpdateAsync(Destination destination)
        {
            return (await _dbConnection.ExecuteAsync(
                sql: "UPDATE Destination SET Name = @Name, Code = @Code, City = @City, Region = @Region, Country = @Country, NormalizedName = @NormalizedName, NormalizedCode = @NormalizedCode, NormalizedCity = @NormalizedCity, NormalizedRegion = @NormalizedRegion, NormalizedCountry = @NormalizedCountry, TimeZone = @TimeZone WHERE DestinationId = @DestinationId;",
                parameters: new Dictionary<string, object>
                {
                    { "@DestinationId", destination.DestinationId },
                    { "@Name", destination.Name },
                    { "@Code", destination.Code },
                    { "@City", destination.City },
                    { "@Region", destination.Region },
                    { "@Country", destination.Country },
                    { "@NormalizedName", destination.NormalizedName },
                    { "@NormalizedCode", destination.NormalizedCode },
                    { "@NormalizedCity", destination.NormalizedCity },
                    { "@NormalizedRegion", destination.NormalizedRegion },
                    { "@NormalizedCountry", destination.NormalizedCountry },
                    { "@TimeZone", destination.TimeZone.Id }
                }
            )).Match<OperationResult>(
                onValue: rowsAffected => rowsAffected > 0 ? Success() : Failure("No se realizaron cambios"),
                onError: error => Failure(error)
            );
        }

        public async Task<OperationResult> DeleteAsync(int id)
        {
            return (await _dbConnection.ExecuteAsync(
                sql: "DELETE FROM Destination WHERE DestinationId = @DestinationId;",
                parameters: new Dictionary<string, object> { { "@DestinationId", id } }
            )).Match<OperationResult>(
                onValue: rowsAffected => rowsAffected > 0 ? Success() : Failure("No se eliminaron entradas"),
                onError: error => Failure(error)
            );
        }
    }
}
