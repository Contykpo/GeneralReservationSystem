using GeneralReservationSystem.Application.Common;
using System.ComponentModel.DataAnnotations;
using KeyAttribute = GeneralReservationSystem.Application.Common.KeyAttribute;

namespace GeneralReservationSystem.Application.Entities
{
    public class Station
    {
        [Key]
        [Computed]
        public int StationId { get; set; }

        [Required(ErrorMessage = "El nombre de la estación es obligatorio.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre de la estación debe tener entre 2 y 100 caracteres.")]
        [RegularExpression(@"^[\p{L}\s'-]+$", ErrorMessage = "El nombre de la estación solo puede contener letras, espacios, apóstrofes o guiones.")]
        public string StationName { get; set; } = null!;
        [Computed]
        public string NormalizedStationName { get; set; } = null!;

        [Required(ErrorMessage = "La direccion es obligatoria.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "La direccion debe tener entre 2 y 50 caracteres.")]
        [RegularExpression(@"^[\p{L}\s'-]+$", ErrorMessage = "La direccion solo puede contener letras, espacios, apóstrofes o guiones.")]
        public string Address { get; set; } = null!;
        [Computed]
        public string NormalizedAddress { get; set; } = null!;

        [Required(ErrorMessage = "La ciudad es obligatoria.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "La ciudad debe tener entre 2 y 50 caracteres.")]
        [RegularExpression(@"^[\p{L}\s'-]+$", ErrorMessage = "La ciudad solo puede contener letras, espacios, apóstrofes o guiones.")]
        public string City { get; set; } = null!;
        [Computed]
        public string NormalizedCity { get; set; } = null!;

        [Required(ErrorMessage = "La provincia es obligatoria.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "La provincia debe tener entre 2 y 50 caracteres.")]
        [RegularExpression(@"^[\p{L}\s'-]+$", ErrorMessage = "La provincia solo puede contener letras, espacios, apóstrofes o guiones.")]
        public string Province { get; set; } = null!;
        [Computed]
        public string NormalizedProvince { get; set; } = null!;

        [Required(ErrorMessage = "El país es obligatorio.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "El país debe tener entre 2 y 50 caracteres.")]
        [RegularExpression(@"^[\p{L}\s'-]+$", ErrorMessage = "El país solo puede contener letras, espacios, apóstrofes o guiones.")]
        public string Country { get; set; } = null!;
        [Computed]
        public string NormalizedCountry { get; set; } = null!;
    }
}
