using System;
using System.Collections.Generic;

namespace Software_SARA.Repositories.Models;

public partial class Proveedor
{
    public int IdProveedor { get; set; }

    public string NomProveedor { get; set; } = null!;

    public virtual ICollection<Factura> Facturas { get; set; } = new List<Factura>();

    public virtual ICollection<Recordatorio> Recordatorios { get; set; } = new List<Recordatorio>();
}
