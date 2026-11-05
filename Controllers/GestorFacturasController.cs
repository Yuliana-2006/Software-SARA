using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Software_SARA.Controllers
{
    public class GestorFacturasController : Controller
    {
        // GET: FacProveedorController
        public ActionResult SelectFactura()
        {
            return View();
        }
    }
}
