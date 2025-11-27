using FluentValidation;
using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.DTOs.Authentication;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Services.Interfaces;
using GeneralReservationSystem.Server.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static GeneralReservationSystem.Application.Constants;

namespace GeneralReservationSystem.Server.Controllers
{
    [Route("api/reservations")]
    [ApiController]
    public class ReservationsController(
        IReservationService reservationService,
        IValidator<PagedSearchRequestDto> pagedSearchValidator,
        IValidator<CreateReservationDto> createReservationValidator,
        IValidator<ReservationKeyDto> reservationKeyValidator,
        IValidator<UserKeyDto> userKeyValidator) : ControllerBase
    {
        [HttpGet("search")]
        [Authorize(Roles = AdminRoleName)]
        public async Task<IActionResult> SearchReservations(CancellationToken cancellationToken)
        {
            PagedSearchRequestDto searchDto = new();
            searchDto.PopulateFromQuery(Request.Query);
            await ValidateAsync(pagedSearchValidator, searchDto, cancellationToken);
            PagedResult<ReservationDetailsDto> result = await reservationService.SearchReservationsAsync(searchDto, cancellationToken);
            return Ok(result);
        }

        [HttpGet("search/{userId:int}")]
        [Authorize]
        public async Task<IActionResult> SearchUserReservations([FromRoute] int userId, CancellationToken cancellationToken)
        {
            if (!IsOwnerOrAdmin(userId))
            {
                return Forbid();
            }
            PagedSearchRequestDto searchDto = new();
            searchDto.PopulateFromQuery(Request.Query);
            await ValidateAsync(pagedSearchValidator, searchDto, cancellationToken);
            UserKeyDto keyDto = new() { UserId = userId };
            await ValidateAsync(userKeyValidator, keyDto, cancellationToken);
            PagedResult<UserReservationDetailsDto> result = await reservationService.SearchUserReservationsAsync(keyDto, searchDto, cancellationToken);
            return Ok(result);
        }

        [HttpGet("search/me")]
        [Authorize]
        public async Task<IActionResult> SearchCurrentUserReservations(CancellationToken cancellationToken)
        {
            PagedSearchRequestDto searchDto = new();
            searchDto.PopulateFromQuery(Request.Query);
            await ValidateAsync(pagedSearchValidator, searchDto, cancellationToken);
            PagedResult<UserReservationDetailsDto> result = await reservationService.SearchUserReservationsAsync(new UserKeyDto { UserId = (int)CurrentUserId! }, searchDto, cancellationToken);
            return Ok(result);
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMyReservations(CancellationToken cancellationToken)
        {
            IEnumerable<UserReservationDetailsDto> reservations = await reservationService.GetUserReservationsAsync(new UserKeyDto { UserId = (int)CurrentUserId! }, cancellationToken);
            return Ok(reservations);
        }

        [HttpGet("user/{userId:int}")]
        [Authorize(Roles = AdminRoleName)]
        public async Task<IActionResult> GetUserReservations([FromRoute] int userId, CancellationToken cancellationToken)
        {
            UserKeyDto keyDto = new() { UserId = userId };
            await ValidateAsync(userKeyValidator, keyDto, cancellationToken);

            IEnumerable<UserReservationDetailsDto> reservations = await reservationService.GetUserReservationsAsync(keyDto, cancellationToken);
            return Ok(reservations);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateReservation([FromBody] CreateReservationDto dto, CancellationToken cancellationToken)
        {
            if (!IsOwnerOrAdmin(dto.UserId))
            {
                return Forbid();
            }
            await ValidateAsync(createReservationValidator, dto, cancellationToken);
            _ = await reservationService.CreateReservationAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetReservation), new { tripId = dto.TripId, seat = dto.Seat }, new { message = "Reserva creada exitosamente" });
        }

        [HttpPost("me")]
        [Authorize]
        public async Task<IActionResult> CreateReservationForMyself([FromBody] ReservationKeyDto keyDto, CancellationToken cancellationToken)
        {
            await ValidateAsync(reservationKeyValidator, keyDto, cancellationToken);
            CreateReservationDto dto = new()
            {
                TripId = keyDto.TripId,
                Seat = keyDto.Seat,
                UserId = (int)CurrentUserId!
            };
            _ = await reservationService.CreateReservationAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetReservation), new { tripId = dto.TripId, seat = dto.Seat }, new { message = "Reserva creada exitosamente" });
        }

        [HttpGet("{tripId:int}/{seat:int}")]
        [Authorize]
        public async Task<IActionResult> GetReservation([FromRoute] int tripId, [FromRoute] int seat, CancellationToken cancellationToken)
        {
            ReservationKeyDto keyDto = new() { TripId = tripId, Seat = seat };
            await ValidateAsync(reservationKeyValidator, keyDto, cancellationToken);
            Reservation reservation = await reservationService.GetReservationAsync(keyDto, cancellationToken);
            return !IsOwnerOrAdmin(reservation.UserId) ? Forbid() : Ok(reservation);
        }

        [HttpDelete("{tripId:int}/{seat:int}")]
        [Authorize]
        public async Task<IActionResult> DeleteReservation([FromRoute] int tripId, [FromRoute] int seat, CancellationToken cancellationToken)
        {
            ReservationKeyDto keyDto = new() { TripId = tripId, Seat = seat };
            await ValidateAsync(reservationKeyValidator, keyDto, cancellationToken);
            Reservation reservation = await reservationService.GetReservationAsync(keyDto, cancellationToken);
            if (!IsOwnerOrAdmin(reservation.UserId))
            {
                return Forbid();
            }
            await reservationService.DeleteReservationAsync(keyDto, cancellationToken);
            return NoContent();
        }
    }
}
