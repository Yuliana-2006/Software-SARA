using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Software_SARA.Repositories.Models;

[Table("factura")]
public partial class Factura
{
    public int IdFactura { get; set; }
    public int IdUsuario { get; set; }
    public bool TipFactura { get; set; }
    public int? IdProveedor { get; set; }
    public int? IdServicio { get; set; }
    public decimal ValorFactura { get; set; }

    // Fechas como DateOnly
    public DateOnly FechaRegFactura { get; set; }
    public DateOnly FechaVencimiento { get; set; }

    public bool EstadoFactura { get; set; }
    public string? Concepto { get; set; }

    [NotMapped]
    public string? NomProveedor { get; set; }
    [NotMapped]
    public string? NomServicio { get; set; }

    public virtual Proveedor? IdProveedorNavigation { get; set; }
    public virtual Servicio? IdServicioNavigation { get; set; }
    public virtual Usuario? IdUsuarioNavigation { get; set; }
}