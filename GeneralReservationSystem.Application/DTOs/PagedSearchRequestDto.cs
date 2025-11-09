using GeneralReservationSystem.Application.Common;

namespace GeneralReservationSystem.Application.DTOs
{
    public class PagedSearchRequestDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public IEnumerable<FilterClause> FilterClauses { get; set; } = [];
        public IEnumerable<SortOption> Orders { get; set; } = [];
    }
}