using System;
using System.Collections.Generic;

namespace Software_SARA.Repositories.Models;

public partial class Usuario
{
    public int IdUsuario { get; set; }

    public int IdRol { get; set; }

    public bool TipDoc { get; set; }

    public int IdSede { get; set; }

    public string NomUsuario { get; set; } = null!;

    public string ApelliUsuario { get; set; } = null!;

    public int NumDoc { get; set; }

    public string Correo { get; set; } = null!;

    public string Telefono { get; set; } = null!;

    public string DirUsuario { get; set; } = null!;

    public string Contraseña { get; set; } = null!;

    public bool EstadoUsuario { get; set; }

    public DateOnly FechaNaci { get; set; }

    public virtual ICollection<DiaCaja> DiaCajas { get; set; } = new List<DiaCaja>();

    public virtual ICollection<Factura> Facturas { get; set; } = new List<Factura>();

    public virtual Rol IdRolNavigation { get; set; } = null!;

    public virtual Sede IdSedeNavigation { get; set; } = null!;

    public virtual ICollection<Recordatorio> Recordatorios { get; set; } = new List<Recordatorio>();
}
