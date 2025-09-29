using System;

namespace GeneralReservationSystem.Application.DTOs
{
    public class TripDetailsDto
    {
        public int TripId { get; set; }
        public required int VehicleId { get; set; }
        public required int DepartureId { get; set; }
        public required int DestinationId { get; set; }
        public required int DriverId { get; set; }
        public required DateTime DepartureTime { get; set; }
        public required DateTime ArrivalTime { get; set; }
        public required string DepartureName { get; set; }
        public required string DepartureCity { get; set; }
        public required string DepartureRegion { get; set; }
        public required string DepartureCountry { get; set; }
        public required TimeZoneInfo DepartureTimeZone { get; set; }
        public required string DestinationName { get; set; }
        public required string DestinationCity { get; set; }
        public required string DestinationRegion { get; set; }
        public required string DestinationCountry { get; set; }
        public required TimeZoneInfo DestinationTimeZone { get; set; }
        public required int TotalSeats { get; set; }
        public required int AvailableSeats { get; set; }
    }
}
