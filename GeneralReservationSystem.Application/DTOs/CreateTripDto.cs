using System.ComponentModel.DataAnnotations;

namespace GeneralReservationSystem.Application.DTOs
{
    public class CreateTripDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "VehicleId must be a positive integer.")]
        public int VehicleId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "DepartureId must be a positive integer.")]
        public int DepartureId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "DestinationId must be a positive integer.")]
        public int DestinationId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "DriverId must be a positive integer.")]
        public int DriverId { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime DepartureTime { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime ArrivalTime { get; set; }
    }
}
