using Software_SARA.Repositories.Models;
using System.Collections.Generic;

namespace Software_SARA.Dtos
{
    public class SustentoDiarioDTO
    {
        public List<Factura> Facturas { get; set; } = new List<Factura>();
        public decimal TotalSustento { get; set; }
        public DateOnly FechaCalculo { get; set; }
    }
}