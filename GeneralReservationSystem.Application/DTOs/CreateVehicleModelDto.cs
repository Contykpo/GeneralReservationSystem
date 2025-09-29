using System.ComponentModel.DataAnnotations;

namespace GeneralReservationSystem.Application.DTOs
{
    public class CreateVehicleModelDto
    {
        [Required]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 50 characters.")]
        [RegularExpression(@"^[\p{L}\s'-]+$", ErrorMessage = "Name must contain only letters, spaces, apostrophes, or hyphens.")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Manufacturer must be between 2 and 50 characters.")]
        [RegularExpression(@"^[\p{L}\s'-]+$", ErrorMessage = "Manufacturer must contain only letters, spaces, apostrophes, or hyphens.")]
        public string Manufacturer { get; set; } = string.Empty;

        [Required]
        [MinLength(1, ErrorMessage = "At least one seat must be provided.")]
        public List<CreateSeatForVehicleModelDto> Seats { get; set; } = new();
    }
}
