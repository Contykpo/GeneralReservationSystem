using System.ComponentModel.DataAnnotations;

namespace GeneralReservationSystem.Application.DTOs.Authentication
{
    public class RegisterUserDto
    {
        [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "El nombre de usuario debe tener entre 3 y 50 caracteres.")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener entre 6 y 100 caracteres.")]
        public string Password { get; set; } = string.Empty;
        [Required(ErrorMessage = "La confirmación de la contraseña es obligatoria.")]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class LoginDto
    {
        [Required(ErrorMessage = "El nombre de usuario o correo electrónico es obligatorio.")]
        public string UserNameOrEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        public string Password { get; set; } = string.Empty;
    }

    public class UpdateUserDto
    {
        [Required(ErrorMessage = "El identificador de usuario es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El identificador de usuario debe ser un número positivo.")]
        public int UserId { get; set; }

        [StringLength(50, MinimumLength = 3, ErrorMessage = "El nombre de usuario debe tener entre 3 y 50 caracteres.")]
        public string? UserName { get; set; }

        [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
        public string? Email { get; set; }
    }

    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "El identificador de usuario es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El identificador de usuario debe ser un número positivo.")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "La contraseña actual es obligatoria.")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "La nueva contraseña debe tener entre 6 y 100 caracteres.")]
        public string NewPassword { get; set; } = string.Empty;
    }

    public class UserKeyDto
    {
        [Required(ErrorMessage = "El identificador de usuario es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El identificador de usuario debe ser un número positivo.")]
        public int UserId { get; set; }
    }

    public class UserInfo
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
    }
}
