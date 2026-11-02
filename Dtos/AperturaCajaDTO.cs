using System.ComponentModel.DataAnnotations;

namespace Software_SARA.Dtos
{
    public class AperturaCajaDTO
    {
        [Required(ErrorMessage = "El monto inicial es obligatorio")]
        [Range(10000, 999999999, ErrorMessage = "El monto debe ser mayor o igual a $10,000")]
        public decimal MontoInicial { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un responsable")]
        public int IdResponsable { get; set; }
    }
}