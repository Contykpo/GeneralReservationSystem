using System.ComponentModel.DataAnnotations;

namespace GeneralReservationSystem.Application.DTOs
{
    public class TripDetailsDto
    {
        public int TripId { get; set; }
        public int VehicleId { get; set; }
        public string VehicleModel { get; set; } = null!;
        public string VehicleLicensePlate { get; set; } = null!;
        public int DepartureId { get; set; }
        public string DepartureName { get; set; } = null!;
        public string DepartureCity { get; set; } = null!;
        public int DestinationId { get; set; }
        public string DestinationName { get; set; } = null!;
        public string DestinationCity { get; set; } = null!;
        public int DriverId { get; set; }
        public string DriverName { get; set; } = null!;
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public int TotalSeats { get; set; }
        public int AvailableSeats { get; set; }
    }

    public enum TripDetailsOrderBy
    {
        TripId,
        VehicleId,
        DepartureId,
        DestinationId,
        DriverId,
        DepartureTime,
        ArrivalTime
    }

    // NOTE: Sanitization is handled in a lower layer.
    public class TripDetailsSearchRequestDto : PagedOrderedRequestDto<TripDetailsOrderBy>
    {
        public string? DepartureName { get; set; }
        public string? DepartureCity { get; set; }
        public string? DestinationName { get; set; }
        public string? DestinationCity { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool OnlyWithAvailableSeats { get; set; } = true;
    }

    public class CreateTripDto
    {
        [Required(ErrorMessage = "El Id de vehículo es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El Id de vehículo debe ser un número positivo.")]
        public int VehicleId { get; set; }

        [Required(ErrorMessage = "El Id de salida es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El Id de salida debe ser un número positivo.")]
        public int DepartureId { get; set; }

        [Required(ErrorMessage = "El Id de destino es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El Id de destino debe ser un número positivo.")]
        public int DestinationId { get; set; }

        [Required(ErrorMessage = "El Id de conductor es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El Id de conductor debe ser un número positivo.")]
        public int DriverId { get; set; }

        [Required(ErrorMessage = "La fecha de salida es obligatoria.")]
        [DataType(DataType.DateTime)]
        public DateTime DepartureTime { get; set; }

        [Required(ErrorMessage = "La fecha de llegada es obligatoria.")]
        [DataType(DataType.DateTime)]
        public DateTime ArrivalTime { get; set; }
    }

    public class UpdateTripDto
    {
        [Required(ErrorMessage = "El identificador es obligatorio.")]
        public int TripId { get; set; }

        [Required(ErrorMessage = "El Id de vehículo es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El Id de vehículo debe ser un número positivo.")]
        public int VehicleId { get; set; }

        [Required(ErrorMessage = "El Id de salida es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El Id de salida debe ser un número positivo.")]
        public int DepartureId { get; set; }

        [Required(ErrorMessage = "El Id de destino es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El Id de destino debe ser un número positivo.")]
        public int DestinationId { get; set; }

        [Required(ErrorMessage = "El Id de conductor es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El Id de conductor debe ser un número positivo.")]
        public int DriverId { get; set; }

        [Required(ErrorMessage = "La fecha de salida es obligatoria.")]
        [DataType(DataType.DateTime)]
        public DateTime DepartureTime { get; set; }

        [Required(ErrorMessage = "La fecha de llegada es obligatoria.")]
        [DataType(DataType.DateTime)]
        public DateTime ArrivalTime { get; set; }
    }
}
