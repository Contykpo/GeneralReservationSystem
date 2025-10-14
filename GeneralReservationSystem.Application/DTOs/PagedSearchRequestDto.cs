using GeneralReservationSystem.Application.Common;
using System.ComponentModel.DataAnnotations;

namespace GeneralReservationSystem.Application.DTOs
{
    public class PagedSearchRequestDto
    {
        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        [Range(1, 1000)]
        public int PageSize { get; set; } = 20;

        public IList<Filter> Filters { get; set; } = [];

        public IList<SortOption> Orders { get; set; } = [];
    }
}