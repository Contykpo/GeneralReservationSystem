using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using System.Text;

namespace GeneralReservationSystem.Web.Client.Helpers
{
    public static class PagedSearchRequestDtoExtensions
    {
        private static string EscapePipe(string input)
        {
            return input.Replace("|", "[PIPE]");
        }

        public static string ToQueryString(this PagedSearchRequestDto dto)
        {
            StringBuilder sb = new();
            _ = sb.Append($"page={dto.Page}&pageSize={dto.PageSize}");
            if (dto.Filters != null && dto.Filters.Count > 0)
            {
                foreach (Filter filter in dto.Filters)
                {
                    string property = Uri.EscapeDataString(EscapePipe(filter.PropertyOrField));
                    string op = Uri.EscapeDataString(EscapePipe(filter.Operator.ToString()));
                    string value = Uri.EscapeDataString(EscapePipe(filter.Value?.ToString() ?? ""));
                    _ = sb.Append($"&filters={property}|{op}|{value}");
                }
            }
            if (dto.Orders != null && dto.Orders.Count > 0)
            {
                foreach (SortOption order in dto.Orders)
                {
                    string property = Uri.EscapeDataString(EscapePipe(order.PropertyOrField));
                    string dir = Uri.EscapeDataString(EscapePipe(order.Direction.ToString()));
                    _ = sb.Append($"&orders={property}|{dir}");
                }
            }
            return sb.ToString();
        }
    }
}
