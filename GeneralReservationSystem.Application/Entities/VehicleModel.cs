using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneralReservationSystem.Application.Entities
{
    public class VehicleModel
    {
        public int VehicleModelId { get; set; } = new int();
        public string Name { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
    }
}
