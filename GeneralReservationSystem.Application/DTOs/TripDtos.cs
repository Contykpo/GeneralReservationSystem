using System.ComponentModel.DataAnnotations;

namespace GeneralReservationSystem.Application.DTOs
{
    public class CreateTripDto
    {
        [Required(ErrorMessage = "El Id de salida es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El Id de salida debe ser un número positivo.")]
        public int DepartureStationId { get; set; }

        [Required(ErrorMessage = "La fecha de salida es obligatoria.")]
        [DataType(DataType.DateTime)]
        public DateTime DepartureTime { get; set; }

        [Required(ErrorMessage = "El Id de destino es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El Id de destino debe ser un número positivo.")]
        public int ArrivalStationId { get; set; }

        [Required(ErrorMessage = "La fecha de llegada es obligatoria.")]
        [DataType(DataType.DateTime)]
        public DateTime ArrivalTime { get; set; }

        [Required(ErrorMessage = "El número de asientos disponibles es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El número de asientos disponibles debe ser un número positivo.")]
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

    public class TripWithDetailsDto
    {
        public int TripId { get; set; }
        public int DepartureStationId { get; set; }
        public DateTime DepartureTime { get; set; }
        public int ArrivalStationId { get; set; }
        public DateTime ArrivalTime { get; set; }
        public int AvailableSeats { get; set; }
        public int ReservedSeats { get; set; }
        public int FreeSeats => AvailableSeats - ReservedSeats;
    }
}
