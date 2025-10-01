using System.ComponentModel.DataAnnotations;

namespace GeneralReservationSystem.Application.DTOs
{
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
}
