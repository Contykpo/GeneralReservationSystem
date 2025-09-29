using System;

namespace GeneralReservationSystem.Application.DTOs
{
    public class VehicleDetailsDto
    {
        public int VehicleId { get; set; }
        public required int VehicleModelId { get; set; }
        public required string LicensePlate { get; set; }
        public required string Status { get; set; }
        public required string ModelName { get; set; }
        public required string Manufacturer { get; set; }
        public required string Color { get; set; }
        public required DateTime ManufactureDate { get; set; }
    }
}
