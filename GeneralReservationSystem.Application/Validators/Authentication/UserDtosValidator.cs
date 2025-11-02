using FluentValidation;
using GeneralReservationSystem.Application.DTOs.Authentication;

namespace GeneralReservationSystem.Application.Validators.Authentication
{
    public class RegisterUserDtoValidator : AppValidator<RegisterUserDto>
    {
        public RegisterUserDtoValidator()
        {
            _ = RuleFor(x => x.UserName)
                .NotEmpty().WithMessage("El nombre de usuario es obligatorio.")
                .Length(3, 50).WithMessage("El nombre de usuario debe tener entre 3 y 50 caracteres.");

            _ = RuleFor(x => x.Email)
                .NotEmpty().WithMessage("El correo electr�nico es obligatorio.")
                .EmailAddress().WithMessage("El correo electr�nico no es v�lido.");

            _ = RuleFor(x => x.Password)
                .NotEmpty().WithMessage("La contrase�a es obligatoria.")
                .Length(6, 100).WithMessage("La contrase�a debe tener entre 6 y 100 caracteres.");

            _ = RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("La confirmaci�n de la contrase�a es obligatoria.")
                .Equal(x => x.Password).WithMessage("Las contrase�as no coinciden.");
        }
    }

    public class LoginDtoValidator : AppValidator<LoginDto>
    {
        public LoginDtoValidator()
        {
            _ = RuleFor(x => x.UserNameOrEmail)
                .NotEmpty().WithMessage("El nombre de usuario o correo electr�nico es obligatorio.");

            _ = RuleFor(x => x.Password)
                .NotEmpty().WithMessage("La contrase�a es obligatoria.");
        }
    }

    public class UpdateUserDtoValidator : AppValidator<UpdateUserDto>
    {
        public UpdateUserDtoValidator()
        {
            _ = RuleFor(x => x.UserId)
                .GreaterThan(0).WithMessage("El identificador de usuario debe ser un n�mero positivo.");

            _ = RuleFor(x => x.UserName)
                .Length(3, 50).When(x => x.UserName != null)
                .WithMessage("El nombre de usuario debe tener entre 3 y 50 caracteres.");

            _ = RuleFor(x => x.Email)
                .EmailAddress().When(x => x.Email != null)
                .WithMessage("El correo electr�nico no es v�lido.");
        }
    }

    public class ChangePasswordDtoValidator : AppValidator<ChangePasswordDto>
    {
        public ChangePasswordDtoValidator()
        {
            _ = RuleFor(x => x.UserId)
                .GreaterThan(0).WithMessage("El identificador de usuario debe ser un n�mero positivo.");

            _ = RuleFor(x => x.CurrentPassword)
                .NotEmpty().WithMessage("La contrase�a actual es obligatoria.");

            _ = RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("La nueva contrase�a es obligatoria.")
                .Length(6, 100).WithMessage("La nueva contrase�a debe tener entre 6 y 100 caracteres.");
        }
    }

    public class UserKeyDtoValidator : AppValidator<UserKeyDto>
    {
        public UserKeyDtoValidator()
        {
            _ = RuleFor(x => x.UserId)
                .GreaterThan(0).WithMessage("El identificador de usuario debe ser un n�mero positivo.");
        }
    }
}
