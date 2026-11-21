using System;
using System.ComponentModel.DataAnnotations;

namespace Software_SARA.Dtos
{
    public class GerenteDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Los nombres son obligatorios.")]
        [StringLength(50, ErrorMessage = "Los nombres no pueden superar los 50 caracteres.")]
        public string? Nombres { get; set; }

        [Required(ErrorMessage = "Los apellidos son obligatorios.")]
        [StringLength(50, ErrorMessage = "Los apellidos no pueden superar los 50 caracteres.")]
        public string? Apellidos { get; set; }

        [Required(ErrorMessage = "El tipo de documento es obligatorio.")]
        public string? TipoDocumento { get; set; }

        [Required(ErrorMessage = "El número de documento es obligatorio.")]
        [RegularExpression(@"^[0-9]{5,10}$", ErrorMessage = "El documento debe tener entre 5 y 10 números, sin letras ni caracteres.")]
        public string? NumeroDocumento { get; set; }

        [Required(ErrorMessage = "El correo es obligatorio.")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@(gmail\.com|outlook\.com|ucundinamarca\.edu\.co)$", ErrorMessage = "El correo debe ser @gmail.com, @outlook.com o @ucundinamarca.edu.co.")]
        public string? Correo { get; set; }

        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "El teléfono debe tener exactamente 10 números, sin letras ni caracteres.")]
        public string? Telefono { get; set; }

        [Required(ErrorMessage = "La dirección es obligatoria.")]
        [StringLength(120, ErrorMessage = "La dirección no puede superar los 120 caracteres.")]
        public string? Direccion { get; set; }

        public string? Sede { get; set; }

        [Required(ErrorMessage = "La sede es obligatoria.")]
        [Range(1, int.MaxValue, ErrorMessage = "La sede es obligatoria.")]
        public int? IdSede { get; set; }

        [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
        public DateTime? FechaNacimiento { get; set; }
    }
}