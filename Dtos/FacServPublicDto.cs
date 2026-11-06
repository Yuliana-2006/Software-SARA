using System.ComponentModel.DataAnnotations;

namespace Software_SARA.Dtos
{
    public class FacServPublicDto
    {
        public int IdFactura { get; set; }

        public int IdUsuario { get; set; }

        public bool TipFactura { get; set; } = false;

        public int? IdProveedor { get; set; } = null;

        [Required(ErrorMessage = "Debe seleccionar un servicio público.")]
        public int? IdServicio { get; set; }

        [Required(ErrorMessage = "El valor de la factura es obligatorio.")]
        [Range(0.01, 99999999.99, ErrorMessage = "Ingrese un valor válido para la factura.")]
        public decimal ValorFactura { get; set; }

        [Required(ErrorMessage = "La fecha de registro es obligatoria.")]
        [DataType(DataType.Date)]
        public DateOnly FechaRegFactura { get; set; } = DateOnly.FromDateTime(DateTime.Now);

        [Required(ErrorMessage = "La fecha de vencimiento es obligatoria.")]
        [DataType(DataType.Date)]
        public DateOnly FechaVencimiento { get; set; }

        public bool EstadoFactura { get; set; } = true;

        public int Reponse { get; set; }

        public string Message { get; set; } = string.Empty;
    }
}