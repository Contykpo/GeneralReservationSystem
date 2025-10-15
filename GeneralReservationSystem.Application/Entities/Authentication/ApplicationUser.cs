using GeneralReservationSystem.Application.Common;

namespace GeneralReservationSystem.Application.Entities.Authentication
{
    [TableName("ApplicationUser")]
    public class ApplicationUser
    {
        [Key]
        [Computed]
        public int UserId { get; set; }
        
        public string UserName { get; set; } = string.Empty;
        [Computed]
        public string NormalizedUserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        [Computed]
        public string NormalizedEmail { get; set; } = string.Empty;
        
        public byte[] PasswordHash { get; set; } = Array.Empty<byte>();
        
        public bool IsAdmin { get; set; } = false;
    }
}
