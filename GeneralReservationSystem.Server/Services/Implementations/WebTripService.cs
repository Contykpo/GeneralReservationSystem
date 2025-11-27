using FluentValidation;
using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Services.Interfaces;
using GeneralReservationSystem.Web.Client.Services.Interfaces;

namespace GeneralReservationSystem.Server.Services.Implementations
{
    public class WebTripService(
        ITripService tripService,
        IHttpContextAccessor httpContextAccessor,
        IValidator<PagedSearchRequestDto> pagedSearchValidator,
        IValidator<CreateTripDto> createTripValidator,
        IValidator<TripKeyDto> tripKeyValidator) : WebServiceBase(httpContextAccessor), IClientTripService
    {
        public async Task<Trip> GetTripAsync(TripKeyDto keyDto, CancellationToken cancellationToken = default)
        {
            await ValidateAsync(tripKeyValidator, keyDto, cancellationToken);
            return await tripService.GetTripAsync(keyDto, cancellationToken);
        }

        public async Task<TripWithDetailsDto> GetTripWithDetailsAsync(TripKeyDto keyDto, CancellationToken cancellationToken = default)
        {
            await ValidateAsync(tripKeyValidator, keyDto, cancellationToken);
            return await tripService.GetTripWithDetailsAsync(keyDto, cancellationToken);
        }

        public async Task<IEnumerable<Trip>> GetAllTripsAsync(CancellationToken cancellationToken = default)
        {
            return await tripService.GetAllTripsAsync(cancellationToken);
        }

        public async Task<PagedResult<TripWithDetailsDto>> SearchTripsAsync(PagedSearchRequestDto searchDto, CancellationToken cancellationToken = default)
        {
            await ValidateAsync(pagedSearchValidator, searchDto, cancellationToken);
            return await tripService.SearchTripsAsync(searchDto, cancellationToken);
        }

        public async Task<Trip> CreateTripAsync(CreateTripDto dto, CancellationToken cancellationToken = default)
        {
            EnsureAdmin();
            await ValidateAsync(createTripValidator, dto, cancellationToken);
            return await tripService.CreateTripAsync(dto, cancellationToken);
        }

        public async Task DeleteTripAsync(TripKeyDto keyDto, CancellationToken cancellationToken = default)
        {
            EnsureAdmin();
            await ValidateAsync(tripKeyValidator, keyDto, cancellationToken);
            await tripService.DeleteTripAsync(keyDto, cancellationToken);
        }

        public async Task<IEnumerable<int>> GetFreeSeatsAsync(TripKeyDto keyDto, CancellationToken cancellationToken = default)
        {
            await ValidateAsync(tripKeyValidator, keyDto, cancellationToken);
            return await tripService.GetFreeSeatsAsync(keyDto, cancellationToken);
        }
    }
}
