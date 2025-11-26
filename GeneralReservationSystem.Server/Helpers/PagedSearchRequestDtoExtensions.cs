using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;

namespace GeneralReservationSystem.Server.Helpers
{
    public static class PagedSearchRequestDtoExtensions
    {
        private static string UnescapeInput(string input)
        {
            return input.Replace("{PIPE}", "|")
                .Replace("{COMMA}", ",")
                .Replace("{COLON}", ":")
                .Replace("{LEFT_BRACKET}", "[")
                .Replace("{RIGHT_BRACKET}", "]");
        }

        public static void PopulateFromQuery(this PagedSearchRequestDto dto, IQueryCollection query)
        {
            dto.Page = int.TryParse(query["page"], out int p) ? p : 1;
            dto.PageSize = int.TryParse(query["pageSize"], out int ps) ? ps : 20;

            List<FilterClause> filterClauses = [];
            List<SortOption> orders = [];

            foreach (string? filterStr in query["filters"])
            {
                if (string.IsNullOrEmpty(filterStr))
                {
                    continue;
                }

                string clauseContent = filterStr.Trim();
                if (clauseContent.StartsWith('[') && clauseContent.EndsWith(']'))
                {
                    clauseContent = clauseContent[1..^1];
                }
                else
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(clauseContent))
                {
                    continue;
                }

                List<Filter> filters = [];

                string[] filterParts = clauseContent.Split(',');
                foreach (string filterPart in filterParts)
                {
                    string[] parts = filterPart.Split('|');
                    if (parts.Length == 3)
                    {
                        string property = UnescapeInput(Uri.UnescapeDataString(parts[0]));
                        string opStr = UnescapeInput(Uri.UnescapeDataString(parts[1]));
                        FilterOperator op = Enum.TryParse(opStr, out FilterOperator fop) ? fop : FilterOperator.Equals;
                        object value;
                        if (op == FilterOperator.Between)
                        {
                            string[] values = parts[2].Split(':');
                            if (values.Length != 2)
                            {
                                continue;
                            }
                            value = new object[]
                            {
                                UnescapeInput(Uri.UnescapeDataString(values[0])),
                                UnescapeInput(Uri.UnescapeDataString(values[1]))
                            };
                        }
                        else
                        {
                            string valueStr = UnescapeInput(Uri.UnescapeDataString(parts[2]));
                            value = valueStr;
                        }
                        filters.Add(new Filter(property, op, value));
                    }
                }

                if (filters.Count > 0)
                {
                    filterClauses.Add(new FilterClause(filters));
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
                    orders.Add(new SortOption(property, dir));
                }
            }

            dto.FilterClauses = filterClauses;
            dto.Orders = orders;
        }
    }
}
