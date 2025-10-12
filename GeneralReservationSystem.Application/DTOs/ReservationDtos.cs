using System.ComponentModel.DataAnnotations;

namespace GeneralReservationSystem.Application.DTOs
{
    public class CreateReservationDto
    {
        [Required(ErrorMessage = "El Id de viaje es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El Id de viaje debe ser un número positivo.")]
        public int TripId { get; set; }

        // User is obtained from the authenticated context, not from the client.

        [Required(ErrorMessage = "El número de asiento es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El número de asiento debe ser un número positivo.")]
        public int Seat { get; set; }
    }

    public class ReservationKeyDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "El Id de viaje debe ser un número positivo.")]
        public int TripId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "El Id de usuario debe ser un número positivo.")]
        public int UserId { get; set; }
    }

    // For this case, we don't need an UpdateReservationDto because reservations are not meant to be updated.
}
