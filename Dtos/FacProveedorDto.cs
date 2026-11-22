using System;
using System.ComponentModel.DataAnnotations;

namespace Software_SARA.Dtos
{
    public class FacProveedorDto
    {
        public int IdFactura { get; set; }
        public int IdUsuario { get; set; }
        public bool TipFactura { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un proveedor.")]
        public int? IdProveedor { get; set; }

        public string? NomProveedor { get; set; }
        public int? IdServicio { get; set; }

        [Required(ErrorMessage = "El valor de la factura es obligatorio.")]
        [Range(10000.01, double.MaxValue, ErrorMessage = "El valor de la factura debe ser mayor a $10.000.")]
        public decimal ValorFactura { get; set; }

        // Fechas como DateOnly
        [Required(ErrorMessage = "La fecha de registro es obligatoria.")]
        public DateOnly FechaRegFactura { get; set; }

        [Required(ErrorMessage = "La fecha de vencimiento es obligatoria.")]
        public DateOnly FechaVencimiento { get; set; }

        public DateTime? FechaPago { get; set; }

        public bool EstadoFactura { get; set; }
        public string? NomUsuario { get; set; }

        // PROPIEDADES QUE FALTABAN (Por eso daba error)
        public int Reponse { get; set; }
        public string? Message { get; set; }
    }
}