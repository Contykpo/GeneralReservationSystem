using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneralReservationSystem.Application.DTOs
{
    public class FreeSeat
    {
        public required int TripId { get; set; }
        public required int SeatId { get; set; }
        public required int Row { get; set; }
        public required int Column { get; set; }
        public required bool IsAtWindow { get; set; } = false;
        public required bool IsAtAisle { get; set; } = false;
        public required bool IsInFront { get; set; } = false;
        public required bool IsInBack { get; set; } = false;
        public required bool IsAccessible { get; set; } = false;
    }
}
