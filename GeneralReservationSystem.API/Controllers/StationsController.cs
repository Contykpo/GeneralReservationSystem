using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Exceptions.Services;
using GeneralReservationSystem.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GeneralReservationSystem.API.Controllers
{
    [Route("api/stations")]
    [ApiController]
    public class StationsController(IStationService stationService) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAllStations(CancellationToken cancellationToken)
        {
            var stations = await stationService.GetAllStationsAsync(cancellationToken);
            return Ok(stations);
        }

        [HttpPost("search")]
        public async Task<IActionResult> SearchStations([FromBody] PagedSearchRequestDto searchDto, CancellationToken cancellationToken)
        {
            var result = await stationService.SearchStationsAsync(searchDto, cancellationToken);
            return Ok(result);
        }

        [HttpGet("{stationId:int}")]
        public async Task<IActionResult> GetStation([FromRoute] int stationId, CancellationToken cancellationToken)
        {
            try
            {
                var keyDto = new StationKeyDto { StationId = stationId };
                var station = await stationService.GetStationAsync(keyDto, cancellationToken);
                return Ok(station);
            }
            catch (ServiceNotFoundException)
            {
                return NotFound(new { error = $"No se encontró la estación con ID {stationId}." });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateStation([FromBody] CreateStationDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var station = await stationService.CreateStationAsync(dto, cancellationToken);
                return CreatedAtAction(nameof(GetStation), new { stationId = station.StationId }, station);
            }
            catch (ServiceBusinessException)
            {
                return BadRequest(new { error = $"Ya existe una estación con el nombre '{dto.StationName}'. Por favor, use un nombre diferente." });
            }
        }

        [HttpPut("{stationId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStation([FromRoute] int stationId, [FromBody] UpdateStationDto dto, CancellationToken cancellationToken)
        {
            try
            {
                dto.StationId = stationId;
                var station = await stationService.UpdateStationAsync(dto, cancellationToken);
                return Ok(station);
            }
            catch (ServiceNotFoundException)
            {
                return NotFound(new { error = $"No se encontró la estación con ID {stationId} para actualizar." });
            }
            catch (ServiceBusinessException)
            {
                return BadRequest(new { error = "El nombre de estación ya está en uso por otra estación." });
            }
        }

        [HttpDelete("{stationId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteStation([FromRoute] int stationId, CancellationToken cancellationToken)
        {
            try
            {
                var keyDto = new StationKeyDto { StationId = stationId };
                await stationService.DeleteStationAsync(keyDto, cancellationToken);
                return NoContent();
            }
            catch (ServiceNotFoundException)
            {
                return NotFound(new { error = $"No se encontró la estación con ID {stationId} para eliminar." });
            }
        }
    }
}
