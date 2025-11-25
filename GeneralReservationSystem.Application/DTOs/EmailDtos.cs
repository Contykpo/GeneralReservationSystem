namespace GeneralReservationSystem.Application.DTOs
{
    public class EmailDto
    {
        public required string Email { get; set; }
        public required string Body { get; set; }
        public required string Subject { get; set; }
    }

    public class ReservationConfirmationEmailDto
    {
        public required string Email { get; set; }
        public required string UserName { get; set; }
        public required string DepartureStation { get; set; }
        public required string ArrivalStation { get; set; }
        public required string DepartureTime { get; set; }
        public int SeatNumber { get; set; }
    }
}
