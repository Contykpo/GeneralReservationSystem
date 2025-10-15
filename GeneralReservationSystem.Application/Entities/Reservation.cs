using GeneralReservationSystem.Application.Common;
using System.ComponentModel.DataAnnotations;
using KeyAttribute = GeneralReservationSystem.Application.Common.KeyAttribute;

namespace GeneralReservationSystem.Application.Entities
{
    public class Reservation
    {
        [Key]
        [Required(ErrorMessage = "El Id de viaje es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El Id de viaje debe ser un número positivo.")]
        public int TripId { get; set; }
        [Key]
        [Required(ErrorMessage = "El número de asiento es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El número de asiento debe ser un número positivo.")]
        public int UserId { get; set; }
        public int Seat { get; set; }
    }
}
