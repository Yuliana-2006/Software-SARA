using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Software_SARA.Repositories.Models;

[Table("servicio")]
public partial class Servicio
{
    public int IdServicio { get; set; }

    [Column("nom_servicio")]
    public string NomServicio { get; set; } = null!;

    public virtual ICollection<Factura> Facturas { get; set; } = new List<Factura>();
}
