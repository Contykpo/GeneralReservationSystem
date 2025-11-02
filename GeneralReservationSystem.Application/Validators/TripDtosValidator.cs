using FluentValidation;
using GeneralReservationSystem.Application.DTOs;

namespace GeneralReservationSystem.Application.Validators
{
    public class CreateTripDtoValidator : AppValidator<CreateTripDto>
    {
        public CreateTripDtoValidator()
        {
            _ = RuleFor(x => x.DepartureStationId)
                .GreaterThan(0).WithMessage("Seleccione una estaci�n de salida v�lida.");
            _ = RuleFor(x => x.DepartureTime)
                .NotEmpty().WithMessage("La fecha de salida es obligatoria.");
            _ = RuleFor(x => x.ArrivalStationId)
                .GreaterThan(0).WithMessage("Seleccione una estaci�n de llegada v�lida.");
            _ = RuleFor(x => x.ArrivalStationId)
                .NotEqual(x => x.DepartureStationId)
                .WithMessage("La estaci�n de llegada debe ser diferente a la de salida.");
            _ = RuleFor(x => x.ArrivalTime)
                .NotEmpty().WithMessage("La fecha de llegada es obligatoria.");
            _ = RuleFor(x => x.AvailableSeats)
                .GreaterThan(0).WithMessage("El n�mero de asientos disponibles debe ser un n�mero positivo.");
            _ = RuleFor(x => x.DepartureTime)
                .Must((dto, departureTime) => dto.ArrivalTime > departureTime)
                .WithMessage("La fecha/hora de llegada debe ser posterior a la salida.");
        }
    }

    public class UpdateTripDtoValidator : AppValidator<UpdateTripDto>
    {
        public UpdateTripDtoValidator()
        {
            _ = RuleFor(x => x.TripId)
                .GreaterThan(0).WithMessage("El identificador es obligatorio.");
            _ = RuleFor(x => x.DepartureStationId)
                .GreaterThan(0).When(x => x.DepartureStationId.HasValue)
                .WithMessage("Seleccione una estaci�n de salida v�lida.");
            _ = RuleFor(x => x.ArrivalStationId)
                .GreaterThan(0).When(x => x.ArrivalStationId.HasValue)
                .WithMessage("Seleccione una estaci�n de llegada v�lida.");
            _ = RuleFor(x => x.ArrivalStationId)
                .NotEqual(x => x.DepartureStationId).When(x => x.ArrivalStationId.HasValue && x.DepartureStationId.HasValue)
                .WithMessage("La estaci�n de llegada debe ser diferente a la de salida.");
            _ = RuleFor(x => x.AvailableSeats)
                .GreaterThan(0).When(x => x.AvailableSeats.HasValue)
                .WithMessage("El n�mero de asientos disponibles debe ser un n�mero positivo.");
            _ = RuleFor(x => x.DepartureTime)
                .Must((dto, departureTime) => dto.ArrivalTime > departureTime)
                .When(x => x.ArrivalTime.HasValue && x.DepartureTime.HasValue)
                .WithMessage("La fecha/hora de llegada debe ser posterior a la salida.");
        }
    }

    public class TripKeyDtoValidator : AppValidator<TripKeyDto>
    {
        public TripKeyDtoValidator()
        {
            _ = RuleFor(x => x.TripId)
                .GreaterThan(0).WithMessage("El Id de viaje debe ser un n�mero positivo.");
        }
    }
}