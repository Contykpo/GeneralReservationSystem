namespace GeneralReservationSystem.Application.Common
{
    public enum SortDirection
    {
        Asc,
        Desc
    }

    public sealed record SortOption(string Field, SortDirection Direction = SortDirection.Asc);
}
