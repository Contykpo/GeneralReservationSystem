using System.ComponentModel.DataAnnotations;

namespace GeneralReservationSystem.Application.DTOs
{
    public class CreateTripDto
    {
        public int DepartureStationId { get; set; }

        public DateTime DepartureTime { get; set; } = DateTime.UtcNow;

        public int ArrivalStationId { get; set; }

        public DateTime ArrivalTime { get; set; } = DateTime.UtcNow;

        public int AvailableSeats { get; set; }
    }

    public class UpdateTripDto
    {
        [Required(ErrorMessage = "El identificador es obligatorio.")]
        public int TripId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El Id de salida debe ser un número positivo.")]
        public int? DepartureStationId { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? DepartureTime { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El Id de destino debe ser un número positivo.")]
        public int? ArrivalStationId { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? ArrivalTime { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El número de asientos disponibles debe ser un número positivo.")]
        public int? AvailableSeats { get; set; }
    }

    public class TripKeyDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "El Id de viaje debe ser un número positivo.")]
        public int TripId { get; set; }
    }
}
