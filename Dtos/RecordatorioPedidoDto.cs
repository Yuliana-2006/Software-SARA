using System.ComponentModel.DataAnnotations;

namespace Software_SARA.Dtos
{
    public class RecordatorioPedidoDto
    {
        public int IdInterno { get; set; }

        [Required(ErrorMessage = "El proveedor es obligatorio.")]
        public string Proveedor { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fecha de entrega es obligatoria.")]
        [DataType(DataType.Date)]
        public DateOnly FechaEntrega { get; set; } = DateOnly.FromDateTime(DateTime.Now);

        [Required(ErrorMessage = "El ID del recordatorio es obligatorio.")]
        public string IdRecordatorio { get; set; } = string.Empty;

        public string Estado { get; set; } = string.Empty;

        public int Response { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}