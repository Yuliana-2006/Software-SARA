using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Software_SARA.Models
{
    [Table("proveedor")]
    public class Proveedor
    {
        [Key]
        [Column("id_proveedor")]
        public int IdProveedor { get; set; }

        [Column("nom_proveedor")]
        [StringLength(50)]
        public string NombreProveedor { get; set; } = string.Empty;
    }
}