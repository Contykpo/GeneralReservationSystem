namespace GeneralReservationSystem.Application.Common
{
    public enum SortDirection
    {
        Asc,
        Desc
    }

    public sealed record SortOption(string PropertyOrField, SortDirection Direction = SortDirection.Asc);
}
