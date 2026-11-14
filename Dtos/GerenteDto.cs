using System;

namespace Software_SARA.Dtos
{
    public class GerenteDto
    {
        public int Id { get; set; }
        public string? Nombres { get; set; }
        public string? Apellidos { get; set; }
        public string? TipoDocumento { get; set; }
        public string? NumeroDocumento { get; set; }
        public string? Correo { get; set; }
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
        public string? Sede { get; set; }
        public int IdSede { get; set; }
        public DateTime? FechaNacimiento { get; set; }
    }
}