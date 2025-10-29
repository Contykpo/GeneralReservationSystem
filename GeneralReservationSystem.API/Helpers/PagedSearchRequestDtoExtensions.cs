using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;

namespace GeneralReservationSystem.API.Helpers
{
    public static class PagedSearchRequestDtoExtensions
    {
        private static string UnescapePipe(string input)
        {
            return input.Replace("[PIPE]", "|");
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
                    string property = UnescapePipe(Uri.UnescapeDataString(parts[0]));
                    FilterOperator op = Enum.TryParse(UnescapePipe(Uri.UnescapeDataString(parts[1])), out FilterOperator fop) ? fop : FilterOperator.Equals;
                    object? value = UnescapePipe(Uri.UnescapeDataString(parts[2]));
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
                    string property = UnescapePipe(Uri.UnescapeDataString(parts[0]));
                    SortDirection dir = parts.Length > 1 && Enum.TryParse(UnescapePipe(Uri.UnescapeDataString(parts[1])), out SortDirection sd) ? sd : SortDirection.Asc;
                    dto.Orders.Add(new SortOption(property, dir));
                }
            }
        }
    }
}
