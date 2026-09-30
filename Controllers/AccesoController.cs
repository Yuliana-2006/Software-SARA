using Microsoft.AspNetCore.Mvc;
using Software_SARA.Models;
using Microsoft.AspNetCore.Http;

namespace Software_SARA.Controllers
{
    public class AccesoController : Controller
    {
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            // Validamos tipo, número de documento y contraseña (datos de prueba para la interfaz)
            string rolUsuario = ValidarEnBaseDeDatos(modelo.TipoDocumento, modelo.NumeroDocumento, modelo.Contrasena);

            if (rolUsuario != null)
            {
                // Guardamos el rol y el documento en la sesión
                HttpContext.Session.SetString("UsuarioRol", rolUsuario);
                HttpContext.Session.SetString("UsuarioDoc", modelo.NumeroDocumento);

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Documento o contraseña incorrectos.";
            return View(modelo);
        }

        public IActionResult CerrarSesion()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        private string ValidarEnBaseDeDatos(string tipoDoc, string numDoc, string clave)
        {
            // Pruebas iniciales para probar cada rol
            if (numDoc == "12345" && clave == "1234") return "Dueño";
            if (numDoc == "67890" && clave == "1234") return "Gerente";
            if (numDoc == "11223" && clave == "1234") return "Tesorero";
            if (numDoc == "44556" && clave == "1234") return "Empleado";

            return null;
        }
    }
}
