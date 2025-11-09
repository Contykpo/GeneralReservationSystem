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

    public static class FilterOperatorExtensions
    {
        public static FilterOperator ToFilterOperator(string name)
        {
            return name switch
            {
                "equals" => FilterOperator.Equals,
                "not equals" => FilterOperator.NotEquals,
                "greater than" => FilterOperator.GreaterThan,
                "greater than or equal" => FilterOperator.GreaterThanOrEqual,
                "less than" => FilterOperator.LessThan,
                "less than or equal" => FilterOperator.LessThanOrEqual,
                "contains" => FilterOperator.Contains,
                "not contains" => FilterOperator.NotContains,
                "starts with" => FilterOperator.StartsWith,
                "ends with" => FilterOperator.EndsWith,
                "is empty" => FilterOperator.IsNullOrEmpty,
                "is not empty" => FilterOperator.IsNotNullOrEmpty,
                "between" => FilterOperator.Between,
                _ => FilterOperator.Equals
            };
        }

        public static string ToString(FilterOperator op)
        {
            return op switch
            {
                FilterOperator.Equals => "equals",
                FilterOperator.NotEquals => "not equals",
                FilterOperator.GreaterThan => "greater than",
                FilterOperator.GreaterThanOrEqual => "greater than or equal",
                FilterOperator.LessThan => "less than",
                FilterOperator.LessThanOrEqual => "less than or equal",
                FilterOperator.Contains => "contains",
                FilterOperator.NotContains => "not contains",
                FilterOperator.StartsWith => "starts with",
                FilterOperator.EndsWith => "ends with",
                FilterOperator.IsNullOrEmpty => "is empty",
                FilterOperator.IsNotNullOrEmpty => "is not empty",
                FilterOperator.Between => "between",
                _ => "equals"
            };
        }
    }

    public sealed record Filter(string PropertyOrField, FilterOperator Operator, object? Value);

    public sealed record FilterClause(IEnumerable<Filter> Filters);
}
