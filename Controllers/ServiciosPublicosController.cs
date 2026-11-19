using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Software_SARA.Dtos;
using Software_SARA.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Software_SARA.Controllers
{
    public class ServiciosPublicosController : Controller
    {
        private readonly FacServPubService _facServPubService;

        public ServiciosPublicosController(FacServPubService facServPubService)
        {
            _facServPubService = facServPubService;
        }

        private void ValidarDatosFacturaServicio(FacServPublicDto dto)
        {
            if (!dto.IdServicio.HasValue || dto.IdServicio.Value <= 0)
            {
                ModelState.AddModelError("IdServicio", "Debe seleccionar un servicio público.");
            }

            if (dto.ValorFactura <= 10000m)
            {
                ModelState.AddModelError("ValorFactura", "El valor de la factura debe ser mayor a $10.000.");
            }

            if (dto.FechaVencimiento < dto.FechaRegFactura)
            {
                ModelState.AddModelError("FechaVencimiento", "La fecha de vencimiento no puede ser inferior a la fecha de registro.");
            }
        }

        [HttpGet]
        public async Task<ActionResult> FacturasServiciosPublicos()
        {
            try
            {
                var facturas = await _facServPubService.ObtenerTodasLasFacturasServiciosPublicos()
                    ?? new List<FacServPublicDto>();
                return View(facturas);
            }
            catch (Exception ex)
            {
                ViewData["Mensaje"] = "Error al cargar las facturas de servicios públicos: " + ex.Message;
                return View(new List<FacServPublicDto>());
            }
        }

        [HttpGet]
        public async Task<ActionResult> CrearFacturaServicioPublico()
        {
            var hoy = DateOnly.FromDateTime(DateTime.Now);
            FacServPublicDto facServPublicDto = new()
            {
                FechaRegFactura = hoy,
                FechaVencimiento = hoy
            };
            ViewBag.Servicios = await _facServPubService.ObtenerServiciosPublicos();
            return View(facServPublicDto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CrearFacturaServicioPublico(FacServPublicDto facServPublicDto)
        {
            try
            {
                string? usuarioIdString = HttpContext.Session.GetString("UsuarioId");

                if (string.IsNullOrEmpty(usuarioIdString) || !int.TryParse(usuarioIdString, out int idUsuarioSesion))
                {
                    return RedirectToAction("Login", "Acceso");
                }

                facServPublicDto.IdUsuario = idUsuarioSesion;
                facServPublicDto.FechaRegFactura = DateOnly.FromDateTime(DateTime.Now);

                ValidarDatosFacturaServicio(facServPublicDto);

                if (!ModelState.IsValid)
                {
                    ViewBag.Servicios = await _facServPubService.ObtenerServiciosPublicos();
                    return View(facServPublicDto);
                }

                var resultado = await _facServPubService.CreateFacturaServicioPublico(facServPublicDto);

                if (resultado.Reponse == 0)
                {
                    ViewBag.Servicios = await _facServPubService.ObtenerServiciosPublicos();

                    string mensajeError = resultado.Message ?? "No fue posible registrar la factura.";

                    if (mensajeError.Contains("valor", StringComparison.OrdinalIgnoreCase) || mensajeError.Contains("10", StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError("ValorFactura", mensajeError);
                    }
                    else if (mensajeError.Contains("fecha de registro", StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError("FechaRegFactura", mensajeError);
                    }
                    else if (mensajeError.Contains("fecha de vencimiento", StringComparison.OrdinalIgnoreCase) || mensajeError.Contains("inferior", StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError("FechaVencimiento", "La fecha de vencimiento no puede ser inferior a la fecha de registro.");
                    }
                    else if (mensajeError.Contains("servicio", StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError("IdServicio", mensajeError);
                    }
                    else
                    {
                        ViewData["Mensaje"] = mensajeError;
                    }

                    return View(facServPublicDto);
                }

                TempData["MensajeExito"] = resultado.Message;
                return RedirectToAction(nameof(FacturasServiciosPublicos));
            }
            catch (Exception)
            {
                ViewBag.Servicios = await _facServPubService.ObtenerServiciosPublicos();
                ViewData["Mensaje"] = "Error al crear factura de servicio público. Intente nuevamente.";
                return View(facServPublicDto);
            }
        }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditarFacturaServicioPublico(FacServPublicDto facServPublicDto)
        {
            try
            {
                string? usuarioIdString = HttpContext.Session.GetString("UsuarioId");

                if (string.IsNullOrEmpty(usuarioIdString) || !int.TryParse(usuarioIdString, out int idUsuarioSesion))
                {
                    return RedirectToAction("Login", "Acceso");
                }

                facServPublicDto.IdUsuario = idUsuarioSesion;
                ValidarDatosFacturaServicio(facServPublicDto);

                if (!ModelState.IsValid)
                {
                    ViewBag.Servicios = await _facServPubService.ObtenerServiciosPublicos();
                    return View(facServPublicDto);
                }

                var resultado = await _facServPubService.EditarFacturaServicioPublico(facServPublicDto);

                if (resultado.Reponse == 0)
                {
                    ViewBag.Servicios = await _facServPubService.ObtenerServiciosPublicos();
                    ViewData["Mensaje"] = resultado.Message ?? "No fue posible actualizar la factura.";
                    return View(facServPublicDto);
                }

                TempData["MensajeExito"] = resultado.Message;
                return RedirectToAction(nameof(FacturasServiciosPublicos));
            }
            catch (Exception)
            {
                ViewBag.Servicios = await _facServPubService.ObtenerServiciosPublicos();
                ViewData["Mensaje"] = "Error al editar factura de servicio público. Intente nuevamente.";
                return View(facServPublicDto);
            }
        }

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
            catch (Exception)
            {
                ViewData["Mensaje"] = "Error al confirmar pago de factura de servicio público.";
                return View(facServPublicDto);
            }
        }

        [HttpGet("factura/servicios-publicos/vencimiento-proximos")]
        public async Task<ActionResult> VencimientoProximos()
        {
            try
            {
                var facturasProximas = await _facServPubService.ObtenerVencimientosProximos();
                return View(facturasProximas);
            }
            catch (Exception)
            {
                ViewData["Mensaje"] = "No fue posible cargar la lista de vencimientos próximos. Intente nuevamente más tarde.";
                return View(new List<FacServPublicDto>());
            }
        }
    }
}