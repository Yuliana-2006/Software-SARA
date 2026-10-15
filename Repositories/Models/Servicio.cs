using System;
using System.Collections.Generic;

namespace Software_SARA.Repositories.Models;

public partial class Servicio
{
    public int IdServicio { get; set; }

    public string NomServicio { get; set; } = null!;

    public virtual ICollection<Factura> Facturas { get; set; } = new List<Factura>();
}
