using GeneralReservationSystem.Application.Common;
using MudBlazor;
using SortDirection = GeneralReservationSystem.Application.Common.SortDirection;

namespace GeneralReservationSystem.Web.Helpers
{
    public static class GridHelpers
    {
        public static IList<Filter> BuildFilters<T>(GridState<T> state)
        {
            var filters = new List<Filter>();

            if (state?.FilterDefinitions is null)
                return filters;

            foreach (var def in state.FilterDefinitions)
            {
                var field = def?.Column?.PropertyName;
                if (string.IsNullOrWhiteSpace(field))
                    continue;

                var op = FilterOperatorExtensions.ToFilterOperator(def!.Operator!);
                var value = def!.Value;

                filters.Add(new Filter(field!, op, value));
            }

            return filters;
        }

        public static IList<SortOption> BuildOrders<T>(GridState<T> state)
        {
            var orders = new List<SortOption>();

            if (state?.SortDefinitions is null)
                return orders;

            foreach (var def in state.SortDefinitions)
            {
                var field = def.SortBy;
                if (string.IsNullOrWhiteSpace(field))
                    continue;

                var direction = def.Descending
                    ? SortDirection.Desc
                    : SortDirection.Asc;

                orders.Add(new SortOption(field!, direction));
            }

            return orders;
        }
    }
}
