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
        public int VehicleModelId { get; set; }
        public string LicensePlate { get; set; } = null!;
        public string Status { get; set; } = null!; // Possible values: Active, Inactive, Maintenance
    }
}
