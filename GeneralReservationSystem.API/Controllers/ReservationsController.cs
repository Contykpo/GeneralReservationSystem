using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Exceptions.Services;
using GeneralReservationSystem.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
            Application.Common.PagedResult<Application.Entities.Reservation> result = await reservationService.SearchReservationsAsync(searchDto, cancellationToken);
            return Ok(result);
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMyReservations(CancellationToken cancellationToken)
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            IEnumerable<Application.Entities.Reservation> reservations = await reservationService.GetUserReservationsAsync(int.Parse(userId), cancellationToken);
            return Ok(reservations);
        }

        [HttpGet("user/{userId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUserReservations([FromRoute] int userId, CancellationToken cancellationToken)
        {
            IEnumerable<Application.Entities.Reservation> reservations = await reservationService.GetUserReservationsAsync(userId, cancellationToken);
            return Ok(reservations);
        }

        [HttpGet("me/trip/{tripId:int}")]
        [Authorize]
        public async Task<IActionResult> GetMyReservationForTrip([FromRoute] int tripId, CancellationToken cancellationToken)
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            try
            {
                ReservationKeyDto keyDto = new() { TripId = tripId, UserId = int.Parse(userId) };
                Application.Entities.Reservation reservation = await reservationService.GetReservationAsync(keyDto, cancellationToken);
                return Ok(reservation);
            }
            catch (ServiceNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpGet("trip/{tripId:int}/user/{userId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetReservation([FromRoute] int tripId, [FromRoute] int userId, CancellationToken cancellationToken)
        {
            try
            {
                ReservationKeyDto keyDto = new() { TripId = tripId, UserId = userId };
                Application.Entities.Reservation reservation = await reservationService.GetReservationAsync(keyDto, cancellationToken);
                return Ok(reservation);
            }
            catch (ServiceNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateReservation([FromBody] CreateReservationDto dto, CancellationToken cancellationToken)
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            try
            {
                await reservationService.CreateReservationAsync(dto, int.Parse(userId), cancellationToken);
                return CreatedAtAction(nameof(GetMyReservationForTrip), new { tripId = dto.TripId }, new { message = "Reserva creada exitosamente" });
            }
            catch (ServiceBusinessException ex)
            {
                return Conflict(new { error = ex.Message });
            }
        }

        [HttpDelete("me/trip/{tripId:int}")]
        [Authorize]
        public async Task<IActionResult> DeleteMyReservation([FromRoute] int tripId, CancellationToken cancellationToken)
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            try
            {
                ReservationKeyDto keyDto = new() { TripId = tripId, UserId = int.Parse(userId) };
                await reservationService.DeleteReservationAsync(keyDto, cancellationToken);
                return NoContent();
            }
            catch (ServiceNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpDelete("trip/{tripId:int}/user/{userId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteReservation([FromRoute] int tripId, [FromRoute] int userId, CancellationToken cancellationToken)
        {
            try
            {
                ReservationKeyDto keyDto = new() { TripId = tripId, UserId = userId };
                await reservationService.DeleteReservationAsync(keyDto, cancellationToken);
                return NoContent();
            }
            catch (ServiceNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }
    }
}
