using FluentValidation;
using GeneralReservationSystem.Application.DTOs.Authentication;

namespace GeneralReservationSystem.Application.Validators.Authentication
{
    public class RegisterUserDtoValidator : AppValidator<RegisterUserDto>
    {
        public RegisterUserDtoValidator()
        {
            RuleFor(x => x.UserName)
                .NotEmpty().WithMessage("El nombre de usuario es obligatorio.")
                .Length(3, 50).WithMessage("El nombre de usuario debe tener entre 3 y 50 caracteres.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("El correo electrónico es obligatorio.")
                .EmailAddress().WithMessage("El correo electrónico no es válido.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("La contraseña es obligatoria.")
                .Length(6, 100).WithMessage("La contraseña debe tener entre 6 y 100 caracteres.");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("La confirmación de la contraseña es obligatoria.")
                .Equal(x => x.Password).WithMessage("Las contraseñas no coinciden.");
        }
    }

    public class LoginDtoValidator : AppValidator<LoginDto>
    {
        public LoginDtoValidator()
        {
            RuleFor(x => x.UserNameOrEmail)
                .NotEmpty().WithMessage("El nombre de usuario o correo electrónico es obligatorio.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("La contraseña es obligatoria.");
        }
    }

    public class UpdateUserDtoValidator : AppValidator<UpdateUserDto>
    {
        public UpdateUserDtoValidator()
        {
            RuleFor(x => x.UserId)
                .GreaterThan(0).WithMessage("El identificador de usuario debe ser un número positivo.");

            RuleFor(x => x.UserName)
                .Length(3, 50).When(x => x.UserName != null)
                .WithMessage("El nombre de usuario debe tener entre 3 y 50 caracteres.");

            RuleFor(x => x.Email)
                .EmailAddress().When(x => x.Email != null)
                .WithMessage("El correo electrónico no es válido.");
        }
    }

    public class ChangePasswordDtoValidator : AppValidator<ChangePasswordDto>
    {
        public ChangePasswordDtoValidator()
        {
            RuleFor(x => x.UserId)
                .GreaterThan(0).WithMessage("El identificador de usuario debe ser un número positivo.");

            RuleFor(x => x.CurrentPassword)
                .NotEmpty().WithMessage("La contraseña actual es obligatoria.");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("La nueva contraseña es obligatoria.")
                .Length(6, 100).WithMessage("La nueva contraseña debe tener entre 6 y 100 caracteres.");
        }
    }

    public class UserKeyDtoValidator : AppValidator<UserKeyDto>
    {
        public UserKeyDtoValidator()
        {
            RuleFor(x => x.UserId)
                .GreaterThan(0).WithMessage("El identificador de usuario debe ser un número positivo.");
        }
    }
}
