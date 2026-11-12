using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Software_SARA.Dtos;
using Software_SARA.Services;

namespace Software_SARA.Controllers
{
    public class ProveedoresController : Controller
    {
        private readonly FacProveedorService _facProveedorServices;

        public ProveedoresController(FacProveedorService facProveedorServices)
        {
            _facProveedorServices = facProveedorServices;
        }

        // GET: ProveedoresController
        public async Task<ActionResult> FacturasProveedores()
        {
            var facturas = await _facProveedorServices.ObtenerTodasLasFacturasProveedores();
            return View(facturas);
        }

        // GET: /Proveedores/CrearFacturaProveedor
        [HttpGet]
        public async Task<ActionResult> CrearFacturaProveedor()
        {
            FacProveedorDto facProveedorDto = new();
            ViewBag.Proveedores = await _facProveedorServices.ObtenerProveedores();
            return View(facProveedorDto);
        }


        // POST: /Proveedores/CrearFacturaProveedor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CrearFacturaProveedor(FacProveedorDto facProveedorDto)
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
                facProveedorDto.IdUsuario = idUsuarioSesion;

                // Validar el modelo
                if (!ModelState.IsValid)
                {
                    ViewBag.Proveedores = await _facProveedorServices.ObtenerProveedores();
                    // Si el modelo no es válido, devolver la vista con los errores
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
                // Manejar cualquier excepción que ocurra durante la validación
                ViewData["Mensaje"] = "Error al crear factura de proveedor: " + ex.Message;
                return View(facProveedorDto);
            }

        }

        // GET: /Proveedores/EditarFacturaProveedor
        [HttpGet]
        public async Task<ActionResult> EditarFacturaProveedor(int id)
        {
            var facProveedorDto = await _facProveedorServices.ObtenerFacturaProveedorPorId(id);

            if (facProveedorDto == null)
            {
                return NotFound(); // Si no existe la factura de proveedor retorna error
            }

            ViewBag.Proveedores = await _facProveedorServices.ObtenerProveedores();

            return View(facProveedorDto);
        }

        // POST: /Proveedores/EditarFacturaProveedor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditarFacturaProveedor(FacProveedorDto facProveedorDto)
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
                facProveedorDto.IdUsuario = idUsuarioSesion;

                // Validar el modelo
                if (!ModelState.IsValid)
                {
                    ViewBag.Proveedores = await _facProveedorServices.ObtenerProveedores();
                    // Si el modelo no es válido, devolver la vista con los errores
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
                // Manejar cualquier excepción que ocurra durante la validación
                ViewData["Mensaje"] = "Error al editar factura de proveedor: " + ex.Message;
                return View(facProveedorDto);
            }

        }

        // GET: /Proveedores/ConfirmarPagoFacturaProveedor
        [HttpGet]
        public async Task<ActionResult> ConfirmarPagoFacturaProveedor(int id)
        {
            var facProveedorDto = await _facProveedorServices.ObtenerFacturaProveedorPorId(id);

            if (facProveedorDto == null)
            {
                return NotFound(); // Si no existe la factura de proveedor retorna error
            }

            return View(facProveedorDto);
        }

        // POST: /Proveedores/ConfirmarPagoFacturaProveedor
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
                // Manejar cualquier excepción que ocurra durante la validación
                ViewData["Mensaje"] = "Error al confirmar pago de factura de proveedor: " + ex.Message;
                return View(facProveedorDto);
            }

        }
    }
}
