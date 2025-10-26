using FluentValidation;
using GeneralReservationSystem.API.Helpers;
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
    public class StationsController(IStationService stationService, IValidator<PagedSearchRequestDto> pagedSearchValidator, IValidator<CreateStationDto> createStationValidator, IValidator<UpdateStationDto> updateStationValidator, IValidator<StationKeyDto> stationKeyValidator) : ControllerBase
    {
        private readonly IValidator<PagedSearchRequestDto> _pagedSearchValidator = pagedSearchValidator;
        private readonly IValidator<CreateStationDto> _createStationValidator = createStationValidator;
        private readonly IValidator<UpdateStationDto> _updateStationValidator = updateStationValidator;
        private readonly IValidator<StationKeyDto> _stationKeyValidator = stationKeyValidator;

        [HttpGet]
        public async Task<IActionResult> GetAllStations(CancellationToken cancellationToken)
        {
            IEnumerable<Station> stations = await stationService.GetAllStationsAsync(cancellationToken);
            return Ok(stations);
        }

        [HttpPost("search")]
        public async Task<IActionResult> SearchStations([FromBody] PagedSearchRequestDto searchDto, CancellationToken cancellationToken)
        {
            var validationResult = await ValidationHelper.ValidateAsync(_pagedSearchValidator, searchDto, cancellationToken);
            if (validationResult != null)
                return validationResult;

            PagedResult<Station> result = await stationService.SearchStationsAsync(searchDto, cancellationToken);
            return Ok(result);
        }

        [HttpGet("{stationId:int}")]
        public async Task<IActionResult> GetStation([FromRoute] int stationId, CancellationToken cancellationToken)
        {
            var keyDto = new StationKeyDto { StationId = stationId };
            var validationResult = await ValidationHelper.ValidateAsync(_stationKeyValidator, keyDto, cancellationToken);
            if (validationResult != null)
                return validationResult;

            try
            {
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
            var validationResult = await ValidationHelper.ValidateAsync(_createStationValidator, dto, cancellationToken);
            if (validationResult != null)
                return validationResult;

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
            dto.StationId = stationId;
            var validationResult = await ValidationHelper.ValidateAsync(_updateStationValidator, dto, cancellationToken);
            if (validationResult != null)
                return validationResult;

            try
            {
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
            var keyDto = new StationKeyDto { StationId = stationId };
            var validationResult = await ValidationHelper.ValidateAsync(_stationKeyValidator, keyDto, cancellationToken);
            if (validationResult != null)
                return validationResult;

            try
            {
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
