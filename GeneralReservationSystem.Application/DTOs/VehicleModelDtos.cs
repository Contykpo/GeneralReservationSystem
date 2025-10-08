using System.ComponentModel.DataAnnotations;

namespace GeneralReservationSystem.Application.DTOs
{
    public enum VehicleModelOrderBy
    {
        Name,
        Manufacturer
    }

    // NOTE: Sanitization is handled in a lower layer.
    public class VehicleModelSearchRequestDto : PagedOrderedRequestDto<VehicleModelOrderBy>
    {
        public string? Name { get; set; }
        public string? Manufacturer { get; set; }
    }

    public class CreateVehicleModelDto
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 50 caracteres.")]
        [RegularExpression(@"^[\p{L}\s'-]+$", ErrorMessage = "El nombre solo puede contener letras, espacios, apóstrofes o guiones.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "El fabricante es obligatorio.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "El fabricante debe tener entre 2 y 50 caracteres.")]
        [RegularExpression(@"^[\p{L}\s'-]+$", ErrorMessage = "El fabricante solo puede contener letras, espacios, apóstrofes o guiones.")]
        public string Manufacturer { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe proporcionar al menos un asiento.")]
        [MinLength(1, ErrorMessage = "Debe proporcionar al menos un asiento.")]
        public List<CreateSeatForVehicleModelDto> Seats { get; set; } = new();
    }

    public class UpdateVehicleModelDto
    {
        [Required(ErrorMessage = "El identificador es obligatorio.")]
        public int VehicleModelId { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 50 caracteres.")]
        [RegularExpression(@"^[\p{L}\s'-]+$", ErrorMessage = "El nombre solo puede contener letras, espacios, apóstrofes o guiones.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "El fabricante es obligatorio.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "El fabricante debe tener entre 2 y 50 caracteres.")]
        [RegularExpression(@"^[\p{L}\s'-]+$", ErrorMessage = "El fabricante solo puede contener letras, espacios, apóstrofes o guiones.")]
        public string Manufacturer { get; set; } = string.Empty;
    }
}
