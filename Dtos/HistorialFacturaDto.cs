namespace Software_SARA.Dtos
{
    public class HistorialFacturaDto
    {
        public int IdFactura { get; set; }
        public string? NomUsuario { get; set; }
        public string? NombreEntidad { get; set; }
        public decimal ValorFactura { get; set; }
        public DateOnly FechaRegFactura { get; set; }
        public DateOnly FechaVencimiento { get; set; }
        public DateOnly? FechaPago { get; set; }
        public bool EstadoFactura { get; set; }
    }
}