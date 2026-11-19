using System.ComponentModel.DataAnnotations;

namespace Software_SARA.Dtos
{
    public class SedeDto
    {
        public int IdSede { get; set; }

        [Required(ErrorMessage = "El nombre de la sede es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
        public string NomSede { get; set; } = string.Empty;

        [Required(ErrorMessage = "La dirección es obligatoria.")]
        [StringLength(100, ErrorMessage = "La dirección no puede superar los 100 caracteres.")]
        public string DirSede { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "El teléfono debe tener exactamente 10 números, sin letras ni caracteres.")]
        public string TelSede { get; set; } = string.Empty;

        public bool EstadoSede { get; set; } = true;

        [Required(ErrorMessage = "La ciudad es obligatoria.")]
        [StringLength(50, ErrorMessage = "La ciudad no puede superar los 50 caracteres.")]
        public string CiuSede { get; set; } = string.Empty;

        [Required(ErrorMessage = "El departamento es obligatorio.")]
        [StringLength(50, ErrorMessage = "El departamento no puede superar los 50 caracteres.")]
        public string DepSede { get; set; } = string.Empty;

        [Required(ErrorMessage = "El barrio es obligatorio.")]
        [StringLength(50, ErrorMessage = "El barrio no puede superar los 50 caracteres.")]
        public string BarrioSede { get; set; } = string.Empty;

        public int Reponse { get; set; }

        public string Message { get; set; } = string.Empty;
    }
}