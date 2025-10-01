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
        public required int IdentificationNumber { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string LicenseNumber { get; set; }
        public required DateTime LicenseExpiryDate { get; set; }
    }
}
