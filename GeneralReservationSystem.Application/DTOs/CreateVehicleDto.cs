using System.ComponentModel.DataAnnotations;

namespace GeneralReservationSystem.Application.DTOs
{
    public class CreateVehicleDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Vehicle model ID must be a positive integer.")]
        public int VehicleModelId { get; set; }

        [Required]
        [StringLength(7, MinimumLength = 6, ErrorMessage = "License plate must be 6-7 characters.")]
        [RegularExpression(@"^([A-Z]{3}\d{3}|[A-Z]{2}\d{3}[A-Z]{2})$", ErrorMessage = "License plate must match formats: old (ABC123) or new (AB123CD).")]
        public string LicensePlate { get; set; } = string.Empty;

        [Required]
        [StringLength(20, ErrorMessage = "Status must be less than 20 characters.")]
        [RegularExpression(@"^(Active|Inactive|Maintenance)$", ErrorMessage = "Status must be 'Active', 'Inactive', or 'Maintenance'.")]
        public string Status { get; set; } = string.Empty;
    }
}
