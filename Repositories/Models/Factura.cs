using System;
using System.Collections.Generic;

namespace Software_SARA.Repositories.Models;

public partial class Factura
{
    public int IdFactura { get; set; }

    public int IdUsuario { get; set; }

    public bool TipFactura { get; set; }

    public int? IdProveedor { get; set; }

    public string? NomProveedor { get; set; }

    public int? IdServicio { get; set; }

    public decimal ValorFactura { get; set; }

    public DateOnly FechaRegFactura { get; set; }

    public DateOnly FechaVencimiento { get; set; }

    public bool EstadoFactura { get; set; }

    public virtual Proveedor? IdProveedorNavigation { get; set; }

    public virtual Servicio? IdServicioNavigation { get; set; }

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
