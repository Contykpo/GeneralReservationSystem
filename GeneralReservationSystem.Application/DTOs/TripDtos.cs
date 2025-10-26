using System.ComponentModel.DataAnnotations;

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

    public class UpdateTripDto
    {
        public int TripId { get; set; }
        public int? DepartureStationId { get; set; }
        public DateTime? DepartureTime { get; set; }
        public int? ArrivalStationId { get; set; }
        public DateTime? ArrivalTime { get; set; }
        public int? AvailableSeats { get; set; }
    }

    public class TripKeyDto
    {
        public int TripId { get; set; }
    }

    public class TripWithDetailsDto
    {
        public int TripId { get; set; }
        public int DepartureStationId { get; set; }
        public DateTime DepartureTime { get; set; }
        public int ArrivalStationId { get; set; }
        public DateTime ArrivalTime { get; set; }
        public int AvailableSeats { get; set; }
        public int ReservedSeats { get; set; }
        public int FreeSeats => AvailableSeats - ReservedSeats;
    }
}
