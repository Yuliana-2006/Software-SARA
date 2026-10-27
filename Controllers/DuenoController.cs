using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace Software_SARA.Controllers
{
    public class DuenoController : Controller
    {
        public IActionResult Index()
        {
            System.Console.WriteLine("[DEBUG DUENO] Entrando al controlador Dueno - Index");

            // Aseguramos que las variables de sesión existan para que ningún filtro las rechace
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UsuarioRol")))
            {
                HttpContext.Session.SetString("UsuarioRol", "Dueño");
                HttpContext.Session.SetString("UsuarioNombre", "Dueño Sistema");
            }

            return View();
        }
    }
}