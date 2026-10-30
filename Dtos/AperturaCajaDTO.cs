using System.ComponentModel.DataAnnotations;

namespace Software_SARA.Dtos
{
    public class AperturaCajaDTO
    {
        [Required(ErrorMessage = "El monto inicial es obligatorio")]
        [Range(1, 999999999, ErrorMessage = "El monto debe ser mayor a 0")]
        public decimal MontoInicial { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un responsable")]
        public int IdResponsable { get; set; }
    }
}