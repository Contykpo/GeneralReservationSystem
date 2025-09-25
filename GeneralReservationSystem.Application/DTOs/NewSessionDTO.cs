using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneralReservationSystem.Application.DTOs
{
    public class NewSessionDTO
    {
        public required Guid SessionId { get; set; }
        public required Guid UserId { get; set; }
        public required DateTime CreatedAt { get; set; }
        public required DateTime ExpiresAt { get; set; }
    }
}
