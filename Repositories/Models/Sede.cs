using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Software_SARA.Models
{
    [Table("sede")]
    public class Sede
    {
        [Key]
        [Column("id_sede")]
        public int IdSede { get; set; }

        [Column("nom_sede")]
        [StringLength(100)]
        public string NombreSede { get; set; } = string.Empty;

        [Column("dir_sede")]
        [StringLength(100)]
        public string DireccionSede { get; set; } = string.Empty;

        [Column("tel_sede")]
        public int TelefonoSede { get; set; }

        [Column("estado_sede")]
        public bool EstadoSede { get; set; }

        [Column("ciu_sede")]
        [StringLength(50)]
        public string CiudadSede { get; set; } = string.Empty;

        [Column("dep_sede")]
        [StringLength(50)]
        public string DepartamentoSede { get; set; } = string.Empty;

        [Column("barrio_sede")]
        [StringLength(50)]
        public string BarrioSede { get; set; } = string.Empty;
    }
}