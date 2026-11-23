using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Software_SARA.Dtos;
using Software_SARA.Services;
using Software_SARA.Utilities;

namespace Software_SARA.Controllers
{
    [AuthorizeRole("Tesorero", "Gerente")]
    public class ServiciosPublicosController : Controller
    {
        private readonly FacServPubService _facServPubService;

        public ServiciosPublicosController(FacServPubService facServPubService)
        {
            _facServPubService = facServPubService;
        }

        private string FormatearErrorServicio(Exception ex, string accion)
        {
            var detalle = (ex.InnerException?.Message ?? ex.Message) ?? string.Empty;

            if (detalle.Contains("overflow", StringComparison.OrdinalIgnoreCase) ||
                detalle.Contains("arithmetic", StringComparison.OrdinalIgnoreCase) ||
                detalle.Contains("numeric", StringComparison.OrdinalIgnoreCase))
            {
                return $"El valor ingresado en la factura es demasiado grande para guardarse al {accion}. Ingrese un monto menor.";
            }

            return $"Error al {accion} factura de servicio público. Intente nuevamente.";
        }

        private void LimpiarMensajesEnInglesYDuplicados(FacServPublicDto dto)
        {
            if (ModelState.TryGetValue(nameof(dto.ValorFactura), out var entryValor) && entryValor.Errors.Count > 0)
            {
                bool errorEnlaceValor = entryValor.Errors.Any(e =>
                    e.ErrorMessage.Contains("invalid", StringComparison.OrdinalIgnoreCase) ||
                    e.ErrorMessage.Contains("value", StringComparison.OrdinalIgnoreCase) ||
                    e.ErrorMessage.Contains("convert", StringComparison.OrdinalIgnoreCase));

                if (errorEnlaceValor)
                {
                    entryValor.Errors.Clear();
                    entryValor.Errors.Add("Ingrese un valor numérico válido mayor a $10.000.");
                }
            }

            if (ModelState.TryGetValue(nameof(dto.IdServicio), out var entryServ) && entryServ.Errors.Count > 0)
            {
                bool errorEnlaceServ = entryServ.Errors.Any(e =>
                    e.ErrorMessage.Contains("invalid", StringComparison.OrdinalIgnoreCase) ||
                    e.ErrorMessage.Contains("value", StringComparison.OrdinalIgnoreCase) ||
                    e.ErrorMessage.Contains("convert", StringComparison.OrdinalIgnoreCase));

                if (errorEnlaceServ)
                {
                    entryServ.Errors.Clear();
                    entryServ.Errors.Add("Debe seleccionar un servicio público.");
                }
            }

            foreach (var key in ModelState.Keys.ToList())
            {
                var state = ModelState[key];
                if (state == null || state.Errors.Count == 0) continue;

                var erroresLimpios = state.Errors
                    .Select(e =>
                    {
                        if (e.ErrorMessage.Contains("is invalid", StringComparison.OrdinalIgnoreCase) ||
                            e.ErrorMessage.Contains("The value", StringComparison.OrdinalIgnoreCase))
                        {
                            return "Revise el valor ingresado en el campo.";
                        }
                        return e.ErrorMessage.Trim();
                    })
                    .Where(m => !string.IsNullOrWhiteSpace(m))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                state.Errors.Clear();
                foreach (var msg in erroresLimpios)
                {
                    state.Errors.Add(msg);
                }
            }
        }

        private void ValidarDatosFacturaServicio(FacServPublicDto dto)
        {
            LimpiarMensajesEnInglesYDuplicados(dto);

            if (!dto.IdServicio.HasValue || dto.IdServicio.Value <= 0)
            {
                if (!ModelState.ContainsKey(nameof(dto.IdServicio)) || ModelState[nameof(dto.IdServicio)]!.Errors.Count == 0)
                    ModelState.AddModelError("IdServicio", "Debe seleccionar un servicio público.");
            }

            if (!ModelState.ContainsKey(nameof(dto.ValorFactura)) || ModelState[nameof(dto.ValorFactura)]!.Errors.Count == 0)
            {
                if (dto.ValorFactura <= 10000m)
                {
                    ModelState.AddModelError("ValorFactura", "El valor de la factura debe ser mayor a $10.000.");
                }
                else if (dto.ValorFactura > 999999999999.99m)
                {
                    ModelState.AddModelError("ValorFactura", "El valor de la factura es demasiado grande.");
                }
            }

            if (dto.FechaVencimiento < dto.FechaRegFactura)
            {
                if (!ModelState.ContainsKey(nameof(dto.FechaVencimiento)) || ModelState[nameof(dto.FechaVencimiento)]!.Errors.Count == 0)
                    ModelState.AddModelError("FechaVencimiento", "La fecha de vencimiento no puede ser inferior a la fecha de registro.");
            }

            LimpiarMensajesEnInglesYDuplicados(dto);
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

                    LimpiarMensajesEnInglesYDuplicados(facServPublicDto);
                    return View(facServPublicDto);
                }

                TempData["MensajeExito"] = resultado.Message;
                return RedirectToAction(nameof(FacturasServiciosPublicos));
            }
            catch (Exception ex)
            {
                ViewBag.Servicios = await _facServPubService.ObtenerServiciosPublicos();
                ViewData["Mensaje"] = FormatearErrorServicio(ex, "crear");
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
            catch (Exception ex)
            {
                ViewBag.Servicios = await _facServPubService.ObtenerServiciosPublicos();
                ViewData["Mensaje"] = FormatearErrorServicio(ex, "editar");
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