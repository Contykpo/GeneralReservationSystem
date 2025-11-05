using GeneralReservationSystem.Application.Common;

namespace GeneralReservationSystem.Application.Entities
{
    public class Reservation
    {
        [Key]
        public int TripId { get; set; }
        public int UserId { get; set; }
        [Key]
        public int Seat { get; set; }
    }
}
