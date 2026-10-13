using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Software_SARA.Models
{
    [Table("factura")]
    public class Factura
    {
        [Key]
        [Column("id_factura")]
        public int IdFactura { get; set; }

        [Column("id_usuario")]
        public int IdUsuario { get; set; }
        [ForeignKey("IdUsuario")]
        public Usuario? Usuario { get; set; }

        [Column("tip_factura")]
        public bool TipoFactura { get; set; }

        [Column("id_proveedor")]
        public int? IdProveedor { get; set; }
        [ForeignKey("IdProveedor")]
        public Proveedor? Proveedor { get; set; }

        [Column("id_servicio")]
        public int? IdServicio { get; set; }
        [ForeignKey("IdServicio")]
        public Servicio? Servicio { get; set; }

        [Column("valor_factura")]
        public decimal ValorFactura { get; set; }

        [Column("fecha_reg_factura")]
        public DateTime FechaRegistro { get; set; }

        [Column("fecha_vencimiento")]
        public DateTime FechaVencimiento { get; set; }

        [Column("estado_factura")]
        public bool EstadoFactura { get; set; }
    }
}