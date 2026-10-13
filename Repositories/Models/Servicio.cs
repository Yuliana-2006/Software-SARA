using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Software_SARA.Models
{
    [Table("servicio")]
    public class Servicio
    {
        [Key]
        [Column("id_servicio")]
        public int IdServicio { get; set; }

        [Column("nom_servicio")]
        [StringLength(50)]
        public string NombreServicio { get; set; } = string.Empty;
    }
}