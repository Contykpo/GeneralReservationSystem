using FluentValidation;
using GeneralReservationSystem.API.Helpers;
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
    public class ReservationsController(IReservationService reservationService, IValidator<PagedSearchRequestDto> pagedSearchValidator, IValidator<CreateReservationDto> createReservationValidator, IValidator<ReservationKeyDto> reservationKeyValidator) : ControllerBase
    {
        private readonly IValidator<PagedSearchRequestDto> _pagedSearchValidator = pagedSearchValidator;
        private readonly IValidator<CreateReservationDto> _createReservationValidator = createReservationValidator;
        private readonly IValidator<ReservationKeyDto> _reservationKeyValidator = reservationKeyValidator;

        [HttpPost("search")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SearchReservations([FromBody] PagedSearchRequestDto searchDto, CancellationToken cancellationToken)
        {
            var validationResult = await ValidationHelper.ValidateAsync(_pagedSearchValidator, searchDto, cancellationToken);
            if (validationResult != null)
                return validationResult;

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

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateReservation([FromBody] CreateReservationDto dto, CancellationToken cancellationToken)
        {
            var validationResult = await ValidationHelper.ValidateAsync(_createReservationValidator, dto, cancellationToken);
            if (validationResult != null)
                return validationResult;

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

        [HttpGet("me/trip/{tripId:int}")]
        [Authorize]
        public async Task<IActionResult> GetMyReservationForTrip([FromRoute] int tripId, CancellationToken cancellationToken)
        {
            string? userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            var keyDto = new ReservationKeyDto { TripId = tripId, UserId = int.Parse(userId) };
            var validationResult = await ValidationHelper.ValidateAsync(_reservationKeyValidator, keyDto, cancellationToken);
            if (validationResult != null)
                return validationResult;

            try
            {
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
            var keyDto = new ReservationKeyDto { TripId = tripId, UserId = userId };
            var validationResult = await ValidationHelper.ValidateAsync(_reservationKeyValidator, keyDto, cancellationToken);
            if (validationResult != null)
                return validationResult;

            try
            {
                Application.Entities.Reservation reservation = await reservationService.GetReservationAsync(keyDto, cancellationToken);
                return Ok(reservation);
            }
            catch (ServiceNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpDelete("me/trip/{tripId:int}")]
        [Authorize]
        public async Task<IActionResult> DeleteMyReservation([FromRoute] int tripId, CancellationToken cancellationToken)
        {
            string? userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            var keyDto = new ReservationKeyDto { TripId = tripId, UserId = int.Parse(userId) };
            var validationResult = await ValidationHelper.ValidateAsync(_reservationKeyValidator, keyDto, cancellationToken);
            if (validationResult != null)
                return validationResult;

            try
            {
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
            var keyDto = new ReservationKeyDto { TripId = tripId, UserId = userId };
            var validationResult = await ValidationHelper.ValidateAsync(_reservationKeyValidator, keyDto, cancellationToken);
            if (validationResult != null)
                return validationResult;

            try
            {
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
