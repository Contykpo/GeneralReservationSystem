using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;

namespace GeneralReservationSystem.API.Helpers
{
    public static class PagedSearchRequestDtoExtensions
    {
        private static string UnescapeInput(string input)
        {
            return input.Replace("[PIPE]", "|").Replace("[COMMA]", ",");
        }

        public static void PopulateFromQuery(this PagedSearchRequestDto dto, IQueryCollection query)
        {
            dto.Page = int.TryParse(query["page"], out int p) ? p : 1;
            dto.PageSize = int.TryParse(query["pageSize"], out int ps) ? ps : 20;
            dto.Filters = [];
            dto.Orders = [];

            foreach (string? filterStr in query["filters"])
            {
                if (string.IsNullOrEmpty(filterStr))
                {
                    continue;
                }

                string[] parts = filterStr.Split('|');
                if (parts.Length == 3)
                {
                    string property = UnescapeInput(Uri.UnescapeDataString(parts[0]));
                    string opStr = UnescapeInput(Uri.UnescapeDataString(parts[1]));
                    FilterOperator op = Enum.TryParse(opStr, out FilterOperator fop) ? fop : FilterOperator.Equals;
                    string valueStr = UnescapeInput(Uri.UnescapeDataString(parts[2]));
                    object? value;
                    if (op == FilterOperator.Between)
                    {
                        string[] vals = valueStr.Split(',', 2);
                        value = vals.Length == 2 ? new object[] { vals[0], vals[1] } : valueStr;
                    }
                    else
                    {
                        value = valueStr;
                    }
                    dto.Filters.Add(new Filter(property, op, value));
                }
            }
            foreach (string? orderStr in query["orders"])
            {
                if (string.IsNullOrEmpty(orderStr))
                {
                    continue;
                }

                string[] parts = orderStr.Split('|');
                if (parts.Length >= 1)
                {
                    string property = UnescapeInput(Uri.UnescapeDataString(parts[0]));
                    SortDirection dir = parts.Length > 1 && Enum.TryParse(UnescapeInput(Uri.UnescapeDataString(parts[1])), out SortDirection sd) ? sd : SortDirection.Asc;
                    dto.Orders.Add(new SortOption(property, dir));
                }
            }
        }
    }
}
