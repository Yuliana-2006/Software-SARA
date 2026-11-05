using System.ComponentModel.DataAnnotations;

namespace Software_SARA.Dtos
{
    public class FacProveedorDto
    {
        public int IdFactura { get; set; }

        public int IdUsuario { get; set; }

        // Por defecto en 1 (true) para indicar que es factura de proveedor
        public bool TipFactura { get; set; } = true;

        [Required(ErrorMessage = "Debe seleccionar un proveedor.")]
        public int? IdProveedor { get; set; }

        // En las facturas de proveedores, el servicio va en nulo
        public int? IdServicio { get; set; } = null;

        [Required(ErrorMessage = "El valor de la factura es obligatorio.")]
        [Range(0.01, 99999999.99, ErrorMessage = "Ingrese un valor válido para la factura.")]
        public decimal ValorFactura { get; set; }

        [Required(ErrorMessage = "La fecha de registro es obligatoria.")]
        [DataType(DataType.Date)]
        public DateOnly FechaRegFactura { get; set; } = DateOnly.FromDateTime(DateTime.Now);

        [Required(ErrorMessage = "La fecha de vencimiento es obligatoria.")]
        [DataType(DataType.Date)]
        public DateOnly FechaVencimiento { get; set; }

        public bool EstadoFactura { get; set; } = true; // Activa por defecto

        public int Reponse { get; set; }

        public string Message { get; set; } = string.Empty;
    }
}
