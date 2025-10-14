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
                "starts with" => FilterOperator.StartsWith,
                "ends with" => FilterOperator.EndsWith,
                "is empty" => FilterOperator.IsNullOrEmpty,
                "is not empty" => FilterOperator.IsNotNullOrEmpty,
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
                FilterOperator.StartsWith => "starts with",
                FilterOperator.EndsWith => "ends with",
                FilterOperator.IsNullOrEmpty => "is empty",
                FilterOperator.IsNotNullOrEmpty => "is not empty",
                _ => "equals"
            };
        }
    }

    public sealed record Filter(string Field, FilterOperator Operator, object? Value);
}
