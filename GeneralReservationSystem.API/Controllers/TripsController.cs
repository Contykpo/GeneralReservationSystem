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
            var trips = await tripService.GetAllTripsAsync(cancellationToken);
            return Ok(trips);
        }

        [HttpPost("search")]
        public async Task<IActionResult> SearchTrips([FromBody] PagedSearchRequestDto searchDto, CancellationToken cancellationToken)
        {
            var result = await tripService.SearchTripsAsync(searchDto, cancellationToken);
            return Ok(result);
        }

        [HttpGet("{tripId:int}")]
        public async Task<IActionResult> GetTrip([FromRoute] int tripId, CancellationToken cancellationToken)
        {
            try
            {
                var keyDto = new TripKeyDto { TripId = tripId };
                var trip = await tripService.GetTripAsync(keyDto, cancellationToken);
                return Ok(trip);
            }
            catch (ServiceNotFoundException)
            {
                return NotFound(new { error = $"No se encontró el viaje con ID {tripId}." });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateTrip([FromBody] CreateTripDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var trip = await tripService.CreateTripAsync(dto, cancellationToken);
                return CreatedAtAction(nameof(GetTrip), new { tripId = trip.TripId }, trip);
            }
            catch (ServiceBusinessException ex) when (ex.Message.Contains("estación de salida o llegada"))
            {
                return BadRequest(new { error = "Una o ambas estaciones especificadas no existen. Verifique los IDs de las estaciones." });
            }
            catch (ServiceBusinessException ex) when (ex.Message.Contains("diferentes"))
            {
                return BadRequest(new { error = "La estación de salida y la estación de llegada deben ser diferentes." });
            }
            catch (ServiceBusinessException ex) when (ex.Message.Contains("hora de llegada"))
            {
                return BadRequest(new { error = "La hora de llegada debe ser posterior a la hora de salida." });
            }
            catch (ServiceBusinessException)
            {
                return BadRequest(new { error = "Error al crear el viaje. Verifique que todos los datos sean válidos." });
            }
        }

        [HttpPut("{tripId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateTrip([FromRoute] int tripId, [FromBody] UpdateTripDto dto, CancellationToken cancellationToken)
        {
            try
            {
                dto.TripId = tripId;
                var trip = await tripService.UpdateTripAsync(dto, cancellationToken);
                return Ok(trip);
            }
            catch (ServiceNotFoundException)
            {
                return NotFound(new { error = $"No se encontró el viaje con ID {tripId} para actualizar." });
            }
            catch (ServiceBusinessException ex) when (ex.Message.Contains("estación de salida o llegada"))
            {
                return BadRequest(new { error = "Una o ambas estaciones especificadas no existen. Verifique los IDs de las estaciones." });
            }
            catch (ServiceBusinessException ex) when (ex.Message.Contains("diferentes"))
            {
                return BadRequest(new { error = "La estación de salida y la estación de llegada deben ser diferentes." });
            }
            catch (ServiceBusinessException ex) when (ex.Message.Contains("hora de llegada"))
            {
                return BadRequest(new { error = "La hora de llegada debe ser posterior a la hora de salida." });
            }
            catch (ServiceBusinessException)
            {
                return BadRequest(new { error = "Error al actualizar el viaje. Verifique que todos los datos sean válidos." });
            }
        }

        [HttpDelete("{tripId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteTrip([FromRoute] int tripId, CancellationToken cancellationToken)
        {
            try
            {
                var keyDto = new TripKeyDto { TripId = tripId };
                await tripService.DeleteTripAsync(keyDto, cancellationToken);
                return NoContent();
            }
            catch (ServiceNotFoundException)
            {
                return NotFound(new { error = $"No se encontró el viaje con ID {tripId} para eliminar." });
            }
        }
    }
}
