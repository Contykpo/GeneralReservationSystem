using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneralReservationSystem.Application.Entities
{
    public class Reservation
    {
        public int TripId { get; set; }
        public int SeatId { get; set; }
        public Guid UserId { get; set; }
    }
}
