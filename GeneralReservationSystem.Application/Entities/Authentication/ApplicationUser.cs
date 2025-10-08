namespace GeneralReservationSystem.Application.Entities.Authentication
{
    public class ApplicationUser
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = null!;
        public string NormalizedUserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string NormalizedEmail { get; set; } = null!;
        public bool EmailConfirmed { get; set; } = false;
        public byte[] PasswordHash { get; set; } = null!;
        public Guid SecurityStamp { get; set; }
    }
}
