using System.ComponentModel.DataAnnotations;

namespace GeneralReservationSystem.Application.DTOs
{
    public class CreateDestinationDto
    {
        [Required]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")]
        [RegularExpression(@"^[\p{L}\s'-]+$", ErrorMessage = "Name must contain only letters, spaces, apostrophes, or hyphens.")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(10, MinimumLength = 2, ErrorMessage = "Code must be between 2 and 10 characters.")]
        [RegularExpression(@"^[A-Z0-9-]+$", ErrorMessage = "Code must be alphanumeric and may include dashes.")]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "City must be between 2 and 50 characters.")]
        [RegularExpression(@"^[\p{L}\s'-]+$", ErrorMessage = "City must contain only letters, spaces, apostrophes, or hyphens.")]
        public string City { get; set; } = string.Empty;

        [Required]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Region must be between 2 and 50 characters.")]
        [RegularExpression(@"^[\p{L}\s'-]+$", ErrorMessage = "Region must contain only letters, spaces, apostrophes, or hyphens.")]
        public string Region { get; set; } = string.Empty;

        [Required]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Country must be between 2 and 50 characters.")]
        [RegularExpression(@"^[\p{L}\s'-]+$", ErrorMessage = "Country must contain only letters, spaces, apostrophes, or hyphens.")]
        public string Country { get; set; } = string.Empty;

        [Required]
        public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Utc;
    }
}
