using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Software_SARA.Repositories.Models;

[Table("recordatorio")]
public partial class Recordatorio
{
    public int IdRecordatorio { get; set; }
    public int IdUsuario { get; set; }
    public int IdProveedor { get; set; }

    public DateOnly FechaRegRecordatorio { get; set; }
    public DateOnly FechaEntrega { get; set; }

    public bool EstadoRecordatorio { get; set; }

    public virtual Proveedor? IdProveedorNavigation { get; set; }
    public virtual Usuario? IdUsuarioNavigation { get; set; }
}