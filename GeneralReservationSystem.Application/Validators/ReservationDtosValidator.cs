using FluentValidation;
using GeneralReservationSystem.Application.DTOs;

namespace GeneralReservationSystem.Application.Validators
{
    public class CreateReservationDtoValidator : AppValidator<CreateReservationDto>
    {
        public CreateReservationDtoValidator()
        {
            RuleFor(x => x.TripId)
                .GreaterThan(0).WithMessage("El Id de viaje debe ser un número positivo.");
            RuleFor(x => x.Seat)
                .GreaterThan(0).WithMessage("El número de asiento debe ser un número positivo.");
        }
    }

    public class ReservationKeyDtoValidator : AppValidator<ReservationKeyDto>
    {
        public ReservationKeyDtoValidator()
        {
            RuleFor(x => x.TripId)
                .GreaterThan(0).WithMessage("El Id de viaje debe ser un número positivo.");
            RuleFor(x => x.UserId)
                .GreaterThan(0).WithMessage("El Id de usuario debe ser un número positivo.");
        }
    }
}