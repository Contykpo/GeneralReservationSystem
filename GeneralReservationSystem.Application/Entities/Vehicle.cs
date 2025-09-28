using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneralReservationSystem.Application.Entities
{
    public class Vehicle
    {
        public int VehicleId { get; set; }
        public required int VehicleModelId { get; set; }
        public required string LicensePlate { get; set; }
        public required string Status { get; set; } // Possible values: Active, Inactive, Maintenance
    }
}
