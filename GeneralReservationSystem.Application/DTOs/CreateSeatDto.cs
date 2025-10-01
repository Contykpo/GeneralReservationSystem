using System.ComponentModel.DataAnnotations;

namespace GeneralReservationSystem.Application.DTOs
{
    public class CreateSeatDto
    {
        [Required(ErrorMessage = "El Id de modelo de vehículo es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El Id de modelo de vehículo debe ser un número positivo.")]
        public int VehicleModelId { get; set; }

        [Required(ErrorMessage = "La fila del asiento es obligatoria.")]
        [Range(1, int.MaxValue, ErrorMessage = "La fila del asiento debe ser al menos 1.")]
        public int SeatRow { get; set; }

        [Required(ErrorMessage = "La columna del asiento es obligatoria.")]
        [Range(1, int.MaxValue, ErrorMessage = "La columna del asiento debe ser al menos 1.")]
        public int SeatColumn { get; set; }

        public bool IsAtWindow { get; set; } = false;
        public bool IsAtAisle { get; set; } = false;
        public bool IsInFront { get; set; } = false;
        public bool IsInBack { get; set; } = false;
        public bool IsAccessible { get; set; } = false;
    }

    public class CreateSeatForVehicleModelDto
    {
        [Required(ErrorMessage = "La fila del asiento es obligatoria.")]
        [Range(1, int.MaxValue, ErrorMessage = "La fila del asiento debe ser al menos 1.")]
        public int SeatRow { get; set; }

        [Required(ErrorMessage = "La columna del asiento es obligatoria.")]
        [Range(1, int.MaxValue, ErrorMessage = "La columna del asiento debe ser al menos 1.")]
        public int SeatColumn { get; set; }

        public bool IsAtWindow { get; set; } = false;
        public bool IsAtAisle { get; set; } = false;
        public bool IsInFront { get; set; } = false;
        public bool IsInBack { get; set; } = false;
        public bool IsAccessible { get; set; } = false;
    }
}
