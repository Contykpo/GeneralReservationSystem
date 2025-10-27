using FluentValidation;
using GeneralReservationSystem.Application.DTOs;

namespace GeneralReservationSystem.Application.Validators
{
    public class CreateTripDtoValidator : AppValidator<CreateTripDto>
    {
        public CreateTripDtoValidator()
        {
            RuleFor(x => x.DepartureStationId)
                .GreaterThan(0).WithMessage("Seleccione una estación de salida válida.");
            RuleFor(x => x.DepartureTime)
                .NotEmpty().WithMessage("La fecha de salida es obligatoria.");
            RuleFor(x => x.ArrivalStationId)
                .GreaterThan(0).WithMessage("Seleccione una estación de llegada válida.");
            RuleFor(x => x.ArrivalStationId)
                .NotEqual(x => x.DepartureStationId)
                .WithMessage("La estación de llegada debe ser diferente a la de salida.");
            RuleFor(x => x.ArrivalTime)
                .NotEmpty().WithMessage("La fecha de llegada es obligatoria.");
            RuleFor(x => x.AvailableSeats)
                .GreaterThan(0).WithMessage("El número de asientos disponibles debe ser un número positivo.");
            RuleFor(x => x)
                .Must(x => x.ArrivalTime > x.DepartureTime)
                .WithMessage("La fecha/hora de llegada debe ser posterior a la salida.");
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
                .WithMessage("Seleccione una estación de salida válida.");
            RuleFor(x => x.ArrivalStationId)
                .GreaterThan(0).When(x => x.ArrivalStationId.HasValue)
                .WithMessage("Seleccione una estación de llegada válida.");
            RuleFor(x => x.ArrivalStationId)
                .NotEqual(x => x.DepartureStationId).When(x => x.ArrivalStationId.HasValue && x.DepartureStationId.HasValue)
                .WithMessage("La estación de llegada debe ser diferente a la de salida.");
            RuleFor(x => x.AvailableSeats)
                .GreaterThan(0).When(x => x.AvailableSeats.HasValue)
                .WithMessage("El número de asientos disponibles debe ser un número positivo.");
            RuleFor(x => x)
                .Must(x => x.ArrivalTime > x.DepartureTime).When(x => x.ArrivalTime.HasValue && x.DepartureTime.HasValue)
                .WithMessage("La fecha/hora de llegada debe ser posterior a la salida.");
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