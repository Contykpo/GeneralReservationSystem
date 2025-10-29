using FluentValidation;
using GeneralReservationSystem.API.Helpers;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.DTOs.Authentication;
using GeneralReservationSystem.Application.Entities;
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
    public class ReservationsController(IReservationService reservationService, IValidator<PagedSearchRequestDto> pagedSearchValidator, IValidator<CreateReservationDto> createReservationValidator, IValidator<ReservationKeyDto> reservationKeyValidator, IValidator<TripUserReservationsKeyDto> tripUserReservationsKeyValidator, IValidator<UserKeyDto> userKeyValidator) : ControllerBase
    {
        [HttpPost("search")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SearchReservations([FromBody] PagedSearchRequestDto searchDto, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = await ValidationHelper.ValidateAsync(pagedSearchValidator, searchDto, cancellationToken);
            if (validationResult != null)
            {
                return validationResult;
            }

            Application.Common.PagedResult<ReservationDetailsDto> result = await reservationService.SearchReservationsAsync(searchDto, cancellationToken);
            return Ok(result);
        }

        [HttpPost("search/{userId:int}")]
        public async Task<IActionResult> SearchUserReservations([FromBody] PagedSearchRequestDto searchDto, [FromRoute] int userId, CancellationToken cancellationToken)
        {
            IActionResult? paginationValidationResult = await ValidationHelper.ValidateAsync(pagedSearchValidator, searchDto, cancellationToken);
            if (paginationValidationResult != null)
            {
                return paginationValidationResult;
            }
            var keyDto = new UserKeyDto { UserId = userId };
            IActionResult? userValidationResult = await ValidationHelper.ValidateAsync(userKeyValidator, keyDto, cancellationToken);
            if (userValidationResult != null)
            {
                return userValidationResult;
            }

            string? currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized();
            }

            if (int.Parse(currentUserId) != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            Application.Common.PagedResult<UserReservationDetailsDto> result = await reservationService.SearchUserReservationsAsync(keyDto, searchDto, cancellationToken);
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

            IEnumerable<UserReservationDetailsDto> reservations = await reservationService.GetUserReservationsAsync(new UserKeyDto{ UserId = int.Parse(userId) }, cancellationToken);
            return Ok(reservations);
        }

        [HttpGet("user/{userId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUserReservations([FromRoute] int userId, CancellationToken cancellationToken)
        {
            var keyDto = new UserKeyDto { UserId = userId };
            IActionResult? validationResult = await ValidationHelper.ValidateAsync(userKeyValidator, keyDto, cancellationToken);
            if (validationResult != null)
            {
                return validationResult;
            }

            IEnumerable<UserReservationDetailsDto> reservations = await reservationService.GetUserReservationsAsync(keyDto, cancellationToken);
            return Ok(reservations);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateReservation([FromBody] CreateReservationDto dto, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = await ValidationHelper.ValidateAsync(createReservationValidator, dto, cancellationToken);
            if (validationResult != null)
            {
                return validationResult;
            }

            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            if (dto.UserId != int.Parse(userId) && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            try
            {
                _ = await reservationService.CreateReservationAsync(dto, cancellationToken);
                return CreatedAtAction(nameof(GetReservation), new { tripId = dto.TripId, seat = dto.Seat }, new { message = "Reserva creada exitosamente" });
            }
            catch (ServiceBusinessException ex)
            {
                return Conflict(new { error = ex.Message });
            }
        }

        [HttpPost("me")]
        [Authorize]
        public async Task<IActionResult> CreateReservationForMyself([FromBody] ReservationKeyDto keyDto, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = await ValidationHelper.ValidateAsync(reservationKeyValidator, keyDto, cancellationToken);
            if (validationResult != null)
            {
                return validationResult;
            }

            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            CreateReservationDto dto = new()
            {
                TripId = keyDto.TripId,
                Seat = keyDto.Seat,
                UserId = int.Parse(userId)
            };

            try
            {
                _ = await reservationService.CreateReservationAsync(dto, cancellationToken);
                return CreatedAtAction(nameof(GetReservation), new { tripId = dto.TripId, seat = dto.Seat }, new { message = "Reserva creada exitosamente" });
            }
            catch (ServiceBusinessException ex)
            {
                return Conflict(new { error = ex.Message });
            }
        }

        [HttpGet("me/trip/{tripId:int}")]
        [Authorize]
        public async Task<IActionResult> GetMyReservationsForTrip([FromRoute] int tripId, CancellationToken cancellationToken)
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            TripUserReservationsKeyDto keyDto = new() { TripId = tripId, UserId = int.Parse(userId) };
            IActionResult? validationResult = await ValidationHelper.ValidateAsync(tripUserReservationsKeyValidator, keyDto, cancellationToken);
            if (validationResult != null)
            {
                return validationResult;
            }

            try
            {
                IEnumerable<UserReservationDetailsDto> reservations = await reservationService.GetTripUserReservationsAsync(keyDto, cancellationToken);
                return Ok(reservations);
            }
            catch (ServiceNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpGet("{tripId:int}/{seat:int}")]
        [Authorize]
        public async Task<IActionResult> GetReservation([FromRoute] int tripId, [FromRoute] int seat, CancellationToken cancellationToken)
        {
            ReservationKeyDto keyDto = new() { TripId = tripId, Seat = seat };
            IActionResult? validationResult = await ValidationHelper.ValidateAsync(reservationKeyValidator, keyDto, cancellationToken);
            if (validationResult != null)
            {
                return validationResult;
            }

            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            try
            {
                Reservation reservation = await reservationService.GetReservationAsync(keyDto, cancellationToken);

                return reservation.UserId != int.Parse(userId) && !User.IsInRole("Admin") ? Forbid() : Ok(reservation);
            }
            catch (ServiceNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpDelete("{tripId:int}/{seat:int}")]
        [Authorize]
        public async Task<IActionResult> DeleteReservation([FromRoute] int tripId, [FromRoute] int seat, CancellationToken cancellationToken)
        {
            ReservationKeyDto keyDto = new() { TripId = tripId, Seat = seat };
            IActionResult? validationResult = await ValidationHelper.ValidateAsync(reservationKeyValidator, keyDto, cancellationToken);
            if (validationResult != null)
            {
                return validationResult;
            }

            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            try
            {
                Reservation reservation = await reservationService.GetReservationAsync(keyDto, cancellationToken);

                if (reservation.UserId != int.Parse(userId) && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }

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
