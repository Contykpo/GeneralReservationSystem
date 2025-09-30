using System.ComponentModel.DataAnnotations;

namespace GeneralReservationSystem.Application.DTOs
{
    public class CreateReservationDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "TripId must be a positive integer.")]
        public int TripId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "SeatId must be a positive integer.")]
        public int SeatId { get; set; }

        [Required]
        public Guid UserId { get; set; }
    }
}
