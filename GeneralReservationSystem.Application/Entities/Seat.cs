using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneralReservationSystem.Application.Entities
{
    public class Seat
    {
        public int SeatId { get; set; } = new int();
        public int VehicleModelId { get; set; } = new int();
        public int SeatRow { get; set; } = new int();
        public int SeatColumn { get; set; } = new int();
        public bool IsAtWindow { get; set; } = false;
        public bool IsAtAisle { get; set; } = false;
        public bool IsInFront { get; set; } = false;
        public bool IsInBack { get; set; } = false;
        public bool IsAccessible { get; set; } = false;
    }
}
