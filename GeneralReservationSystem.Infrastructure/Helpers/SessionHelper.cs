using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace GeneralReservationSystem.Infrastructure.Helpers
{
    public class UserSessionInfo
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
    }
}
