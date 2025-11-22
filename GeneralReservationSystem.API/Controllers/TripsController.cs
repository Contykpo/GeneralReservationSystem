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
    [Route("api/trips")]
    [ApiController]
    public class TripsController(ITripService tripService, IValidator<PagedSearchRequestDto> pagedSearchValidator, IValidator<CreateTripDto> createTripValidator, IValidator<UpdateTripDto> updateTripValidator, IValidator<TripKeyDto> tripKeyValidator) : ControllerBase
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
            IActionResult? validationResult = await ValidationHelper.ValidateAsync(pagedSearchValidator, searchDto, cancellationToken);
            if (validationResult != null)
            {
                return validationResult;
            }

            PagedResult<TripWithDetailsDto> result = await tripService.SearchTripsAsync(searchDto, cancellationToken);
            return Ok(result);
        }

        [HttpGet("{tripId:int}")]
        public async Task<IActionResult> GetTrip([FromRoute] int tripId, CancellationToken cancellationToken)
        {
            TripKeyDto keyDto = new() { TripId = tripId };
            IActionResult? validationResult = await ValidationHelper.ValidateAsync(tripKeyValidator, keyDto, cancellationToken);
            if (validationResult != null)
            {
                return validationResult;
            }

            try
            {
                Trip trip = await tripService.GetTripAsync(keyDto, cancellationToken);
                return Ok(trip);
            }
            catch (ServiceNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpGet("{tripId:int}/details")]
        public async Task<IActionResult> GetTripWithDetails([FromRoute] int tripId, CancellationToken cancellationToken)
        {
            TripKeyDto keyDto = new() { TripId = tripId };
            IActionResult? validationResult = await ValidationHelper.ValidateAsync(tripKeyValidator, keyDto, cancellationToken);
            if (validationResult != null)
            {
                return validationResult;
            }

            try
            {
                TripWithDetailsDto trip = await tripService.GetTripWithDetailsAsync(keyDto, cancellationToken);
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
            IActionResult? validationResult = await ValidationHelper.ValidateAsync(createTripValidator, dto, cancellationToken);
            if (validationResult != null)
            {
                return validationResult;
            }

            try
            {
                Trip trip = await tripService.CreateTripAsync(dto, cancellationToken);
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
            dto.TripId = tripId;
            IActionResult? validationResult = await ValidationHelper.ValidateAsync(updateTripValidator, dto, cancellationToken);
            if (validationResult != null)
            {
                return validationResult;
            }

            try
            {
                Trip trip = await tripService.UpdateTripAsync(dto, cancellationToken);
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
            TripKeyDto keyDto = new() { TripId = tripId };
            IActionResult? validationResult = await ValidationHelper.ValidateAsync(tripKeyValidator, keyDto, cancellationToken);
            if (validationResult != null)
            {
                return validationResult;
            }

            try
            {
                await tripService.DeleteTripAsync(keyDto, cancellationToken);
                return NoContent();
            }
            catch (ServiceNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpGet("{tripId:int}/free-seats")]
        public async Task<IActionResult> GetFreeSeats([FromRoute] int tripId, CancellationToken cancellationToken)
        {
            TripKeyDto keyDto = new() { TripId = tripId };
            IActionResult? validationResult = await ValidationHelper.ValidateAsync(tripKeyValidator, keyDto, cancellationToken);
            if (validationResult != null)
            {
                return validationResult;
            }

            try
            {
                IEnumerable<int> freeSeats = await tripService.GetFreeSeatsAsync(keyDto, cancellationToken);
                return Ok(freeSeats);
            }
            catch (ServiceNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ServiceException ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
