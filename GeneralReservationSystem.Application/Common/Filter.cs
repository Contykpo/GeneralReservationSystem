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
        NotContains,
        StartsWith,
        EndsWith,
        IsNullOrEmpty,
        IsNotNullOrEmpty,
        Between
    }

    public sealed record Filter(string PropertyOrField, FilterOperator Operator, object? Value);

    public sealed record FilterClause(IEnumerable<Filter> Filters);
}
