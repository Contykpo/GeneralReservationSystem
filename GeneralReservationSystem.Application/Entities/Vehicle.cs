using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneralReservationSystem.Application.Entities
{
    public class Vehicle
    {
        public int VehicleId { get; set; } = new int();
        public int VehicleModelId { get; set; } = new int();
        public string LicensePlate { get; set; } = string.Empty;
        // Possible values: Active, Inactive, Maintenance
        public string Status { get; set; } = string.Empty;
    }
}
