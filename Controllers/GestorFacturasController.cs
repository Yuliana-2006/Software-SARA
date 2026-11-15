using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Software_SARA.Services;
using Software_SARA.Utilities;

namespace Software_SARA.Controllers
{
    [AuthorizeRole("Gerente", "Tesorero")]
 
    public class GestorFacturasController : Controller
    {
        private readonly FacProveedorService _facProveedorService;

        public GestorFacturasController(FacProveedorService facProveedorService)
        {
            _facProveedorService = facProveedorService;
        }

        // GET: FacProveedorController
        public ActionResult SelectFactura()
        {
            return View();
        }

        public async Task<ActionResult> HistorialFacturas()
        {
            var historial = await _facProveedorService.ObtenerHistorialFacturasProveedoresAsync();
            return View(historial);
        }
    }
}
