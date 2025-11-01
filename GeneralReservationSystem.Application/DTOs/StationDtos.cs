namespace GeneralReservationSystem.Application.DTOs
{
    public class CreateStationDto
    {
        public string StationName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
    }

    public class UpdateStationDto
    {
        public int StationId { get; set; }
        public string? StationName { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }
        public string? Country { get; set; }
    }

    public class StationKeyDto
    {
        public int StationId { get; set; }
    }

    public class ImportStationDto
    {
        public string StationName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
    }
}
