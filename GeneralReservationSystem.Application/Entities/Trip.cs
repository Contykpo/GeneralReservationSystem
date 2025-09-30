using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneralReservationSystem.Application.Entities
{
    public class Trip
    {
        public int TripId { get; set; }
        public int VehicleId { get; set; }
        public int DepartureId { get; set; }
        public int DestinationId { get; set; }
        public int DriverId { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
    }
}
