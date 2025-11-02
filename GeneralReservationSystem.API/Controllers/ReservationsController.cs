using FluentValidation;
using GeneralReservationSystem.API.Helpers;
using GeneralReservationSystem.Application.Common;
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
    public class ReservationsController(IReservationService reservationService, IValidator<PagedSearchRequestDto> pagedSearchValidator, IValidator<CreateReservationDto> createReservationValidator, IValidator<ReservationKeyDto> reservationKeyValidator, IValidator<UserKeyDto> userKeyValidator) : ControllerBase
    {
        [HttpGet("search")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SearchReservations(CancellationToken cancellationToken)
        {
            PagedSearchRequestDto searchDto = new();
            searchDto.PopulateFromQuery(Request.Query);
            IActionResult? validationResult = await ValidationHelper.ValidateAsync(pagedSearchValidator, searchDto, cancellationToken);
            if (validationResult != null)
            {
                return validationResult;
            }
            PagedResult<ReservationDetailsDto> result = await reservationService.SearchReservationsAsync(searchDto, cancellationToken);
            return Ok(result);
        }

        [HttpGet("search/{userId:int}")]
        public async Task<IActionResult> SearchUserReservations([FromRoute] int userId, CancellationToken cancellationToken)
        {
            string? currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized();
            }
            PagedSearchRequestDto searchDto = new();
            searchDto.PopulateFromQuery(Request.Query);
            IActionResult? paginationValidationResult = await ValidationHelper.ValidateAsync(pagedSearchValidator, searchDto, cancellationToken);
            if (paginationValidationResult != null)
            {
                return paginationValidationResult;
            }
            UserKeyDto keyDto = new() { UserId = userId };
            IActionResult? userValidationResult = await ValidationHelper.ValidateAsync(userKeyValidator, keyDto, cancellationToken);
            if (userValidationResult != null)
            {
                return userValidationResult;
            }
            if (int.Parse(currentUserId) != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }
            PagedResult<UserReservationDetailsDto> result = await reservationService.SearchUserReservationsAsync(keyDto, searchDto, cancellationToken);
            return Ok(result);
        }

        [HttpGet("search/me")]
        public async Task<IActionResult> SearchCurrentUserReservations(CancellationToken cancellationToken)
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            PagedSearchRequestDto searchDto = new();
            searchDto.PopulateFromQuery(Request.Query);
            IActionResult? paginationValidationResult = await ValidationHelper.ValidateAsync(pagedSearchValidator, searchDto, cancellationToken);
            if (paginationValidationResult != null)
            {
                return paginationValidationResult;
            }
            PagedResult<UserReservationDetailsDto> result = await reservationService.SearchUserReservationsAsync(new UserKeyDto { UserId = int.Parse(userId) }, searchDto, cancellationToken);
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

            IEnumerable<UserReservationDetailsDto> reservations = await reservationService.GetUserReservationsAsync(new UserKeyDto { UserId = int.Parse(userId) }, cancellationToken);
            return Ok(reservations);
        }

        [HttpGet("user/{userId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUserReservations([FromRoute] int userId, CancellationToken cancellationToken)
        {
            UserKeyDto keyDto = new() { UserId = userId };
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
