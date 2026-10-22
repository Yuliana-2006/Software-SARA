using System;
using System.Collections.Generic;

namespace Software_SARA.Repositories.Models;

public partial class DiaCaja
{
    public int IdDiaCaja { get; set; }

    public int IdUsuario { get; set; }

    public decimal MontoInicial { get; set; }

    public DateTime FechaHoraApertura { get; set; }

    public decimal MontoFinal { get; set; }

    public DateTime FechaHoraCierre { get; set; }

    public bool EstadoDia { get; set; }

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
