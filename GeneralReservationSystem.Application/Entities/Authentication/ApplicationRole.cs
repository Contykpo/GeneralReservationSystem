namespace GeneralReservationSystem.Application.Entities.Authentication
{
    public class ApplicationRole
    {
        public Guid RoleId { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string NormalizedName { get; set; } = string.Empty;
    }
}
