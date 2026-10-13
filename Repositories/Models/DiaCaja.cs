using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Software_SARA.Models
{
    [Table("dia_caja")]
    public class DiaCaja
    {
        [Key]
        [Column("id_dia_caja")]
        public int IdDiaCaja { get; set; }

        [Column("id_usuario")]
        public int IdUsuario { get; set; }
        [ForeignKey("IdUsuario")]
        public Usuario? Usuario { get; set; }

        [Column("monto_inicial")]
        public decimal MontoInicial { get; set; }

        [Column("fecha_hora_apertura")]
        public DateTime FechaHoraApertura { get; set; }

        [Column("monto_final")]
        public decimal MontoFinal { get; set; }

        [Column("fecha_hora_cierre")]
        public DateTime FechaHoraCierre { get; set; }

        [Column("estado_dia")]
        public bool EstadoDia { get; set; }
    }
}