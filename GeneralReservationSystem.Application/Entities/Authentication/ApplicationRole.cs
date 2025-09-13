namespace GeneralReservationSystem.Application.Entities.Authentication
{
    public class ApplicationRole
    {
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string NormalizedName { get; set; } = string.Empty;
    }
}
