using FluentValidation;
using GeneralReservationSystem.Application.Validators;
using GeneralReservationSystem.Application.Validators.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace GeneralReservationSystem.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddFluentValidators(this IServiceCollection services)
        {
            _ = services.AddValidatorsFromAssemblyContaining<RegisterUserDtoValidator>();
            _ = services.AddValidatorsFromAssemblyContaining<LoginDtoValidator>();
            _ = services.AddValidatorsFromAssemblyContaining<UserKeyDtoValidator>();
            _ = services.AddValidatorsFromAssemblyContaining<UpdateUserDtoValidator>();

            _ = services.AddValidatorsFromAssemblyContaining<PagedSearchRequestDtoValidator>();

            _ = services.AddValidatorsFromAssemblyContaining<CreateReservationDtoValidator>();
            _ = services.AddValidatorsFromAssemblyContaining<ReservationKeyDtoValidator>();

            _ = services.AddValidatorsFromAssemblyContaining<CreateStationDtoValidator>();
            _ = services.AddValidatorsFromAssemblyContaining<UpdateStationDtoValidator>();
            _ = services.AddValidatorsFromAssemblyContaining<StationKeyDtoValidator>();

            _ = services.AddValidatorsFromAssemblyContaining<CreateTripDtoValidator>();
            _ = services.AddValidatorsFromAssemblyContaining<UpdateTripDtoValidator>();
            _ = services.AddValidatorsFromAssemblyContaining<TripKeyDtoValidator>();

            return services;
        }
    }
}
