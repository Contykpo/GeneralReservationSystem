using FluentValidation;
using GeneralReservationSystem.Application.DTOs;

namespace GeneralReservationSystem.Application.Validators
{
    public class CreateTripDtoValidator : AppValidator<CreateTripDto>
    {
        public CreateTripDtoValidator()
        {
            RuleFor(x => x.DepartureStationId)
                .GreaterThan(0).WithMessage("El Id de salida debe ser un número positivo.");
            RuleFor(x => x.DepartureTime)
                .NotEmpty().WithMessage("La fecha de salida es obligatoria.");
            RuleFor(x => x.ArrivalStationId)
                .GreaterThan(0).WithMessage("El Id de destino debe ser un número positivo.");
            RuleFor(x => x.ArrivalTime)
                .NotEmpty().WithMessage("La fecha de llegada es obligatoria.");
            RuleFor(x => x.AvailableSeats)
                .GreaterThan(0).WithMessage("El número de asientos disponibles debe ser un número positivo.");
        }
    }

    public class UpdateTripDtoValidator : AppValidator<UpdateTripDto>
    {
        public UpdateTripDtoValidator()
        {
            RuleFor(x => x.TripId)
                .GreaterThan(0).WithMessage("El identificador es obligatorio.");
            RuleFor(x => x.DepartureStationId)
                .GreaterThan(0).When(x => x.DepartureStationId.HasValue)
                .WithMessage("El Id de salida debe ser un número positivo.");
            RuleFor(x => x.ArrivalStationId)
                .GreaterThan(0).When(x => x.ArrivalStationId.HasValue)
                .WithMessage("El Id de destino debe ser un número positivo.");
            RuleFor(x => x.AvailableSeats)
                .GreaterThan(0).When(x => x.AvailableSeats.HasValue)
                .WithMessage("El número de asientos disponibles debe ser un número positivo.");
        }
    }

    public class TripKeyDtoValidator : AppValidator<TripKeyDto>
    {
        public TripKeyDtoValidator()
        {
            RuleFor(x => x.TripId)
                .GreaterThan(0).WithMessage("El Id de viaje debe ser un número positivo.");
        }
    }
}