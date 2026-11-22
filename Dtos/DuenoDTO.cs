using System;
using System.ComponentModel.DataAnnotations;

namespace Software_SARA.Dtos
{
    public class DuenoDTO
    {
        [Required(ErrorMessage = "El tipo de documento es obligatorio")]
        public string TipoDoc { get; set; } = "CC";

        [Required(ErrorMessage = "El número de documento es obligatorio")]
        [RegularExpression(@"^[0-9]{5,10}$", ErrorMessage = "El documento debe tener entre 5 y 10 números, sin puntos.")]
        public string NumDoc { get; set; } = string.Empty;

        [Required(ErrorMessage = "Los nombres son obligatorios")]
        [StringLength(50, ErrorMessage = "Máximo 50 caracteres")]
        public string NomUsuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "Los apellidos son obligatorios")]
        [StringLength(50, ErrorMessage = "Máximo 50 caracteres")]
        public string ApelliUsuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es obligatorio")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@(gmail\.com|outlook\.com|ucundinamarca\.edu\.co)$", ErrorMessage = "El correo debe ser @gmail.com, @outlook.com o @ucundinamarca.edu.co")]
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "El teléfono debe tener  10 números.")]
        public string Telefono { get; set; } = string.Empty;

        [Required(ErrorMessage = "La dirección es obligatoria")]
        public string DirUsuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fecha de nacimiento es obligatoria")]
        public DateOnly FechaNaci { get; set; }

        [Required(ErrorMessage = "La sede es obligatoria")]
        public int IdSede { get; set; }
    }
}