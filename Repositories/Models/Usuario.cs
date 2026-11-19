using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Software_SARA.Repositories.Models;

public partial class Usuario
{
    public int IdUsuario { get; set; }
    public int IdRol { get; set; }
    public bool TipDoc { get; set; }
    public int? IdSede { get; set; }

    [Required(ErrorMessage = "Los nombres son obligatorios")]
    public string NomUsuario { get; set; } = null!;

    [Required(ErrorMessage = "Los apellidos son obligatorios")]
    public string ApelliUsuario { get; set; } = null!;

    [Required(ErrorMessage = "El número de documento es obligatorio")]
    public int NumDoc { get; set; }

    [Required(ErrorMessage = "El correo electrónico es obligatorio")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido (ejemplo@dominio.com)")]
    public string Correo { get; set; } = null!;

    [Required(ErrorMessage = "El teléfono es obligatorio")]
    public string Telefono { get; set; } = null!;

    [Required(ErrorMessage = "La dirección es obligatoria")]
    public string DirUsuario { get; set; } = null!;

    // SIN [Required] AQUÍ
    public string? Contraseña { get; set; }

    public bool EstadoUsuario { get; set; }

    [Required(ErrorMessage = "La fecha de nacimiento es obligatoria")]
    public DateOnly FechaNaci { get; set; }

    public virtual ICollection<DiaCaja> DiaCajas { get; set; } = new List<DiaCaja>();
    public virtual ICollection<Factura> Facturas { get; set; } = new List<Factura>();
    public virtual Rol? IdRolNavigation { get; set; }
    public virtual Sede? IdSedeNavigation { get; set; }
    public virtual ICollection<Recordatorio> Recordatorios { get; set; } = new List<Recordatorio>();
}