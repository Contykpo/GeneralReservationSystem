using GeneralReservationSystem.Application.Common;

namespace GeneralReservationSystem.Application.DTOs
{
    public class PagedSearchRequestDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public IList<Filter> Filters { get; set; } = [];
        public IList<SortOption> Orders { get; set; } = [];
    }
}