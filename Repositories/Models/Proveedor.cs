using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Software_SARA.Repositories.Models;

[Table("proveedor")]
public partial class Proveedor
{
    public int IdProveedor { get; set; }

    [Column("nom_proveedor")]
    public string NomProveedor { get; set; } = null!;

    public virtual ICollection<Factura> Facturas { get; set; } = new List<Factura>();
    public virtual ICollection<Recordatorio> Recordatorios { get; set; } = new List<Recordatorio>();
}