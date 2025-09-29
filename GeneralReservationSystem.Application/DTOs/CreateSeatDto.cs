using System.ComponentModel.DataAnnotations;

namespace GeneralReservationSystem.Application.DTOs
{
    public class CreateSeatDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "VehicleModelId must be a positive integer.")]
        public int VehicleModelId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Row must be at least 1.")]
        public int Row { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Column must be at least 1.")]
        public int Column { get; set; }

        public bool IsAtWindow { get; set; } = false;
        public bool IsAtAisle { get; set; } = false;
        public bool IsInFront { get; set; } = false;
        public bool IsInBack { get; set; } = false;
        public bool IsAccessible { get; set; } = false;
    }

    public class CreateSeatForVehicleModelDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Row must be at least 1.")]
        public int Row { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Column must be at least 1.")]
        public int Column { get; set; }

        public bool IsAtWindow { get; set; } = false;
        public bool IsAtAisle { get; set; } = false;
        public bool IsInFront { get; set; } = false;
        public bool IsInBack { get; set; } = false;
        public bool IsAccessible { get; set; } = false;
    }
}
