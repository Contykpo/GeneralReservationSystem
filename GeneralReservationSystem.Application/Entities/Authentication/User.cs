using GeneralReservationSystem.Application.Common;

namespace GeneralReservationSystem.Application.Entities.Authentication
{
    [TableName("ApplicationUser")]
    public class User
    {
        [Key]
        [Computed]
        public int UserId { get; set; }
        public string UserName { get; set; } = null!;
        [Computed]
        public string NormalizedUserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        [Computed]
        public string NormalizedEmail { get; set; } = null!;
        public byte[] PasswordHash { get; set; } = null!;
        public byte[] PasswordSalt { get; set; } = null!;
        public bool IsAdmin { get; set; } = false;
    }
}
