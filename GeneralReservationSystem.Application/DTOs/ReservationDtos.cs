namespace GeneralReservationSystem.Application.DTOs
{
    public class CreateReservationDto
    {
        public int TripId { get; set; }
        public int UserId { get; set; }
        public int Seat { get; set; }
    }

    public class ReservationKeyDto
    {
        public int TripId { get; set; }
        public int Seat { get; set; }
    }

    public class TripUserReservationsKeyDto
    {
        public int TripId { get; set; }
        public int UserId { get; set; }
    }
}
