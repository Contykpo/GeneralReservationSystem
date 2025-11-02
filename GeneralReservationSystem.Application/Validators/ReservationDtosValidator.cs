using FluentValidation;
using GeneralReservationSystem.Application.DTOs;

namespace GeneralReservationSystem.Application.Validators
{
    public class CreateReservationDtoValidator : AppValidator<CreateReservationDto>
    {
        public CreateReservationDtoValidator()
        {
            _ = RuleFor(x => x.TripId)
                .GreaterThan(0).WithMessage("El Id de viaje debe ser un n�mero positivo.");
            _ = RuleFor(x => x.UserId)
                .GreaterThan(0).WithMessage("El Id de usuario debe ser un n�mero positivo.");
            _ = RuleFor(x => x.Seat)
                .GreaterThan(0).WithMessage("El n�mero de asiento debe ser un n�mero positivo.");
        }
    }

    public class ReservationKeyDtoValidator : AppValidator<ReservationKeyDto>
    {
        public ReservationKeyDtoValidator()
        {
            _ = RuleFor(x => x.TripId)
                .GreaterThan(0).WithMessage("El Id de viaje debe ser un n�mero positivo.");
            _ = RuleFor(x => x.Seat)
                .GreaterThan(0).WithMessage("El n�mero de asiento debe ser un n�mero positivo.");
        }
    }
}