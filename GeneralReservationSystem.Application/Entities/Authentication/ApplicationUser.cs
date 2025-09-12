namespace GeneralReservationSystem.Application.Entities.Authentication
{
    public class ApplicationUser
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string NormalizedUserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string NormalizedEmail { get; set; } = string.Empty;

        public bool EmailConfirmed { get; set; }

        public byte[] PasswordHash { get; set; } = Array.Empty<byte>();

        public Guid SecurityStamp { get; set; } = Guid.NewGuid();
    }
}
