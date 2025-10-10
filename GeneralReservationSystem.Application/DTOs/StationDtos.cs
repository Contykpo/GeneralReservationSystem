using System.ComponentModel.DataAnnotations;

namespace GeneralReservationSystem.Application.DTOs
{
    public class CreateStationDto
    {
        [Required(ErrorMessage = "El nombre de la estación es obligatorio.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre de la estación debe tener entre 2 y 100 caracteres.")]
        [RegularExpression(@"^[\p{L}\s'-]+$", ErrorMessage = "El nombre de la estación solo puede contener letras, espacios, apóstrofes o guiones.")]
        public string StationName { get; set; } = string.Empty;

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
    }

    public class UpdateStationDto
    {
        [Required(ErrorMessage = "El identificador es obligatorio.")]
        public int StationId { get; set; }

        [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre de la estación debe tener entre 2 y 100 caracteres.")]
        [RegularExpression(@"^[\p{L}\s'-]+$", ErrorMessage = "El nombre de la estación solo puede contener letras, espacios, apóstrofes o guiones.")]
        public string? StationName { get; set; }

        [StringLength(50, MinimumLength = 2, ErrorMessage = "La ciudad debe tener entre 2 y 50 caracteres.")]
        [RegularExpression(@"^[\p{L}\s'-]+$", ErrorMessage = "La ciudad solo puede contener letras, espacios, apóstrofes o guiones.")]
        public string? City { get; set; }

        [StringLength(50, MinimumLength = 2, ErrorMessage = "La región debe tener entre 2 y 50 caracteres.")]
        [RegularExpression(@"^[\p{L}\s'-]+$", ErrorMessage = "La región solo puede contener letras, espacios, apóstrofes o guiones.")]
        public string? Region { get; set; }

        [StringLength(50, MinimumLength = 2, ErrorMessage = "El país debe tener entre 2 y 50 caracteres.")]
        [RegularExpression(@"^[\p{L}\s'-]+$", ErrorMessage = "El país solo puede contener letras, espacios, apóstrofes o guiones.")]
        public string? Country { get; set; }
    }

    public class StationKeyDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "El Id de estación debe ser un número positivo.")]
        public int StationId { get; set; }
    }
}
