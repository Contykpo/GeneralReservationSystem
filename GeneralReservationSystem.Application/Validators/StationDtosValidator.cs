using FluentValidation;
using GeneralReservationSystem.Application.DTOs;

namespace GeneralReservationSystem.Application.Validators
{
    public class CreateStationDtoValidator : AppValidator<CreateStationDto>
    {
        public CreateStationDtoValidator()
        {
            _ = RuleFor(x => x.StationName)
                .NotEmpty().WithMessage("El nombre de la estaci�n es obligatorio.")
                .Length(2, 100).WithMessage("El nombre de la estaci�n debe tener entre 2 y 100 caracteres.")
                .Matches(@"^[\p{L}\s'-]+$").WithMessage("El nombre de la estaci�n solo puede contener letras, espacios, ap�strofes o guiones.");
            _ = RuleFor(x => x.City)
                .NotEmpty().WithMessage("La ciudad es obligatoria.")
                .Length(2, 50).WithMessage("La ciudad debe tener entre 2 y 50 caracteres.")
                .Matches(@"^[\p{L}\s'-]+$").WithMessage("La ciudad solo puede contener letras, espacios, ap�strofes o guiones.");
            _ = RuleFor(x => x.Province)
                .NotEmpty().WithMessage("La provincia es obligatoria.")
                .Length(2, 50).WithMessage("La provincia debe tener entre 2 y 50 caracteres.")
                .Matches(@"^[\p{L}\s'-]+$").WithMessage("La provincia solo puede contener letras, espacios, ap�strofes o guiones.");
            _ = RuleFor(x => x.Country)
                .NotEmpty().WithMessage("El pa�s es obligatorio.")
                .Length(2, 50).WithMessage("El pa�s debe tener entre 2 y 50 caracteres.")
                .Matches(@"^[\p{L}\s'-]+$").WithMessage("El pa�s solo puede contener letras, espacios, ap�strofes o guiones.");
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
                .WithMessage("El nombre de la estaci�n debe tener entre 2 y 100 caracteres.")
                .Matches(@"^[\p{L}\s'-]+$").When(x => x.StationName != null)
                .WithMessage("El nombre de la estaci�n solo puede contener letras, espacios, ap�strofes o guiones.");
            _ = RuleFor(x => x.City)
                .Length(2, 50).When(x => x.City != null)
                .WithMessage("La ciudad debe tener entre 2 y 50 caracteres.")
                .Matches(@"^[\p{L}\s'-]+$").When(x => x.City != null)
                .WithMessage("La ciudad solo puede contener letras, espacios, ap�strofes o guiones.");
            _ = RuleFor(x => x.Province)
                .Length(2, 50).When(x => x.Province != null)
                .WithMessage("La provincia debe tener entre 2 y 50 caracteres.")
                .Matches(@"^[\p{L}\s'-]+$").When(x => x.Province != null)
                .WithMessage("La provincia solo puede contener letras, espacios, ap�strofes o guiones.");
            _ = RuleFor(x => x.Country)
                .Length(2, 50).When(x => x.Country != null)
                .WithMessage("El pa�s debe tener entre 2 y 50 caracteres.")
                .Matches(@"^[\p{L}\s'-]+$").When(x => x.Country != null)
                .WithMessage("El pa�s solo puede contener letras, espacios, ap�strofes o guiones.");
        }
    }

    public class StationKeyDtoValidator : AppValidator<StationKeyDto>
    {
        public StationKeyDtoValidator()
        {
            _ = RuleFor(x => x.StationId)
                .GreaterThan(0).WithMessage("El Id de estaci�n debe ser un n�mero positivo.");
        }
    }

    public class ImportStationDtoValidator : AppValidator<ImportStationDto>
    {
        public ImportStationDtoValidator()
        {
            _ = RuleFor(x => x.StationName)
                .NotEmpty().WithMessage("El nombre de la estaci�n es obligatorio.")
                .Length(2, 100).WithMessage("El nombre de la estaci�n debe tener entre 2 y 100 caracteres.")
                .Matches(@"^[\p{L}\s'-]+$").WithMessage("El nombre de la estaci�n solo puede contener letras, espacios, ap�strofes o guiones.");
            _ = RuleFor(x => x.City)
                .NotEmpty().WithMessage("La ciudad es obligatoria.")
                .Length(2, 50).WithMessage("La ciudad debe tener entre 2 y 50 caracteres.")
                .Matches(@"^[\p{L}\s'-]+$").WithMessage("La ciudad solo puede contener letras, espacios, ap�strofes o guiones.");
            _ = RuleFor(x => x.Province)
                .NotEmpty().WithMessage("La provincia es obligatoria.")
                .Length(2, 50).WithMessage("La provincia debe tener entre 2 y 50 caracteres.")
                .Matches(@"^[\p{L}\s'-]+$").WithMessage("La provincia solo puede contener letras, espacios, ap�strofes o guiones.");
            _ = RuleFor(x => x.Country)
                .NotEmpty().WithMessage("El pa�s es obligatorio.")
                .Length(2, 50).WithMessage("El pa�s debe tener entre 2 y 50 caracteres.")
                .Matches(@"^[\p{L}\s'-]+$").WithMessage("El pa�s solo puede contener letras, espacios, ap�strofes o guiones.");
        }
    }
}