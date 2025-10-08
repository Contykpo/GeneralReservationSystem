using System.ComponentModel.DataAnnotations;

namespace GeneralReservationSystem.Application.DTOs
{
    // NOTE: Sanitization is handled in a lower layer.
    public enum DriverOrderBy
    {
        DriverId,
        IdentificationNumber,
        FirstName,
        LastName,
        LicenseNumber,
        LicenseExpiryDate
    }

    // NOTE: Sanitization is handled in a lower layer.
    public class DriverSearchRequestDto : PagedOrderedRequestDto<DriverOrderBy>
    {
        public int? IdentificationNumber { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? LicenseNumber { get; set; }
    }

    public class CreateDriverDto
    {
        [Required(ErrorMessage = "El número de identificación es obligatorio.")]
        [RegularExpression(@"^\d{8}$", ErrorMessage = "El número de identificación debe tener exactamente 8 dígitos.")]
        public string IdentificationNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 50 caracteres.")]
        [RegularExpression(@"^[\p{L}\s'-]+$", ErrorMessage = "El nombre solo puede contener letras, espacios, apóstrofes o guiones.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "El apellido debe tener entre 2 y 50 caracteres.")]
        [RegularExpression(@"^[\p{L}\s'-]+$", ErrorMessage = "El apellido solo puede contener letras, espacios, apóstrofes o guiones.")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El número de licencia es obligatorio.")]
        [RegularExpression(@"^[A-Z]{3}\d{8}$", ErrorMessage = "El número de licencia debe comenzar con 3 letras mayúsculas seguidas de 8 dígitos.")]
        public string LicenseNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fecha de vencimiento de la licencia es obligatoria.")]
        [DataType(DataType.Date)]
        public DateTime LicenseExpiryDate { get; set; }
    }

    public class UpdateDriverDto
    {
        [Required(ErrorMessage = "El identificador es obligatorio.")]
        public int DriverId { get; set; }

        [Required(ErrorMessage = "El número de identificación es obligatorio.")]
        [RegularExpression(@"^\d{8}$", ErrorMessage = "El número de identificación debe tener exactamente 8 dígitos.")]
        public string IdentificationNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 50 caracteres.")]
        [RegularExpression(@"^[\p{L}\s'-]+$", ErrorMessage = "El nombre solo puede contener letras, espacios, apóstrofes o guiones.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "El apellido debe tener entre 2 y 50 caracteres.")]
        [RegularExpression(@"^[\p{L}\s'-]+$", ErrorMessage = "El apellido solo puede contener letras, espacios, apóstrofes o guiones.")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El número de licencia es obligatorio.")]
        [RegularExpression(@"^[A-Z]{3}\d{8}$", ErrorMessage = "El número de licencia debe comenzar con 3 letras mayúsculas seguidas de 8 dígitos.")]
        public string LicenseNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fecha de vencimiento de la licencia es obligatoria.")]
        [DataType(DataType.Date)]
        public DateTime LicenseExpiryDate { get; set; }
    }
}
