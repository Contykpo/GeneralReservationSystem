using System.ComponentModel.DataAnnotations;

namespace GeneralReservationSystem.Application.DTOs
{
    public class CreateDestinationDto
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres.")]
        [RegularExpression(@"^[\p{L}\s'-]+$", ErrorMessage = "El nombre solo puede contener letras, espacios, apóstrofes o guiones.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "El código es obligatorio.")]
        [StringLength(10, MinimumLength = 2, ErrorMessage = "El código debe tener entre 2 y 10 caracteres.")]
        [RegularExpression(@"^[-A-Za-z0-9]+$", ErrorMessage = "El código debe ser alfanumérico y puede incluir guiones simples entre caracteres.")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "La ciudad es obligatoria.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "La ciudad debe tener entre 2 y 50 caracteres.")]
        [RegularExpression(@"^[\p{L}\s'-]+$", ErrorMessage = "La ciudad solo puede contener letras, espacios, apóstrofes o guiones.")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "La región es obligatoria.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "La región debe tener entre 2 y 50 caracteres.")]
        [RegularExpression(@"^[\p{L}\s'-]+$", ErrorMessage = "La región solo puede contener letras, espacios, apóstrofes o guiones.")]
        public string Region { get; set; } = string.Empty;

        [Required(ErrorMessage = "El país es obligatorio.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "El país debe tener entre 2 y 50 caracteres.")]
        [RegularExpression(@"^[\p{L}\s'-]+$", ErrorMessage = "El país solo puede contener letras, espacios, apóstrofes o guiones.")]
        public string Country { get; set; } = string.Empty;

        [Required(ErrorMessage = "La zona horaria es obligatoria.")]
        public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Utc;
    }
}
