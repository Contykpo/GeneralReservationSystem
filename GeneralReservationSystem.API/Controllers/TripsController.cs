using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Exceptions.Services;
using GeneralReservationSystem.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GeneralReservationSystem.API.Controllers
{
    [Route("api/trips")]
    [ApiController]
    public class TripsController(ITripService tripService) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAllTrips(CancellationToken cancellationToken)
        {
            IEnumerable<Application.Entities.Trip> trips = await tripService.GetAllTripsAsync(cancellationToken);
            return Ok(trips);
        }

        [HttpPost("search")]
        public async Task<IActionResult> SearchTrips([FromBody] PagedSearchRequestDto searchDto, CancellationToken cancellationToken)
        {
            Application.Common.PagedResult<Application.Entities.Trip> result = await tripService.SearchTripsAsync(searchDto, cancellationToken);
            return Ok(result);
        }

        [HttpGet("{tripId:int}")]
        public async Task<IActionResult> GetTrip([FromRoute] int tripId, CancellationToken cancellationToken)
        {
            try
            {
                TripKeyDto keyDto = new() { TripId = tripId };
                Application.Entities.Trip trip = await tripService.GetTripAsync(keyDto, cancellationToken);
                return Ok(trip);
            }
            catch (ServiceNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateTrip([FromBody] CreateTripDto dto, CancellationToken cancellationToken)
        {
            try
            {
                Application.Entities.Trip trip = await tripService.CreateTripAsync(dto, cancellationToken);
                return CreatedAtAction(nameof(GetTrip), new { tripId = trip.TripId }, trip);
            }
            catch (ServiceBusinessException ex)
            {
                return Conflict(new { error = ex.Message });
            }
        }

        [HttpPut("{tripId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateTrip([FromRoute] int tripId, [FromBody] UpdateTripDto dto, CancellationToken cancellationToken)
        {
            try
            {
                dto.TripId = tripId;
                Application.Entities.Trip trip = await tripService.UpdateTripAsync(dto, cancellationToken);
                return Ok(trip);
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

        [HttpDelete("{tripId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteTrip([FromRoute] int tripId, CancellationToken cancellationToken)
        {
            try
            {
                TripKeyDto keyDto = new() { TripId = tripId };
                await tripService.DeleteTripAsync(keyDto, cancellationToken);
                return NoContent();
            }
            catch (ServiceNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }
    }
}
