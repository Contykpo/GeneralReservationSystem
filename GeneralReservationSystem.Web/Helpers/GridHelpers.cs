using GeneralReservationSystem.Application.Common;
using MudBlazor;
using SortDirection = GeneralReservationSystem.Application.Common.SortDirection;

namespace GeneralReservationSystem.Web.Helpers
{
    public static class GridHelpers
    {
        public static IList<Filter> BuildFilters<T>(GridState<T> state)
        {
            List<Filter> filters = [];

            if (state?.FilterDefinitions is null)
            {
                return filters;
            }

            foreach (IFilterDefinition<T> def in state.FilterDefinitions)
            {
                string? field = def?.Column?.PropertyName;
                if (string.IsNullOrWhiteSpace(field))
                {
                    continue;
                }

                Application.Common.FilterOperator op = FilterOperatorExtensions.ToFilterOperator(def!.Operator!);
                object? value = def!.Value;

                filters.Add(new Filter(field!, op, value));
            }

            return filters;
        }

        public static IList<SortOption> BuildOrders<T>(GridState<T> state)
        {
            List<SortOption> orders = [];

            if (state?.SortDefinitions is null)
            {
                return orders;
            }

            foreach (SortDefinition<T> def in state.SortDefinitions)
            {
                string field = def.SortBy;
                if (string.IsNullOrWhiteSpace(field))
                {
                    continue;
                }

                SortDirection direction = def.Descending
                    ? SortDirection.Desc
                    : SortDirection.Asc;

                orders.Add(new SortOption(field!, direction));
            }

            return orders;
        }
    }
}
