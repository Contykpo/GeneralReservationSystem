using System.ComponentModel.DataAnnotations;

namespace GeneralReservationSystem.Application.DTOs
{
    public class CreateTripDto
    {
        [Required(ErrorMessage = "El Id de vehículo es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El Id de vehículo debe ser un número positivo.")]
        public int VehicleId { get; set; }

        [Required(ErrorMessage = "El Id de salida es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El Id de salida debe ser un número positivo.")]
        public int DepartureId { get; set; }

        [Required(ErrorMessage = "El Id de destino es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El Id de destino debe ser un número positivo.")]
        public int DestinationId { get; set; }

        [Required(ErrorMessage = "El Id de conductor es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El Id de conductor debe ser un número positivo.")]
        public int DriverId { get; set; }

        [Required(ErrorMessage = "La fecha de salida es obligatoria.")]
        [DataType(DataType.DateTime)]
        public DateTime DepartureTime { get; set; }

        [Required(ErrorMessage = "La fecha de llegada es obligatoria.")]
        [DataType(DataType.DateTime)]
        public DateTime ArrivalTime { get; set; }
    }
}
