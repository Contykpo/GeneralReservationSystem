using FluentValidation;
using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Services.Interfaces;
using GeneralReservationSystem.Server.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static GeneralReservationSystem.Application.Constants;

namespace GeneralReservationSystem.Server.Controllers
{
    [Route("api/trips")]
    [ApiController]
    public class TripsController(
        ITripService tripService,
        IValidator<PagedSearchRequestDto> pagedSearchValidator,
        IValidator<CreateTripDto> createTripValidator,
        IValidator<TripKeyDto> tripKeyValidator) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAllTrips(CancellationToken cancellationToken)
        {
            IEnumerable<Trip> trips = await tripService.GetAllTripsAsync(cancellationToken);
            return Ok(trips);
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchTrips(CancellationToken cancellationToken)
        {
            PagedSearchRequestDto searchDto = new();
            searchDto.PopulateFromQuery(Request.Query);
            await ValidateAsync(pagedSearchValidator, searchDto, cancellationToken);

            PagedResult<TripWithDetailsDto> result = await tripService.SearchTripsAsync(searchDto, cancellationToken);
            return Ok(result);
        }

        [HttpGet("{tripId:int}")]
        public async Task<IActionResult> GetTrip([FromRoute] int tripId, CancellationToken cancellationToken)
        {
            TripKeyDto keyDto = new() { TripId = tripId };
            await ValidateAsync(tripKeyValidator, keyDto, cancellationToken);
            Trip trip = await tripService.GetTripAsync(keyDto, cancellationToken);
            return Ok(trip);
        }

        [HttpGet("{tripId:int}/details")]
        public async Task<IActionResult> GetTripWithDetails([FromRoute] int tripId, CancellationToken cancellationToken)
        {
            TripKeyDto keyDto = new() { TripId = tripId };
            await ValidateAsync(tripKeyValidator, keyDto, cancellationToken);
            TripWithDetailsDto trip = await tripService.GetTripWithDetailsAsync(keyDto, cancellationToken);
            return Ok(trip);
        }

        [HttpPost]
        [Authorize(Roles = AdminRoleName)]
        public async Task<IActionResult> CreateTrip([FromBody] CreateTripDto dto, CancellationToken cancellationToken)
        {
            await ValidateAsync(createTripValidator, dto, cancellationToken);
            Trip trip = await tripService.CreateTripAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetTrip), new { tripId = trip.TripId }, trip);
        }

        [HttpDelete("{tripId:int}")]
        [Authorize(Roles = AdminRoleName)]
        public async Task<IActionResult> DeleteTrip([FromRoute] int tripId, CancellationToken cancellationToken)
        {
            TripKeyDto keyDto = new() { TripId = tripId };
            await ValidateAsync(tripKeyValidator, keyDto, cancellationToken);
            await tripService.DeleteTripAsync(keyDto, cancellationToken);
            return NoContent();
        }

        [HttpGet("{tripId:int}/free-seats")]
        public async Task<IActionResult> GetFreeSeats([FromRoute] int tripId, CancellationToken cancellationToken)
        {
            TripKeyDto keyDto = new() { TripId = tripId };
            await ValidateAsync(tripKeyValidator, keyDto, cancellationToken);
            IEnumerable<int> freeSeats = await tripService.GetFreeSeatsAsync(keyDto, cancellationToken);
            return Ok(freeSeats);
        }
    }
}
