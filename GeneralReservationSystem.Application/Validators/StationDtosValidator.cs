using FluentValidation;
using GeneralReservationSystem.Application.DTOs;

namespace GeneralReservationSystem.Application.Validators
{
    public class CreateStationDtoValidator : AppValidator<CreateStationDto>
    {
        public CreateStationDtoValidator()
        {
            _ = RuleFor(x => x.StationName)
                .NotEmpty().WithMessage("El nombre de la estación es obligatorio.")
                .Length(2, 100).WithMessage("El nombre de la estación debe tener entre 2 y 100 caracteres.")
                .Matches(@"^[\p{L}\s'-]+$").WithMessage("El nombre de la estación solo puede contener letras, espacios, apóstrofes o guiones.");
            _ = RuleFor(x => x.City)
                .NotEmpty().WithMessage("La ciudad es obligatoria.")
                .Length(2, 50).WithMessage("La ciudad debe tener entre 2 y 50 caracteres.")
                .Matches(@"^[\p{L}\s'-]+$").WithMessage("La ciudad solo puede contener letras, espacios, apóstrofes o guiones.");
            _ = RuleFor(x => x.Region)
                .NotEmpty().WithMessage("La región es obligatoria.")
                .Length(2, 50).WithMessage("La región debe tener entre 2 y 50 caracteres.")
                .Matches(@"^[\p{L}\s'-]+$").WithMessage("La región solo puede contener letras, espacios, apóstrofes o guiones.");
            _ = RuleFor(x => x.Country)
                .NotEmpty().WithMessage("El país es obligatorio.")
                .Length(2, 50).WithMessage("El país debe tener entre 2 y 50 caracteres.")
                .Matches(@"^[\p{L}\s'-]+$").WithMessage("El país solo puede contener letras, espacios, apóstrofes o guiones.");
        }
    }

    public class UpdateStationDtoValidator : AppValidator<UpdateStationDto>
    {
        public UpdateStationDtoValidator()
        {
            _ = RuleFor(x => x.StationId)
                .GreaterThan(0).WithMessage("El identificador es obligatorio.");
            _ = RuleFor(x => x.StationName)
                .Length(2, 100).When(x => x.StationName != null)
                .WithMessage("El nombre de la estación debe tener entre 2 y 100 caracteres.")
                .Matches(@"^[\p{L}\s'-]+$").When(x => x.StationName != null)
                .WithMessage("El nombre de la estación solo puede contener letras, espacios, apóstrofes o guiones.");
            _ = RuleFor(x => x.City)
                .Length(2, 50).When(x => x.City != null)
                .WithMessage("La ciudad debe tener entre 2 y 50 caracteres.")
                .Matches(@"^[\p{L}\s'-]+$").When(x => x.City != null)
                .WithMessage("La ciudad solo puede contener letras, espacios, apóstrofes o guiones.");
            _ = RuleFor(x => x.Region)
                .Length(2, 50).When(x => x.Region != null)
                .WithMessage("La región debe tener entre 2 y 50 caracteres.")
                .Matches(@"^[\p{L}\s'-]+$").When(x => x.Region != null)
                .WithMessage("La región solo puede contener letras, espacios, apóstrofes o guiones.");
            _ = RuleFor(x => x.Country)
                .Length(2, 50).When(x => x.Country != null)
                .WithMessage("El país debe tener entre 2 y 50 caracteres.")
                .Matches(@"^[\p{L}\s'-]+$").When(x => x.Country != null)
                .WithMessage("El país solo puede contener letras, espacios, apóstrofes o guiones.");
        }
    }

    public class StationKeyDtoValidator : AppValidator<StationKeyDto>
    {
        public StationKeyDtoValidator()
        {
            _ = RuleFor(x => x.StationId)
                .GreaterThan(0).WithMessage("El Id de estación debe ser un número positivo.");
        }
    }
}