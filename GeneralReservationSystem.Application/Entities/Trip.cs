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
        public required int VehicleId { get; set; }
        public required int DepartureId { get; set; }
        public required int DestinationId { get; set; }
        public required int DriverId { get; set; }
        public required DateTime DepartureTime { get; set; }
        public required DateTime ArrivalTime { get; set; }
    }
}
