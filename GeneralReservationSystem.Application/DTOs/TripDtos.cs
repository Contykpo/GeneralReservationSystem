namespace GeneralReservationSystem.Application.DTOs
{
    public class CreateTripDto
    {
        public int DepartureStationId { get; set; }
        public DateTime DepartureTime { get; set; }
        public int ArrivalStationId { get; set; }
        public DateTime ArrivalTime { get; set; }
        public int AvailableSeats { get; set; }
    }

    // Trips shouldn't be updated, so no UpdateTripDto is needed

    public class TripKeyDto
    {
        public int TripId { get; set; }
    }

    public class TripWithDetailsDto
    {
        public int TripId { get; set; }
        public int DepartureStationId { get; set; }
        public string DepartureStationName { get; set; } = null!;
        public string DepartureCity { get; set; } = null!;
        public string DepartureProvince { get; set; } = null!;
        public string DepartureCountry { get; set; } = null!;
        public DateTime DepartureTime { get; set; }
        public int ArrivalStationId { get; set; }
        public string ArrivalStationName { get; set; } = null!;
        public string ArrivalCity { get; set; } = null!;
        public string ArrivalProvince { get; set; } = null!;
        public string ArrivalCountry { get; set; } = null!;
        public DateTime ArrivalTime { get; set; }
        public int AvailableSeats { get; set; }
        public int ReservedSeats { get; set; }
        public int FreeSeats => AvailableSeats - ReservedSeats;
    }
}