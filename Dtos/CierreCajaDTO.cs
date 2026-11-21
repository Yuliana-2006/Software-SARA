using System.ComponentModel.DataAnnotations;

namespace Software_SARA.Dtos
{
    public class CierreCajaDTO
    {
        [Required(ErrorMessage = "El monto final es obligatorio")]
        [Range(10000, 999999999, ErrorMessage = "El valor proporcionado no es válido.")]
        public decimal MontoFinal { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un responsable")]
        public int IdResponsable { get; set; }
    }
}