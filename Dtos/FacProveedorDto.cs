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

        public string? NomProveedor { get; set; }

        // En las facturas de proveedores, el servicio va en nulo
        public int? IdServicio { get; set; } = null;

        [Required(ErrorMessage = "El valor de la factura es obligatorio.")]
        [Range(10000.00, 99999999.99, ErrorMessage = "Ingrese un valor mayor o igual a $10,000 para la factura.")]
        public decimal ValorFactura { get; set; }

        [DataType(DataType.Date)]
        public DateOnly FechaRegFactura { get; set; } = DateOnly.FromDateTime(DateTime.Now);

        [DataType(DataType.Date)]
        public DateOnly FechaVencimiento { get; set; } = DateOnly.FromDateTime(DateTime.Now);

        public bool EstadoFactura { get; set; } = true; // Activa por defecto

        public int Reponse { get; set; }

        public string Message { get; set; } = string.Empty;

        public string? NomUsuario { get; set; } // Nombre del responsable que ingresó la factura

        public DateTime? FechaPago { get; set; } // Fecha en la que se confirmó el pago
    }
}
