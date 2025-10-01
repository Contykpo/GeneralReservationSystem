using System.ComponentModel.DataAnnotations;

namespace GeneralReservationSystem.Application.DTOs
{
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
        [RegularExpression(@"^\d{8}$", ErrorMessage = "El número de licencia debe tener exactamente 8 dígitos.")]
        public string LicenseNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fecha de vencimiento de la licencia es obligatoria.")]
        [DataType(DataType.Date)]
        public DateTime LicenseExpiryDate { get; set; }
    }
}
