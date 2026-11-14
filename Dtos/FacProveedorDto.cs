using System;
using System.ComponentModel.DataAnnotations;

namespace Software_SARA.Dtos
{
    public class FacProveedorDto
    {
        public int IdFactura { get; set; }
        public int IdUsuario { get; set; }
        public bool TipFactura { get; set; }
        public int? IdProveedor { get; set; }
        public string? NomProveedor { get; set; }
        public int? IdServicio { get; set; }
        public decimal ValorFactura { get; set; }

        // Fechas como DateOnly
        public DateOnly FechaRegFactura { get; set; }
        public DateOnly FechaVencimiento { get; set; }
        public DateTime? FechaPago { get; set; }

        public bool EstadoFactura { get; set; }
        public string? NomUsuario { get; set; }

        // PROPIEDADES QUE FALTABAN (Por eso daba error)
        public int Reponse { get; set; }
        public string? Message { get; set; }
    }
}