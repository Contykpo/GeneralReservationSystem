namespace GeneralReservationSystem.Application.Entities
{
    public class Destination
    {
        public int DestinationId { get; set; }
        public required string Name { get; set; }
        public required string City { get; set; }
        public required string Region { get; set; }
        public required string Country { get; set; }
        public required string NormalizedName { get; set; }
        public required string NormalizedCity { get; set; }
        public required string NormalizedRegion { get; set; }
        public required string NormalizedCountry { get; set; }
        public required TimeZoneInfo TimeZone { get; set; }
    }
}
