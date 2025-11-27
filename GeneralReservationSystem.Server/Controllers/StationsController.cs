using FluentValidation;
using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Exceptions.Services;
using GeneralReservationSystem.Server.Helpers;
using GeneralReservationSystem.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static GeneralReservationSystem.Application.Constants;

namespace GeneralReservationSystem.Server.Controllers
{
    [Route("api/stations")]
    [ApiController]
    public class StationsController(
        IApiStationService stationService,
        IValidator<PagedSearchRequestDto> pagedSearchValidator,
        IValidator<CreateStationDto> createStationValidator,
        IValidator<UpdateStationDto> updateStationValidator,
        IValidator<StationKeyDto> stationKeyValidator,
        IValidator<ImportStationDto> importStationValidator) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAllStations(CancellationToken cancellationToken)
        {
            IEnumerable<Station> stations = await stationService.GetAllStationsAsync(cancellationToken);
            return Ok(stations);
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchStations(CancellationToken cancellationToken)
        {
            PagedSearchRequestDto searchDto = new();
            searchDto.PopulateFromQuery(Request.Query);
            await ValidateAsync(pagedSearchValidator, searchDto, cancellationToken);

            PagedResult<Station> result = await stationService.SearchStationsAsync(searchDto, cancellationToken);
            return Ok(result);
        }

        [HttpGet("{stationId:int}")]
        public async Task<IActionResult> GetStation([FromRoute] int stationId, CancellationToken cancellationToken)
        {
            StationKeyDto keyDto = new() { StationId = stationId };
            await ValidateAsync(stationKeyValidator, keyDto, cancellationToken);
            Station station = await stationService.GetStationAsync(keyDto, cancellationToken);
            return Ok(station);
        }

        [HttpPost]
        [Authorize(Roles = AdminRoleName)]
        public async Task<IActionResult> CreateStation([FromBody] CreateStationDto dto, CancellationToken cancellationToken)
        {
            await ValidateAsync(createStationValidator, dto, cancellationToken);
            Station station = await stationService.CreateStationAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetStation), new { stationId = station.StationId }, station);
        }

        [HttpPatch("{stationId:int}")]
        [Authorize(Roles = AdminRoleName)]
        public async Task<IActionResult> UpdateStation([FromRoute] int stationId, [FromBody] UpdateStationDto dto, CancellationToken cancellationToken)
        {
            dto.StationId = stationId;
            await ValidateAsync(updateStationValidator, dto, cancellationToken);
            Station station = await stationService.UpdateStationAsync(dto, cancellationToken);
            return Ok(station);
        }

        [HttpDelete("{stationId:int}")]
        [Authorize(Roles = AdminRoleName)]
        public async Task<IActionResult> DeleteStation([FromRoute] int stationId, CancellationToken cancellationToken)
        {
            StationKeyDto keyDto = new() { StationId = stationId };
            await ValidateAsync(stationKeyValidator, keyDto, cancellationToken);
            await stationService.DeleteStationAsync(keyDto, cancellationToken);
            return NoContent();
        }

        [HttpPost("import")]
        [Authorize(Roles = AdminRoleName)]
        public async Task<IActionResult> ImportStationsFromCsv(IFormFile file, CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
            {
                throw new ServiceValidationException("El archivo CSV es requerido.", [new ValidationError("El archivo CSV es requerido.", "file")]);
            }

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                throw new ServiceValidationException("El archivo debe ser un CSV.", [new ValidationError("El archivo debe ser un CSV.", "file")]);
            }

            List<ImportStationDto> importDtos = [];
            await foreach (ImportStationDto? dto in CsvHelper.ParseAndValidateCsvAsync(
                file.OpenReadStream(),
                importStationValidator,
                cancellationToken
            ))
            {
                importDtos.Add(dto);
            }

            if (importDtos.Count == 0)
            {
                throw new ServiceValidationException("El archivo CSV no contiene estaciones válidas.", [new ValidationError("El archivo CSV no contiene estaciones válidas.", "file")]);
            }

            int affected = await stationService.CreateStationsBulkAsync(importDtos, cancellationToken);

            return Ok(new { message = $"Se importaron {affected} estaciones exitosamente.", count = affected });
        }

        [HttpGet("export")]
        [Authorize(Roles = AdminRoleName)]
        public async Task<IActionResult> ExportStationsToCsv(CancellationToken cancellationToken)
        {
            IEnumerable<Station> stations = await stationService.GetAllStationsAsync(cancellationToken);
            byte[] bytes = CsvHelper.ExportToCsv(stations);
            return File(bytes, "text/csv;charset=utf-8", $"stations_{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
        }
    }
}
