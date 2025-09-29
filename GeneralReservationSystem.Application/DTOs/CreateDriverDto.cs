using System.ComponentModel.DataAnnotations;

namespace GeneralReservationSystem.Application.DTOs
{
    public class CreateDriverDto
    {
        [Required]
        [RegularExpression(@"^\d{8}$", ErrorMessage = "Identification number must be exactly 8 digits.")]
        public string IdentificationNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters.")]
        [RegularExpression(@"^[\p{L}\s'-]+$", ErrorMessage = "First name must contain only letters, spaces, apostrophes, or hyphens.")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters.")]
        [RegularExpression(@"^[\p{L}\s'-]+$", ErrorMessage = "Last name must contain only letters, spaces, apostrophes, or hyphens.")]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^\d{8}$", ErrorMessage = "License number must be 8 digits.")]
        public string LicenseNumber { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        public DateTime LicenseExpiryDate { get; set; }
    }
}
