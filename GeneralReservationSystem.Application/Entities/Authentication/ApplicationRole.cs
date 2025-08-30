namespace GeneralReservationSystem.Application.Entities.Authentication
{
    public class ApplicationRole : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string NormalizedName { get; set; } = string.Empty;
    }
}
