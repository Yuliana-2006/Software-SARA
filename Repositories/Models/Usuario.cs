using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Software_SARA.Models
{
    [Table("usuario")]
    public class Usuario
    {
        [Key]
        [Column("id_usuario")]
        public int IdUsuario { get; set; }

        [Column("id_rol")]
        public int IdRol { get; set; }
        [ForeignKey("IdRol")]
        public Rol? Rol { get; set; }

        [Column("tip_doc")]
        public bool TipoDoc { get; set; }

        [Column("id_sede")]
        public int IdSede { get; set; }
        [ForeignKey("IdSede")]
        public Sede? Sede { get; set; }

        [Column("nom_usuario")]
        [StringLength(50)]
        public string Nombres { get; set; } = string.Empty;

        [Column("apelli_usuario")]
        [StringLength(50)]
        public string Apellidos { get; set; } = string.Empty;

        [Column("num_doc")]
        public int NumeroDoc { get; set; }

        [Column("correo")]
        [StringLength(100)]
        public string Correo { get; set; } = string.Empty;

        [Column("telefono")]
        public int Telefono { get; set; }

        [Column("dir_usuario")]
        [StringLength(100)]
        public string Direccion { get; set; } = string.Empty;

        [Column("contraseña")]
        [StringLength(250)]
        public string Contrasena { get; set; } = string.Empty;

        [Column("estado_usuario")]
        public bool Estado { get; set; }

        [Column("fecha_naci")]
        public DateTime FechaNacimiento { get; set; }
    }
}