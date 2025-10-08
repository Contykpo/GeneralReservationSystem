using System.ComponentModel.DataAnnotations;

namespace GeneralReservationSystem.Application.DTOs
{
    public class AvailableSeatDto
    {
        public int SeatId { get; set; }
        public int TripId { get; set; }
        public int VehicleModelId { get; set; }
        public int SeatRow { get; set; }
        public int SeatColumn { get; set; }
        public bool IsAtWindow { get; set; }
        public bool IsAtAisle { get; set; }
        public bool IsInFront { get; set; }
        public bool IsInBack { get; set; }
        public bool IsAccessible { get; set; }
        public int VehicleId { get; set; }
        public int DepartureId { get; set; }
        public int DestinationId { get; set; }
        public int DriverId { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
    }

    public class ReservedSeatDto
    {
        public int SeatId { get; set; }
        public int TripId { get; set; }
        public Guid UserId { get; set; }
        public int VehicleModelId { get; set; }
        public int SeatRow { get; set; }
        public int SeatColumn { get; set; }
        public bool IsAtWindow { get; set; }
        public bool IsAtAisle { get; set; }
        public bool IsInFront { get; set; }
        public bool IsInBack { get; set; }
        public bool IsAccessible { get; set; }
        public int VehicleId { get; set; }
        public int DepartureId { get; set; }
        public int DestinationId { get; set; }
        public int DriverId { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
    }

    // NOTE: Sanitization is handled in a lower layer.
    public class TripAvailableSeatSearchRequestDto : PagedRequestDto
    {
        [Required(ErrorMessage = "El Id de viaje es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El Id de viaje debe ser un número positivo.")]
        public int TripId { get; set; }
    }

    // NOTE: Sanitization is handled in a lower layer.
    public class TripReservedSeatSearchRequestDto : PagedRequestDto
    {
        [Required(ErrorMessage = "El Id de viaje es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El Id de viaje debe ser un número positivo.")]
        public int TripId { get; set; }
    }

    // NOTE: Sanitization is handled in a lower layer.
    public class UserReservedSeatSearchRequestDto : PagedRequestDto
    {
        [Required(ErrorMessage = "El usuario es obligatorio.")]
        public Guid UserId { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "El Id de viaje debe ser un número positivo.")]
        public int? TripId { get; set; }
    }

    public class CreateReservationDto
    {
        [Required(ErrorMessage = "El Id de viaje es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El Id de viaje debe ser un número positivo.")]
        public int TripId { get; set; }

        [Required(ErrorMessage = "El Id de asiento es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El Id de asiento debe ser un número positivo.")]
        public int SeatId { get; set; }

        [Required(ErrorMessage = "El usuario es obligatorio.")]
        public Guid UserId { get; set; }
    }

    public class UpdateReservationDto
    {
        [Required(ErrorMessage = "El identificador es obligatorio.")]
        public int ReservationId { get; set; }

        [Required(ErrorMessage = "El Id de viaje es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El Id de viaje debe ser un número positivo.")]
        public int TripId { get; set; }

        [Required(ErrorMessage = "El Id de asiento es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El Id de asiento debe ser un número positivo.")]
        public int SeatId { get; set; }

        [Required(ErrorMessage = "El usuario es obligatorio.")]
        public Guid UserId { get; set; }
    }
}
