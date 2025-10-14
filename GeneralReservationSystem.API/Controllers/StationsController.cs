using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;
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
            IEnumerable<Station> stations = await stationService.GetAllStationsAsync(cancellationToken);
            return Ok(stations);
        }

        [HttpPost("search")]
        public async Task<IActionResult> SearchStations([FromBody] PagedSearchRequestDto searchDto, CancellationToken cancellationToken)
        {
            PagedResult<Station> result = await stationService.SearchStationsAsync(searchDto, cancellationToken);
            return Ok(result);
        }

        [HttpGet("{stationId:int}")]
        public async Task<IActionResult> GetStation([FromRoute] int stationId, CancellationToken cancellationToken)
        {
            try
            {
                StationKeyDto keyDto = new() { StationId = stationId };
                Station station = await stationService.GetStationAsync(keyDto, cancellationToken);
                return Ok(station);
            }
            catch (ServiceNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateStation([FromBody] CreateStationDto dto, CancellationToken cancellationToken)
        {
            try
            {
                Station station = await stationService.CreateStationAsync(dto, cancellationToken);
                return CreatedAtAction(nameof(GetStation), new { stationId = station.StationId }, station);
            }
            catch (ServiceBusinessException ex)
            {
                return Conflict(new { error = ex.Message });
            }
        }

        [HttpPut("{stationId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStation([FromRoute] int stationId, [FromBody] UpdateStationDto dto, CancellationToken cancellationToken)
        {
            try
            {
                dto.StationId = stationId;
                Station station = await stationService.UpdateStationAsync(dto, cancellationToken);
                return Ok(station);
            }
            catch (ServiceNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ServiceBusinessException ex)
            {
                return Conflict(new { error = ex.Message });
            }
        }

        [HttpDelete("{stationId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteStation([FromRoute] int stationId, CancellationToken cancellationToken)
        {
            try
            {
                StationKeyDto keyDto = new() { StationId = stationId };
                await stationService.DeleteStationAsync(keyDto, cancellationToken);
                return NoContent();
            }
            catch (ServiceNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }
    }
}
