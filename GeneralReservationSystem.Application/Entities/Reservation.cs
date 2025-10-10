using GeneralReservationSystem.Application.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneralReservationSystem.Application.Entities
{
    public class Reservation
    {
        [Key]
        public int TripId { get; set; }
        [Key]
        public int UserId { get; set; }
        public int SeatNumber { get; set; }
    }
}
