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
    public class ProveedoresController : Controller
    {
        private readonly FacProveedorService _facProveedorServices;

        public ProveedoresController(FacProveedorService facProveedorServices)
        {
            _facProveedorServices = facProveedorServices;
        }

        private string FormatearErrorFactura(Exception ex, string accion)
        {
            var detalle = (ex.InnerException?.Message ?? ex.Message) ?? string.Empty;

            if (detalle.Contains("overflow", StringComparison.OrdinalIgnoreCase) ||
                detalle.Contains("arithmetic", StringComparison.OrdinalIgnoreCase) ||
                detalle.Contains("numeric", StringComparison.OrdinalIgnoreCase))
            {
                return $"El valor ingresado en la factura es demasiado grande para guardarse al {accion}. Ingrese un monto menor.";
            }

            return $"No se pudo {accion} la factura de proveedor. Intente nuevamente o revise los datos ingresados.";
        }

        private void LimpiarMensajesEnInglesYDuplicados(FacProveedorDto dto)
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

            if (ModelState.TryGetValue(nameof(dto.IdProveedor), out var entryProv) && entryProv.Errors.Count > 0)
            {
                bool errorEnlaceProv = entryProv.Errors.Any(e =>
                    e.ErrorMessage.Contains("invalid", StringComparison.OrdinalIgnoreCase) ||
                    e.ErrorMessage.Contains("value", StringComparison.OrdinalIgnoreCase) ||
                    e.ErrorMessage.Contains("convert", StringComparison.OrdinalIgnoreCase));

                if (errorEnlaceProv)
                {
                    entryProv.Errors.Clear();
                    entryProv.Errors.Add("Debe seleccionar un proveedor.");
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

        private void ValidarDatosFacturaProveedor(FacProveedorDto dto, bool validarFechaRegistro = true)
        {
            LimpiarMensajesEnInglesYDuplicados(dto);

            if (!dto.IdProveedor.HasValue || dto.IdProveedor.Value <= 0)
            {
                if (!ModelState.ContainsKey(nameof(dto.IdProveedor)) || ModelState[nameof(dto.IdProveedor)]!.Errors.Count == 0)
                    ModelState.AddModelError(nameof(dto.IdProveedor), "Debe seleccionar un proveedor.");
            }

            if (!ModelState.ContainsKey(nameof(dto.ValorFactura)) || ModelState[nameof(dto.ValorFactura)]!.Errors.Count == 0)
            {
                if (dto.ValorFactura <= 10000m)
                {
                    ModelState.AddModelError(nameof(dto.ValorFactura), "El valor de la factura debe ser mayor a $10.000.");
                }
                else if (dto.ValorFactura > 999999999999.99m)
                {
                    ModelState.AddModelError(nameof(dto.ValorFactura), "El valor de la factura es demasiado grande.");
                }
            }

            if (validarFechaRegistro && dto.FechaRegFactura != default && dto.FechaVencimiento != default && dto.FechaVencimiento < dto.FechaRegFactura)
            {
                if (!ModelState.ContainsKey(nameof(dto.FechaVencimiento)) || ModelState[nameof(dto.FechaVencimiento)]!.Errors.Count == 0)
                    ModelState.AddModelError(nameof(dto.FechaVencimiento), "La fecha de vencimiento no puede ser anterior a la fecha de registro.");
            }

            LimpiarMensajesEnInglesYDuplicados(dto);
        }

        public async Task<ActionResult> FacturasProveedores()
        {
            var facturas = await _facProveedorServices.ObtenerTodasLasFacturasProveedores();
            return View(facturas);
        }

        [HttpGet]
        public async Task<ActionResult> CrearFacturaProveedor()
        {
            var hoy = DateOnly.FromDateTime(DateTime.Now);
            FacProveedorDto facProveedorDto = new()
            {
                FechaRegFactura = hoy,
                FechaVencimiento = hoy
            };
            ViewBag.Proveedores = await _facProveedorServices.ObtenerProveedores();
            return View(facProveedorDto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CrearFacturaProveedor(FacProveedorDto facProveedorDto)
        {
            try
            {
                string? usuarioIdString = HttpContext.Session.GetString("UsuarioId");

                if (string.IsNullOrEmpty(usuarioIdString) || !int.TryParse(usuarioIdString, out int idUsuarioSesion))
                {
                    return RedirectToAction("Login", "Acceso");
                }

                facProveedorDto.IdUsuario = idUsuarioSesion;
                // Fecha de registro automática a la fecha actual
                facProveedorDto.FechaRegFactura = DateOnly.FromDateTime(DateTime.Now);

                ValidarDatosFacturaProveedor(facProveedorDto);

                if (!ModelState.IsValid)
                {
                    ViewBag.Proveedores = await _facProveedorServices.ObtenerProveedores();
                    return View(facProveedorDto);
                }

                var resultado = await _facProveedorServices.CreateFacturaProveedor(facProveedorDto);
                if (resultado.Reponse == 0)
                {
                    ViewBag.Proveedores = await _facProveedorServices.ObtenerProveedores();
                    ViewData["Mensaje"] = resultado.Message;
                    return View(facProveedorDto);
                }

                TempData["MensajeExito"] = resultado.Message;
                return RedirectToAction(nameof(FacturasProveedores));
            }
            catch (Exception ex)
            {
                ViewBag.Proveedores = await _facProveedorServices.ObtenerProveedores();
                ViewData["Mensaje"] = FormatearErrorFactura(ex, "crear");
                return View(facProveedorDto);
            }
        }

        [HttpGet]
        public async Task<ActionResult> EditarFacturaProveedor(int id)
        {
            var facProveedorDto = await _facProveedorServices.ObtenerFacturaProveedorPorId(id);

            if (facProveedorDto == null)
            {
                return NotFound();
            }

            ViewBag.Proveedores = await _facProveedorServices.ObtenerProveedores();

            return View(facProveedorDto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditarFacturaProveedor(FacProveedorDto facProveedorDto)
        {
            try
            {
                string? usuarioIdString = HttpContext.Session.GetString("UsuarioId");

                if (string.IsNullOrEmpty(usuarioIdString) || !int.TryParse(usuarioIdString, out int idUsuarioSesion))
                {
                    return RedirectToAction("Login", "Acceso");
                }

                facProveedorDto.IdUsuario = idUsuarioSesion;

                ValidarDatosFacturaProveedor(facProveedorDto, validarFechaRegistro: false);

                if (!ModelState.IsValid)
                {
                    ViewBag.Proveedores = await _facProveedorServices.ObtenerProveedores();
                    return View(facProveedorDto);
                }

                var resultado = await _facProveedorServices.EditarFacturaProveedor(facProveedorDto);
                if (resultado.Reponse == 0)
                {
                    ViewBag.Proveedores = await _facProveedorServices.ObtenerProveedores();
                    ViewData["Mensaje"] = resultado.Message;
                    return View(facProveedorDto);
                }

                TempData["MensajeExito"] = resultado.Message;
                return RedirectToAction(nameof(FacturasProveedores));
            }
            catch (Exception ex)
            {
                ViewBag.Proveedores = await _facProveedorServices.ObtenerProveedores();
                ViewData["Mensaje"] = FormatearErrorFactura(ex, "editar");
                return View(facProveedorDto);
            }
        }

        [HttpGet]
        public async Task<ActionResult> ConfirmarPagoFacturaProveedor(int id)
        {
            var facProveedorDto = await _facProveedorServices.ObtenerFacturaProveedorPorId(id);

            if (facProveedorDto == null)
            {
                return NotFound();
            }

            return View(facProveedorDto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ConfirmarPagoFacturaProveedor(FacProveedorDto facProveedorDto)
        {
            try
            {
                var resultado = await _facProveedorServices.ConfirmarPagoFacturaProveedor(facProveedorDto.IdFactura);
                TempData["MensajeExito"] = resultado.Message;
                return RedirectToAction(nameof(FacturasProveedores));
            }
            catch (Exception ex)
            {
                ViewData["Mensaje"] = "Error al confirmar pago de factura de proveedor: " + ex.Message;
                return View(facProveedorDto);
            }
        }

        [HttpGet]
        public async Task<ActionResult> VencimientosProximosProveedores()
        {
            try
            {
                var vencimientos = await _facProveedorServices.ObtenerVencimientosProximosProveedores();
                return View(vencimientos);
            }
            catch (Exception ex)
            {
                ViewData["Mensaje"] = "Error al cargar los vencimientos próximos de proveedores: " + ex.Message;
                return View(new List<FacProveedorDto>());
            }
        }
    }
}