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

        // GET: /Sedes/CrearFacturaProveedor
        [HttpGet]
        public async Task<ActionResult> CrearFacturaProveedor()
        {
            FacProveedorDto facProveedorDto = new();
            ViewBag.Proveedores = await _facProveedorServices.ObtenerProveedores();
            return View(facProveedorDto);
        }


        // POST: /Sedes/CrearFacturaProveedor
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
    }
}
