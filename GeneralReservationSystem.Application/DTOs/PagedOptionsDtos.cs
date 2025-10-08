using System.ComponentModel.DataAnnotations;

namespace GeneralReservationSystem.Application.DTOs
{
    public class PaginationOptions
    {
        [Range(1, int.MaxValue, ErrorMessage = "El número de página debe ser mayor o igual a 1.")]
        public int PageNumber { get; set; }

        [Range(1, 1000, ErrorMessage = "El tamaño de página debe ser entre 1 y 1000.")]
        public int PageSize { get; set; }
    }

    public class OrderingOptionsDto<TOrderBy>
    {
        public required TOrderBy OrderBy { get; set; }
        public bool Ascending { get; set; } = true;
    }

    public class PagedRequestDto
    {
        [Required(ErrorMessage = "La configuración de paginación es requerida.")]
        public required PaginationOptions PaginationOptions { get; set; }
    }

    public class PagedOrderedRequestDto<TOrderBy> : PagedRequestDto
    {
        public OrderingOptionsDto<TOrderBy>? OrderingOptions { get; set; }
    }
}
