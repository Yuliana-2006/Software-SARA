using System.ComponentModel.DataAnnotations;

namespace Software_SARA.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Seleccione el tipo de documento")]
        public string TipoDocumento { get; set; }

        [Required(ErrorMessage = "Ingrese el número de documento")]
        public string NumeroDocumento { get; set; }

        [Required(ErrorMessage = "Ingrese la contraseña")]
        [DataType(DataType.Password)]
        public string Contrasena { get; set; }
    }
}