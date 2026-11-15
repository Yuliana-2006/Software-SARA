using System;
using System.ComponentModel.DataAnnotations;

namespace Software_SARA.Dtos
{
    public class FacServPublicDto
    {
        public int IdFactura { get; set; }
        public int IdUsuario { get; set; }
        public bool TipFactura { get; set; }
        public int? IdProveedor { get; set; }
        public int? IdServicio { get; set; }

        // Propiedades que venían de Develop
        public string? NomServicio { get; set; }

        [Required(ErrorMessage = "El valor de la factura es obligatorio.")]
        [Range(20000, 99999999.99, ErrorMessage = "El valor de la factura debe ser mayor o igual a $20,000.")]
        public decimal ValorFactura { get; set; }

        // Fechas como DateOnly
        public DateOnly FechaRegFactura { get; set; }
        public DateOnly FechaVencimiento { get; set; }

        public bool EstadoFactura { get; set; }

        // PROPIEDADES QUE FALTABAN
        public int Reponse { get; set; }

        // Propiedades que venían de Develop
        public string Message { get; set; } = string.Empty;
        public string? NomUsuario { get; set; } // Nombre del responsable que ingresó la factura
        public DateTime? FechaPago { get; set; } // Fecha en la que se confirmó el pago
    }
}