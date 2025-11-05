using GeneralReservationSystem.Application.Common;

namespace GeneralReservationSystem.Application.Entities
{
    public class Station
    {
        [Key]
        [Computed]
        public int StationId { get; set; }
        public string StationName { get; set; } = null!;
        [Computed]
        public string NormalizedStationName { get; set; } = null!;
        public string City { get; set; } = null!;
        [Computed]
        public string NormalizedCity { get; set; } = null!;
        public string Province { get; set; } = null!;
        [Computed]
        public string NormalizedProvince { get; set; } = null!;
        public string Country { get; set; } = null!;
        [Computed]
        public string NormalizedCountry { get; set; } = null!;
    }
}
