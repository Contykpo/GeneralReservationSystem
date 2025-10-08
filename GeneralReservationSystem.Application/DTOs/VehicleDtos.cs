using System.ComponentModel.DataAnnotations;

namespace GeneralReservationSystem.Application.DTOs
{
    public enum VehicleStatus
    {
        Active,
        Inactive,
        Maintenance
    }

    public class VehicleDetailsDto
    {
        public int Id { get; set; }
        public string ModelName { get; set; } = null!;
        public string Manufacturer { get; set; } = null!;
        public string LicensePlate { get; set; } = null!;
        public VehicleStatus Status { get; set; }
    }

    public enum VehicleOrderBy
    {
        ModelName,
        Manufacturer,
        LicensePlate,
        Status
    }

    // NOTE: Sanitization is handled in a lower layer.
    public class VehicleDetailsSearchRequestDto : PagedOrderedRequestDto<VehicleOrderBy>
    {
        public string? ModelName { get; set; }
        public string? Manufacturer { get; set; }
        public string? LicensePlate { get; set; }
    }

    public class CreateVehicleDto
    {
        [Required(ErrorMessage = "El Id de modelo de vehículo es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El Id de modelo de vehículo debe ser un número positivo.")]
        public int VehicleModelId { get; set; }

        [Required(ErrorMessage = "La patente es obligatoria.")]
        [StringLength(7, MinimumLength = 6, ErrorMessage = "La patente debe tener entre 6 y 7 caracteres.")]
        [RegularExpression(@"^([A-Z]{3}\d{3}|[A-Z]{2}\d{3}[A-Z]{2})$", ErrorMessage = "La patente debe coincidir con los formatos: viejo (ABC123) o nuevo (AB123CD).")]
        public string LicensePlate { get; set; } = string.Empty;

        [Required(ErrorMessage = "El estado es obligatorio.")]
        [StringLength(20, ErrorMessage = "El estado debe tener menos de 20 caracteres.")]
        [RegularExpression(@"^(Active|Inactive|Maintenance)$", ErrorMessage = "El estado debe ser 'Active', 'Inactive' o 'Maintenance'.")]
        public string Status { get; set; } = string.Empty;
    }

    public class UpdateVehicleDto
    {
        [Required(ErrorMessage = "El identificador es obligatorio.")]
        public int VehicleId { get; set; }

        [Required(ErrorMessage = "El Id de modelo de vehículo es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El Id de modelo de vehículo debe ser un número positivo.")]
        public int VehicleModelId { get; set; }

        [Required(ErrorMessage = "La patente es obligatoria.")]
        [StringLength(7, MinimumLength = 6, ErrorMessage = "La patente debe tener entre 6 y 7 caracteres.")]
        [RegularExpression(@"^([A-Z]{3}\d{3}|[A-Z]{2}\d{3}[A-Z]{2})$", ErrorMessage = "La patente debe coincidir con los formatos: viejo (ABC123) o nuevo (AB123CD).")]
        public string LicensePlate { get; set; } = string.Empty;

        [Required(ErrorMessage = "El estado es obligatorio.")]
        [StringLength(20, ErrorMessage = "El estado debe tener menos de 20 caracteres.")]
        [RegularExpression(@"^(Active|Inactive|Maintenance)$", ErrorMessage = "El estado debe ser 'Active', 'Inactive' o 'Maintenance'.")]
        public string Status { get; set; } = string.Empty;
    }
}
