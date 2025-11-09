using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneralReservationSystem.Application.DTOs
{
    public class EmailDto
    {
        public string Email { get; set; }
        public string Body { get; set; }
        public string Subject { get; set; }
    }

    public class ReservationConfirmationEmailDto
    {
        public string Email { get; set; }
        public string UserName { get; set; }
        public string DepartureStation { get; set; }
        public string ArrivalStation { get; set; }
        public string DepartureTime { get; set; }
        public int SeatNumber { get; set; }
    }
}
