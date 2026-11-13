using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Software_SARA.Services;

namespace Software_SARA.Controllers
{
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
