using System;
using System.Collections.Generic;

namespace Software_SARA.Dtos
{
    public class SustentoDiarioDTO
    {
        public List<SustentoItemDTO> Facturas { get; set; } = new List<SustentoItemDTO>();
        public decimal TotalSustento { get; set; }
        public DateTime FechaCalculo { get; set; }
    }

    public class SustentoItemDTO
    {
        public int IdFactura { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string NombreTercero { get; set; } = string.Empty;
        // ELIMINADO: public string Concepto { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public decimal Valor { get; set; }
    }
}