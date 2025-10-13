using GeneralReservationSystem.Application.Common;

namespace GeneralReservationSystem.Application.Entities
{
    public class Reservation
    {
        [Key]
        public int TripId { get; set; }
        [Key]
        public int UserId { get; set; }
        public int Seat { get; set; }
    }
}
