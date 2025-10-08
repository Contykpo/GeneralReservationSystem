using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneralReservationSystem.Application.Entities
{
    public class Driver
    {
        public int DriverId { get; set; }
        public int IdentificationNumber { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string LicenseNumber { get; set; } = null!;
        public DateTime LicenseExpiryDate { get; set; }
    }
}
