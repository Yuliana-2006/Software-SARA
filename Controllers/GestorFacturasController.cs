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
        private readonly FacServPubService _facServPubService;

        public GestorFacturasController(FacProveedorService facProveedorService, FacServPubService facServPubService)
        {
            _facProveedorService = facProveedorService;
            _facServPubService = facServPubService;
        }

        // GET: FacProveedorController
        public ActionResult SelectFactura()
        {
            return View();
        }

        public async Task<ActionResult> HistorialFacturas()
        {
            var historialProveedores = await _facProveedorService.ObtenerHistorialFacturasProveedoresAsync();
            var historialServicios = await _facServPubService.ObtenerHistorialFacturasServiciosPublicosAsync();
            var historialUnificado = historialProveedores.Concat(historialServicios)
                                                          .OrderByDescending(f => f.IdFactura)
                                                          .ToList();
            return View(historialUnificado);
        }
    }
}
