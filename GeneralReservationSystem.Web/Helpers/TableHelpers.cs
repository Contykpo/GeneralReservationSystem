using GeneralReservationSystem.Application.Common;
using MudBlazor;
using AppFilterOperator = GeneralReservationSystem.Application.Common.FilterOperator;
using SortDirection = GeneralReservationSystem.Application.Common.SortDirection;

namespace GeneralReservationSystem.Web.Helpers
{
    public static class TableHelpers
    {
        public static IList<SortOption> BuildOrders(TableState state)
        {
            List<SortOption> orders = [];

            if (string.IsNullOrWhiteSpace(state.SortLabel))
            {
                return orders;
            }

            SortDirection direction = state.SortDirection == MudBlazor.SortDirection.Descending
                ? SortDirection.Desc
                : SortDirection.Asc;

            orders.Add(new SortOption(state.SortLabel, direction));

            return orders;
        }
    }
}
