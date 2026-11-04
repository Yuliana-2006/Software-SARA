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

    // NUEVA PROPIEDAD PARA EL CIERRE
    public int? IdUsuarioCierre { get; set; }

    // Navegación
    public virtual Usuario? IdUsuarioNavigation { get; set; } // Quien abre
    public virtual Usuario? IdUsuarioCierreNavigation { get; set; } // Quien cierra
}