using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneralReservationSystem.Application.Entities
{
    public class Reservation
    {
        public required int TripId { get; set; }
        public required int SeatId { get; set; }
        public required Guid UserId { get; set; }
    }
}
