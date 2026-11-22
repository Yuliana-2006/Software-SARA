namespace Software_SARA.Dtos
{
    public class ProveedorVencimientoDto
    {
        public int IdProveedor { get; set; }
        public string NombreProveedor { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public decimal SaldoPendiente { get; set; }
    }
}