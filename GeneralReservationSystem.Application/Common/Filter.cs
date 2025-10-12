namespace GeneralReservationSystem.Application.Common
{
    public enum FilterOperator
    {
        Equals,
        NotEquals,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        Contains,
        StartsWith,
        EndsWith,
        IsNullOrEmpty,
        IsNotNullOrEmpty
    }

    public sealed record Filter(string Field, FilterOperator Operator, object? Value);
}
