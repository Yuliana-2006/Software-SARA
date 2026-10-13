using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Software_SARA.Models
{
    [Table("recordatorio")]
    public class Recordatorio
    {
        [Key]
        [Column("id_recordatorio")]
        public int IdRecordatorio { get; set; }

        [Column("id_usuario")]
        public int IdUsuario { get; set; }
        [ForeignKey("IdUsuario")]
        public Usuario? Usuario { get; set; }

        [Column("id_proveedor")]
        public int IdProveedor { get; set; }
        [ForeignKey("IdProveedor")]
        public Proveedor? Proveedor { get; set; }

        [Column("fecha_reg_recordatorio")]
        public DateTime FechaRegistro { get; set; }

        [Column("fecha_entrega")]
        public DateTime FechaEntrega { get; set; }

        [Column("estado_recordatorio")]
        public bool EstadoRecordatorio { get; set; }
    }
}