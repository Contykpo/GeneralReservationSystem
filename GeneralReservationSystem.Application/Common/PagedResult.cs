using System.Linq.Expressions;

namespace GeneralReservationSystem.Application.Common
{
    public sealed class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages =>
            (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
