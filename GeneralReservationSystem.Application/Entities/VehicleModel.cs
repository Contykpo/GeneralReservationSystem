using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneralReservationSystem.Application.Entities
{
    public class VehicleModel
    {
        public int VehicleModelId { get; set; }
        public required string Name { get; set; }
        public required string Manufacturer { get; set; }
    }
}
