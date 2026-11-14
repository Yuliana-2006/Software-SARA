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
        public decimal ValorFactura { get; set; }

        // Fechas como DateOnly
        public DateOnly FechaRegFactura { get; set; }
        public DateOnly FechaVencimiento { get; set; }

        public bool EstadoFactura { get; set; }

        // PROPIEDADES QUE FALTABAN
        public int Reponse { get; set; }
        public string? Message { get; set; }
    }
}