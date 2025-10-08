namespace GeneralReservationSystem.Application.Entities.Authentication
{
    public class ApplicationRole
    {
        public Guid RoleId { get; set; }
        public string Name { get; set; } = null!;
        public string NormalizedName { get; set; } = null!;
    }
}
