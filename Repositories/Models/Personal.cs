using System;
using System.ComponentModel.DataAnnotations;

namespace Software_SARA.Models
{
    public class Personal
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "La sede es obligatoria")]
        public string SedeRegistro { get; set; } = "Sede Principal";

        [Required(ErrorMessage = "El tipo de documento es obligatorio")]
        public string TipoDoc { get; set; } = "CC";

        [Required(ErrorMessage = "El número de documento es obligatorio")]
        public string NumeroDoc { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string Nombres { get; set; } = string.Empty;

        [Required(ErrorMessage = "Los apellidos son obligatorios")]
        public string Apellidos { get; set; } = string.Empty;

        public string? Telefono { get; set; }

        [EmailAddress(ErrorMessage = "Correo electrónico no válido")]
        public string? Correo { get; set; }

        public string? Direccion { get; set; }

        [DataType(DataType.Date)]
        public DateTime? FechaNacimiento { get; set; }

        public string Rol { get; set; } = "Empleado";
    }
}