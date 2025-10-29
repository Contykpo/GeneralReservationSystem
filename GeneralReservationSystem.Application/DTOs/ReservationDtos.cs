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

    public class ReservationDetailsDto
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
        public int UserId { get; set; }
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public int Seat { get; set; }
    }

    public class UserReservationDetailsDto
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
        public int Seat { get; set; }
    }
}
