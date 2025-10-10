using GeneralReservationSystem.Application.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneralReservationSystem.Application.Entities
{
    public class Trip
    {
        [Key]
        [Computed]
        public int TripId { get; set; }
        public int DepartureStationId { get; set; }
        public DateTime DepartureTime { get; set; }
        public int ArrivalStationId { get; set; }
        public DateTime ArrivalTime { get; set; }
        public int AvailableSeats { get; set; }
    }
}
