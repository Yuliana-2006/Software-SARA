using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Software_SARA.Dtos;
using Software_SARA.Services;

namespace Software_SARA.Controllers
{
    public class ServiciosPublicosController : Controller
    {
        private readonly FacServPubService _facServPubService;

        public ServiciosPublicosController(FacServPubService facServPubService)
        {
            _facServPubService = facServPubService;
        }

        // GET: ServiciosPublicosController
        public async Task<ActionResult> FacturasServiciosPublicos()
        {
            var facturas = await _facServPubService.ObtenerTodasLasFacturasServiciosPublicos();
            return View(facturas);
        }

        // GET: /Servicios/CrearFacturaServicioPublico
        [HttpGet]
        public async Task<ActionResult> CrearFacturaServicioPublico()
        {
            FacServPublicDto facServPublicDto = new();
            ViewBag.Servicios = await _facServPubService.ObtenerServiciosPublicos();
            return View(facServPublicDto);
        }

        // POST: /Servicios/CrearFacturaServicioPublico
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CrearFacturaServicioPublico(FacServPublicDto facServPublicDto)
        {
            try
            {
                string usuarioIdString = HttpContext.Session.GetString("UsuarioId");

                if (string.IsNullOrEmpty(usuarioIdString) || !int.TryParse(usuarioIdString, out int idUsuarioSesion))
                {
                    // Si no hay sesión o el ID no es válido, redirigimos al Login de Acceso
                    return RedirectToAction("Login", "Acceso");
                }

                // Asignamos el ID convertido a entero
                facServPublicDto.IdUsuario = idUsuarioSesion;

                // Validar el modelo
                if (!ModelState.IsValid)
                {
                    ViewBag.Servicios = await _facServPubService.ObtenerServiciosPublicos();
                    // Si el modelo no es válido, devolver la vista con los errores
                    var errors = ModelState.Values.SelectMany(v => v.Errors);
                    ViewData["Message"] = string.Join("; ", errors.Select(e => e.ErrorMessage));
                    return View(facServPublicDto);
                }

                var resultado = await _facServPubService.CreateFacturaServicioPublico(facServPublicDto);
                TempData["MensajeExito"] = resultado.Message;
                return RedirectToAction(nameof(FacturasServiciosPublicos));
            }
            catch (Exception ex)
            {
                ViewBag.Servicios = await _facServPubService.ObtenerServiciosPublicos();
                // Manejar cualquier excepción que ocurra durante la validación
                ViewData["Mensaje"] = "Error al crear factura de servicio público: " + ex.Message;
                return View(facServPublicDto);
            }
        }

        // GET: /Servicios/EditarFacturaServicioPublico
        [HttpGet]
        public async Task<ActionResult> EditarFacturaServicioPublico(int id)
        {
            var facServPublicDto = await _facServPubService.ObtenerFacturaServicioPublicoPorId(id);

            if (facServPublicDto == null)
            {
                return NotFound(); // Si no existe la factura de servicio público retorna error
            }

            ViewBag.Servicios = await _facServPubService.ObtenerServiciosPublicos();

            return View(facServPublicDto);
        }

        // POST: /Servicios/EditarFacturaServicioPublico
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditarFacturaServicioPublico(FacServPublicDto facServPublicDto)
        {
            try
            {
                string usuarioIdString = HttpContext.Session.GetString("UsuarioId");

                if (string.IsNullOrEmpty(usuarioIdString) || !int.TryParse(usuarioIdString, out int idUsuarioSesion))
                {
                    // Si no hay sesión o el ID no es válido, redirigimos al Login de Acceso
                    return RedirectToAction("Login", "Acceso");
                }

                // Asignamos el ID convertido a entero
                facServPublicDto.IdUsuario = idUsuarioSesion;

                // Validar el modelo
                if (!ModelState.IsValid)
                {
                    ViewBag.Servicios = await _facServPubService.ObtenerServiciosPublicos();
                    // Si el modelo no es válido, devolver la vista con los errores
                    var errors = ModelState.Values.SelectMany(v => v.Errors);
                    ViewData["Message"] = string.Join("; ", errors.Select(e => e.ErrorMessage));
                    return View(facServPublicDto);
                }

                var resultado = await _facServPubService.EditarFacturaServicioPublico(facServPublicDto);
                TempData["MensajeExito"] = resultado.Message;
                return RedirectToAction(nameof(FacturasServiciosPublicos));
            }
            catch (Exception ex)
            {
                ViewBag.Servicios = await _facServPubService.ObtenerServiciosPublicos();
                // Manejar cualquier excepción que ocurra durante la validación
                ViewData["Mensaje"] = "Error al editar factura de servicio público: " + ex.Message;
                return View(facServPublicDto);
            }
        }

        // GET: /Servicios/ConfirmarPagoFacturaServicioPublico
        [HttpGet]
        public async Task<ActionResult> ConfirmarPagoFacturaServicioPublico(int id)
        {
            var facServPublicDto = await _facServPubService.ObtenerFacturaServicioPublicoPorId(id);

            if (facServPublicDto == null)
            {
                return NotFound(); // Si no existe la factura de servicio público retorna error
            }

            return View(facServPublicDto);
        }

        // POST: /Servicios/ConfirmarPagoFacturaServicioPublico
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ConfirmarPagoFacturaServicioPublico(FacServPublicDto facServPublicDto)
        {
            try
            {
                var resultado = await _facServPubService.ConfirmarPagoFacturaServicioPublico(facServPublicDto.IdFactura);
                TempData["MensajeExito"] = resultado.Message;
                return RedirectToAction(nameof(FacturasServiciosPublicos));
            }
            catch (Exception ex)
            {
                // Manejar cualquier excepción que ocurra durante la validación
                ViewData["Mensaje"] = "Error al confirmar pago de factura de servicio público: " + ex.Message;
                return View(facServPublicDto);
            }
        }

        // GET: /factura/servicios-publicos/vencimiento-proximos (CU-24)
        [HttpGet("factura/servicios-publicos/vencimiento-proximos")]
        public async Task<ActionResult> VencimientoProximos()
        {
            try
            {
                var facturasProximas = await _facServPubService.ObtenerVencimientosProximos();
                return View(facturasProximas);
            }
            catch (Exception ex)
            {
                // Manejo de excepción para cumplir con FE02-CU25 (Error de carga de datos)
                ViewData["Mensaje"] = "No fue posible cargar la lista de vencimientos próximos. Intente nuevamente más tarde.";
                return View(new List<FacServPublicDto>());
            }
        }
    }
}