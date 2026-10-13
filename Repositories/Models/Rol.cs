using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Software_SARA.Models
{
    [Table("rol")]
    public class Rol
    {
        [Key]
        [Column("id_rol")]
        public int IdRol { get; set; }

        [Column("nom_rol")]
        [StringLength(50)]
        public string NombreRol { get; set; } = string.Empty;
    }
}