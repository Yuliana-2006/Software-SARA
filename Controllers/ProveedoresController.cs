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

        private void ValidarDatosFacturaProveedor(FacProveedorDto dto, bool validarFechaRegistro = true)
        {
            if (!dto.IdProveedor.HasValue || dto.IdProveedor.Value <= 0)
            {
                ModelState.AddModelError("IdProveedor", "Debe seleccionar un proveedor.");
            }

            if (dto.ValorFactura <= 10000m)
            {
                ModelState.AddModelError("ValorFactura", "El valor de la factura debe ser mayor a $10.000.");
            }

            if (validarFechaRegistro && dto.FechaRegFactura != default && dto.FechaVencimiento != default && dto.FechaVencimiento < dto.FechaRegFactura)
            {
                ModelState.AddModelError("FechaVencimiento", "La fecha de vencimiento no puede ser anterior a la fecha de registro.");
            }
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
                    var errors = ModelState.Values.SelectMany(v => v.Errors);
                    ViewData["Message"] = string.Join("; ", errors.Select(e => e.ErrorMessage));
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
                ViewData["Mensaje"] = "Error al crear factura de proveedor: " + ex.Message;
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
                    var errors = ModelState.Values.SelectMany(v => v.Errors);
                    ViewData["Message"] = string.Join("; ", errors.Select(e => e.ErrorMessage));
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
                ViewData["Mensaje"] = "Error al editar factura de proveedor: " + ex.Message;
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