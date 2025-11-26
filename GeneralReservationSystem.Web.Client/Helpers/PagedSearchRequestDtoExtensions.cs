using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using System.Text;

namespace GeneralReservationSystem.Web.Client.Helpers
{
    public static class PagedSearchRequestDtoExtensions
    {
        private static string EscapeInput(object? input)
        {
            string str = input switch
            {
                DateTime dt => dt.ToString("s"),
                _ => input?.ToString() ?? ""
            };
            return str.Replace("|", "{PIPE}")
                .Replace(",", "{COMMA}")
                .Replace(":", "{COLON}")
                .Replace("[", "{LEFT_BRACKET}")
                .Replace("]", "{RIGHT_BRACKET}");
        }

        public static string ToQueryString(this PagedSearchRequestDto dto)
        {
            StringBuilder sb = new();
            _ = sb.Append($"page={dto.Page}&pageSize={dto.PageSize}");
            if (dto.FilterClauses != null && dto.FilterClauses.Any())
            {
                foreach (FilterClause filterClause in dto.FilterClauses)
                {
                    _ = sb.Append("&filters=[");
                    List<string> clauseFilters = [];
                    foreach (Filter filter in filterClause.Filters)
                    {
                        string property = Uri.EscapeDataString(EscapeInput(filter.PropertyOrField));
                        string op = Uri.EscapeDataString(EscapeInput(filter.Operator.ToString()));
                        string value;
                        if (filter.Operator == FilterOperator.Between && filter.Value is object[] arr && arr.Length == 2)
                        {
                            string v1 = EscapeInput(arr[0]);
                            string v2 = EscapeInput(arr[1]);
                            value = Uri.EscapeDataString($"{v1}:{v2}");
                        }
                        else
                        {
                            value = Uri.EscapeDataString(EscapeInput(filter.Value));
                        }
                        clauseFilters.Add($"{property}|{op}|{value}");
                    }
                    _ = sb.Append(string.Join(",", clauseFilters));
                    _ = sb.Append(']');
                }
            }
            if (dto.Orders != null && dto.Orders.Any())
            {
                foreach (SortOption order in dto.Orders)
                {
                    string property = Uri.EscapeDataString(EscapeInput(order.PropertyOrField));
                    string dir = Uri.EscapeDataString(EscapeInput(order.Direction.ToString()));
                    _ = sb.Append($"&orders={property}|{dir}");
                }
            }
            return sb.ToString();
        }
    }
}
