using Microsoft.AspNetCore.Mvc;

namespace Software_SARA.Controllers
{
    // OJO: No le pongas [AuthorizeRole] para que cualquiera pueda ver el error
    public class ErrorController : Controller
    {
        // 1. Para errores del servidor (Error 500)
        [Route("Error")]
        public IActionResult Index()
        {
            return View();
        }

        // 2. Para errores de página no encontrada (Error 404 - "Host")
        [Route("Error/StatusCode/{code:int}")]
        public new IActionResult StatusCode(int code)
        {
            ViewBag.CodigoError = code;

            if (code == 404)
            {
                ViewBag.Mensaje = "La página que buscas no existe o fue movida (Error de Host).";
            }
            else
            {
                ViewBag.Mensaje = "Ocurrió un problema inesperado en el sistema.";
            }

            return View();
        }
    }
}