using System;
using System.Collections.Generic;

namespace Software_SARA.Repositories.Models;

public partial class Sede
{
    public int IdSede { get; set; }

    public string NomSede { get; set; } = null!;

    public string DirSede { get; set; } = null!;

    public string TelSede { get; set; } = null!;

    public bool EstadoSede { get; set; }

    public string CiuSede { get; set; } = null!;

    public string DepSede { get; set; } = null!;

    public string BarrioSede { get; set; } = null!;

    public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
