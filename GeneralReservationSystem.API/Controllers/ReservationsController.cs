using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Exceptions.Services;
using GeneralReservationSystem.Application.Services.Interfaces;
using GeneralReservationSystem.Infrastructure.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GeneralReservationSystem.API.Controllers
{
    [Route("api/reservations")]
    [ApiController]
    [Authorize]
    public class ReservationsController(IReservationService reservationService) : ControllerBase
    {
        [HttpPost("search")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SearchReservations([FromBody] PagedSearchRequestDto searchDto, CancellationToken cancellationToken)
        {
            var result = await reservationService.SearchReservationsAsync(searchDto, cancellationToken);
            return Ok(result);
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyReservations(CancellationToken cancellationToken)
        {
            if (HttpContext.Items["UserSession"] is not UserSessionInfo session)
                return Unauthorized(new { error = "No hay una sesión activa." });

            var reservations = await reservationService.GetUserReservationsAsync(session.UserId, cancellationToken);
            return Ok(reservations);
        }

        [HttpGet("user/{userId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUserReservations([FromRoute] int userId, CancellationToken cancellationToken)
        {
            var reservations = await reservationService.GetUserReservationsAsync(userId, cancellationToken);
            return Ok(reservations);
        }

        [HttpGet("me/trip/{tripId:int}")]
        public async Task<IActionResult> GetMyReservationForTrip([FromRoute] int tripId, CancellationToken cancellationToken)
        {
            if (HttpContext.Items["UserSession"] is not UserSessionInfo session)
                return Unauthorized(new { error = "No hay una sesión activa." });

            try
            {
                var keyDto = new ReservationKeyDto { TripId = tripId, UserId = session.UserId };
                var reservation = await reservationService.GetReservationAsync(keyDto, cancellationToken);
                return Ok(reservation);
            }
            catch (ServiceNotFoundException)
            {
                return NotFound(new { error = $"No se encontró una reserva para el viaje {tripId}." });
            }
        }

        [HttpGet("trip/{tripId:int}/user/{userId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetReservation([FromRoute] int tripId, [FromRoute] int userId, CancellationToken cancellationToken)
        {
            try
            {
                var keyDto = new ReservationKeyDto { TripId = tripId, UserId = userId };
                var reservation = await reservationService.GetReservationAsync(keyDto, cancellationToken);
                return Ok(reservation);
            }
            catch (ServiceNotFoundException)
            {
                return NotFound(new { error = $"No se encontró una reserva para el viaje {tripId} y el usuario especificado." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateReservation([FromBody] CreateReservationDto dto, CancellationToken cancellationToken)
        {
            if (HttpContext.Items["UserSession"] is not UserSessionInfo session)
                return Unauthorized(new { error = "No hay una sesión activa." });

            try
            {
                await reservationService.CreateReservationAsync(dto, session.UserId, cancellationToken);
                return CreatedAtAction(nameof(GetMyReservationForTrip), new { tripId = dto.TripId }, new { message = "Reserva creada exitosamente" });
            }
            catch (ServiceBusinessException ex) when (ex.Message.Contains("viaje o el usuario"))
            {
                return BadRequest(new { error = $"El viaje con ID {dto.TripId} no existe o no está disponible." });
            }
            catch (ServiceBusinessException ex) when (ex.Message.Contains("asiento ya está reservado"))
            {
                return BadRequest(new { error = $"El asiento {dto.Seat} ya está reservado para este viaje. Por favor, seleccione otro asiento." });
            }
            catch (ServiceBusinessException)
            {
                return BadRequest(new { error = "No se pudo crear la reserva. Verifique los datos e intente nuevamente." });
            }
        }

        [HttpDelete("me/trip/{tripId:int}")]
        public async Task<IActionResult> DeleteMyReservation([FromRoute] int tripId, CancellationToken cancellationToken)
        {
            if (HttpContext.Items["UserSession"] is not UserSessionInfo session)
                return Unauthorized(new { error = "No hay una sesión activa." });

            try
            {
                var keyDto = new ReservationKeyDto { TripId = tripId, UserId = session.UserId };
                await reservationService.DeleteReservationAsync(keyDto, cancellationToken);
                return NoContent();
            }
            catch (ServiceNotFoundException)
            {
                return NotFound(new { error = $"No se encontró una reserva para el viaje {tripId}." });
            }
        }

        [HttpDelete("trip/{tripId:int}/user/{userId:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteReservation([FromRoute] int tripId, [FromRoute] int userId, CancellationToken cancellationToken)
        {
            try
            {
                var keyDto = new ReservationKeyDto { TripId = tripId, UserId = userId };
                await reservationService.DeleteReservationAsync(keyDto, cancellationToken);
                return NoContent();
            }
            catch (ServiceNotFoundException)
            {
                return NotFound(new { error = $"No se encontró una reserva para el viaje {tripId} y el usuario especificado para eliminar." });
            }
        }
    }
}
