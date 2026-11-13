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
            FacServPublicDto facServPublicDto = new()
            {
                FechaRegFactura = DateOnly.FromDateTime(DateTime.Now),
                FechaVencimiento = DateOnly.FromDateTime(DateTime.Now)
            };
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
                    return RedirectToAction("Login", "Acceso");
                }

                facServPublicDto.IdUsuario = idUsuarioSesion;

                // VALIDACIÓN DIRECTA DE FECHAS EN EL CONTROLADOR
                if (facServPublicDto.FechaVencimiento < facServPublicDto.FechaRegFactura)
                {
                    ModelState.AddModelError("FechaVencimiento", "La fecha de vencimiento no puede ser inferior a la fecha de registro.");
                    ViewBag.Servicios = await _facServPubService.ObtenerServiciosPublicos();
                    return View(facServPublicDto);
                }

                if (!ModelState.IsValid)
                {
                    ViewBag.Servicios = await _facServPubService.ObtenerServiciosPublicos();
                    return View(facServPublicDto);
                }

                var resultado = await _facServPubService.CreateFacturaServicioPublico(facServPublicDto);

                if (resultado.Reponse == 0)
                {
                    ViewBag.Servicios = await _facServPubService.ObtenerServiciosPublicos();

                    if (resultado.Message.Contains("valor", StringComparison.OrdinalIgnoreCase) || resultado.Message.Contains("$20,000", StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError("ValorFactura", resultado.Message);
                    }
                    else if (resultado.Message.Contains("fecha de registro", StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError("FechaRegFactura", resultado.Message);
                    }
                    else if (resultado.Message.Contains("fecha de vencimiento", StringComparison.OrdinalIgnoreCase) || resultado.Message.Contains("inferior", StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError("FechaVencimiento", "La fecha de vencimiento no puede ser inferior a la fecha de registro.");
                    }
                    else if (resultado.Message.Contains("servicio", StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError("IdServicio", resultado.Message);
                    }
                    else
                    {
                        ViewData["Mensaje"] = resultado.Message;
                    }

                    return View(facServPublicDto);
                }

                TempData["MensajeExito"] = resultado.Message;
                return RedirectToAction(nameof(FacturasServiciosPublicos));
            }
            catch (Exception ex)
            {
                ViewBag.Servicios = await _facServPubService.ObtenerServiciosPublicos();
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
                return NotFound();
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
                    return RedirectToAction("Login", "Acceso");
                }

                facServPublicDto.IdUsuario = idUsuarioSesion;

                // VALIDACIÓN DIRECTA DE FECHAS EN EL CONTROLADOR
                if (facServPublicDto.FechaVencimiento < facServPublicDto.FechaRegFactura)
                {
                    ModelState.AddModelError("FechaVencimiento", "La fecha de vencimiento no puede ser inferior a la fecha de registro.");
                    ViewBag.Servicios = await _facServPubService.ObtenerServiciosPublicos();
                    return View(facServPublicDto);
                }

                if (!ModelState.IsValid)
                {
                    ViewBag.Servicios = await _facServPubService.ObtenerServiciosPublicos();
                    return View(facServPublicDto);
                }

                var resultado = await _facServPubService.EditarFacturaServicioPublico(facServPublicDto);

                if (resultado.Reponse == 0)
                {
                    ViewBag.Servicios = await _facServPubService.ObtenerServiciosPublicos();

                    if (resultado.Message.Contains("valor", StringComparison.OrdinalIgnoreCase) || resultado.Message.Contains("$20,000", StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError("ValorFactura", resultado.Message);
                    }
                    else if (resultado.Message.Contains("fecha de registro", StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError("FechaRegFactura", resultado.Message);
                    }
                    else if (resultado.Message.Contains("fecha de vencimiento", StringComparison.OrdinalIgnoreCase) || resultado.Message.Contains("inferior", StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError("FechaVencimiento", "La fecha de vencimiento no puede ser inferior a la fecha de registro.");
                    }
                    else if (resultado.Message.Contains("servicio", StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError("IdServicio", resultado.Message);
                    }
                    else
                    {
                        ViewData["Mensaje"] = resultado.Message;
                    }

                    return View(facServPublicDto);
                }

                TempData["MensajeExito"] = resultado.Message;
                return RedirectToAction(nameof(FacturasServiciosPublicos));
            }
            catch (Exception ex)
            {
                ViewBag.Servicios = await _facServPubService.ObtenerServiciosPublicos();
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
                return NotFound();
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
                ViewData["Mensaje"] = "No fue posible cargar la lista de vencimientos próximos. Intente nuevamente más tarde.";
                return View(new List<FacServPublicDto>());
            }
        }
    }
}