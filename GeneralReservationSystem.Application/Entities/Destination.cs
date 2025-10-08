using GeneralReservationSystem.Application.Common;

namespace GeneralReservationSystem.Application.Entities
{
    // Normalized fields are computed columns in the database, used for efficient querying and indexing.
    public class Destination
    {
        [Key]
        [Computed]
        public int DestinationId { get; set; }
        public string Name { get; set; } = null!;
        [Computed]
        public string NormalizedName { get; set; } = null!;
        public string Code { get; set; } = null!;
        [Computed]
        public string NormalizedCode { get; set; } = null!;
        public string City { get; set; } = null!;
        [Computed]
        public string NormalizedCity { get; set; } = null!;
        public string Region { get; set; } = null!;
        [Computed]
        public string NormalizedRegion { get; set; } = null!;
        public string Country { get; set; } = null!;
        [Computed]
        public string NormalizedCountry { get; set; } = null!;
        public TimeZoneInfo TimeZone { get; set; } = null!;
    }
}
