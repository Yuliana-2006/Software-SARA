using System;
using System.ComponentModel.DataAnnotations;

namespace Software_SARA.Dtos
{
    public class DuenoDTO
    {
        [Required(ErrorMessage = "El tipo de documento es obligatorio")]
        public string TipoDoc { get; set; } = "CC";

        [Required(ErrorMessage = "El número de documento es obligatorio")]
        [RegularExpression(@"^[0-9]{8,10}$", ErrorMessage = "El documento debe tener entre 8 y 10 números, sin letras.")]
        public string NumDoc { get; set; } = string.Empty;

        [Required(ErrorMessage = "Los nombres son obligatorios")]
        [StringLength(50, ErrorMessage = "Máximo 50 caracteres")]
        public string NomUsuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "Los apellidos son obligatorios")]
        [StringLength(50, ErrorMessage = "Máximo 50 caracteres")]
        public string ApelliUsuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido")]
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "El teléfono debe tener exactamente 10 números.")]
        public string Telefono { get; set; } = string.Empty;

        [Required(ErrorMessage = "La dirección es obligatoria")]
        public string DirUsuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fecha de nacimiento es obligatoria")]
        public DateOnly FechaNaci { get; set; }
    }
}