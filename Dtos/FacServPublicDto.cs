using System;

namespace Software_SARA.Dtos
{
    public class FacServPublicDto
    {
        public int IdFactura { get; set; }
        public int IdUsuario { get; set; }
        public string? NomUsuario { get; set; }
        public bool TipFactura { get; set; }
        public int? IdProveedor { get; set; }
        public string? NomProveedor { get; set; }
        public int? IdServicio { get; set; }
        public string? NomServicio { get; set; }
        public decimal ValorFactura { get; set; }
        public DateOnly FechaRegFactura { get; set; }
        public DateOnly FechaVencimiento { get; set; }
        public DateTime? FechaPago { get; set; }
        public bool EstadoFactura { get; set; }
        public int Reponse { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}