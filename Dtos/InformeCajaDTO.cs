using Software_SARA.Repositories.Models;

namespace Software_SARA.Dtos
{
    public class InformeCajaDTO
    {
        public DateTime Fecha { get; set; }
        public string NombreSede { get; set; } = string.Empty;

        public List<DiaCaja> Movimientos { get; set; } = new List<DiaCaja>();
        public decimal TotalMontoInicial { get; set; }
        public decimal TotalMontoFinal { get; set; }

        public List<Factura> FacturasPendientes { get; set; } = new List<Factura>();
        public List<Factura> FacturasPagadas { get; set; } = new List<Factura>();

        public decimal TotalFacturasPendientes { get; set; }
        public decimal TotalFacturasPagadas { get; set; }

        public List<Usuario> UsuariosCorreo { get; set; } = new List<Usuario>();
    }
}