using System.ComponentModel.DataAnnotations;

namespace GeneralReservationSystem.Application.DTOs
{
    public class CreateStationDto
    {
        public string StationName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
    }

    public class UpdateStationDto
    {
        public int StationId { get; set; }
        public string? StationName { get; set; }
        public string? City { get; set; }
        public string? Region { get; set; }
        public string? Country { get; set; }
    }

    public class StationKeyDto
    {
        public int StationId { get; set; }
    }
}
