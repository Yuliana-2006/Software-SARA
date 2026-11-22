using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Software_SARA.Dtos;
using Software_SARA.Services;
using Software_SARA.Utilities;

namespace Software_SARA.Controllers
{
    [AuthorizeRole("Gerente", "Tesorero", "Empleado")]
    public class RecordatoriosController : Controller
    {
        private readonly RecordatorioPedidoService _service;

        public RecordatoriosController(RecordatorioPedidoService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            string rol = HttpContext.Session.GetString("UsuarioRol") ?? string.Empty;

            // Si el usuario es Tesorero, lo enviamos directo a Entregas Próximas
            if (rol == "Tesorero")
            {
                return RedirectToAction(nameof(EntregasProximas));
            }

            var recordatorios = await _service.ObtenerTodosLosRecordatorios();
            var alertas = await _service.ObtenerEntregasProximasNotificaciones();
            ViewBag.CantidadNotificaciones = alertas.Count;

            return View(recordatorios);
        }

        [HttpGet]
        public async Task<IActionResult> Crear()
        {
            string rol = HttpContext.Session.GetString("UsuarioRol") ?? string.Empty;
            if (rol == "Tesorero")
            {
                TempData["MensajeError"] = "No tienes permisos para crear recordatorios.";
                return RedirectToAction(nameof(EntregasProximas));
            }

            ViewBag.Proveedores = _service.ObtenerListaProveedores();

            var dto = new RecordatorioPedidoDto
            {
                IdRecordatorio = await _service.ObtenerSiguienteId(),
                FechaEntrega = DateOnly.FromDateTime(DateTime.Today),
                Estado = "0"
            };

            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(RecordatorioPedidoDto dto)
        {
            string rol = HttpContext.Session.GetString("UsuarioRol") ?? string.Empty;
            if (rol == "Tesorero")
            {
                return RedirectToAction(nameof(EntregasProximas));
            }

            if (string.IsNullOrWhiteSpace(dto.IdRecordatorio))
            {
                dto.IdRecordatorio = await _service.ObtenerSiguienteId();
                ModelState.Remove(nameof(dto.IdRecordatorio));
            }

            if (string.IsNullOrWhiteSpace(dto.Estado))
            {
                dto.Estado = "0";
            }

            if (dto.FechaEntrega < DateOnly.FromDateTime(DateTime.Today))
            {
                ModelState.AddModelError("FechaEntrega", "La fecha de entrega debe ser igual o posterior al día actual.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Proveedores = _service.ObtenerListaProveedores();
                if (string.IsNullOrWhiteSpace(dto.IdRecordatorio))
                {
                    dto.IdRecordatorio = await _service.ObtenerSiguienteId();
                }
                return View(dto);
            }

            var resultado = await _service.CrearRecordatorio(dto);

            if (resultado.Response == 1)
            {
                TempData["MensajeExito"] = string.IsNullOrWhiteSpace(resultado.Message)
                    ? "Recordatorio de pedido registrado correctamente."
                    : resultado.Message;
                return RedirectToAction(nameof(Index));
            }
            else
            {
                ViewBag.Proveedores = _service.ObtenerListaProveedores();
                if (string.IsNullOrWhiteSpace(dto.IdRecordatorio))
                {
                    dto.IdRecordatorio = await _service.ObtenerSiguienteId();
                }
                ModelState.AddModelError(string.Empty, string.IsNullOrWhiteSpace(resultado.Message)
                    ? "No fue posible registrar el recordatorio. Verifica los datos e intenta nuevamente."
                    : resultado.Message);
                return View(dto);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Consultar(int id, string origen)
        {
            var recordatorio = await _service.ObtenerRecordatorioPorId(id);
            if (recordatorio == null)
            {
                TempData["MensajeError"] = "Error al cargar la información del recordatorio.";
                string rolActual = HttpContext.Session.GetString("UsuarioRol") ?? string.Empty;
                return RedirectToAction(rolActual == "Tesorero" ? nameof(EntregasProximas) : nameof(Index));
            }

            ViewBag.Proveedores = _service.ObtenerListaProveedores();
            ViewBag.Origen = origen;
            return View(recordatorio);
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            string rol = HttpContext.Session.GetString("UsuarioRol") ?? string.Empty;
            if (rol == "Tesorero")
            {
                TempData["MensajeError"] = "No tienes permisos para editar recordatorios.";
                return RedirectToAction(nameof(EntregasProximas));
            }

            var recordatorio = await _service.ObtenerRecordatorioPorId(id);
            if (recordatorio == null)
            {
                TempData["MensajeError"] = "El recordatorio de pedido no existe.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Proveedores = _service.ObtenerListaProveedores();
            return View(recordatorio);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(RecordatorioPedidoDto dto)
        {
            string rol = HttpContext.Session.GetString("UsuarioRol") ?? string.Empty;
            if (rol == "Tesorero")
            {
                return RedirectToAction(nameof(EntregasProximas));
            }

            if (dto.FechaEntrega < DateOnly.FromDateTime(DateTime.Today))
            {
                ModelState.AddModelError("FechaEntrega", "La fecha de entrega debe ser igual o posterior al día actual.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Proveedores = _service.ObtenerListaProveedores();
                return View(dto);
            }

            var resultado = await _service.ActualizarRecordatorio(dto);

            if (resultado.Response == 1)
            {
                TempData["MensajeExito"] = resultado.Message;
                return RedirectToAction(nameof(Index));
            }
            else
            {
                ViewBag.Proveedores = _service.ObtenerListaProveedores();
                ModelState.AddModelError(string.Empty, resultado.Message);
                return View(dto);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Confirmar(int id)
        {
            var recordatorio = await _service.ObtenerRecordatorioPorId(id);
            if (recordatorio == null)
            {
                TempData["MensajeError"] = "El recordatorio no existe o ya fue procesado.";
                string rolActual = HttpContext.Session.GetString("UsuarioRol") ?? string.Empty;
                return RedirectToAction(rolActual == "Tesorero" ? nameof(EntregasProximas) : nameof(Index));
            }

            return View(recordatorio);
        }

        [HttpPost]
        [ActionName("Confirmar")]
        public async Task<IActionResult> ConfirmarPost(int id)
        {
            bool confirmado = await _service.ConfirmarEntrega(id);
            if (confirmado)
            {
                TempData["MensajeExito"] = "Entrega del pedido confirmada exitosamente.";
            }
            else
            {
                TempData["MensajeError"] = "No fue posible confirmar la entrega del pedido en este momento.";
            }

            string rolActual = HttpContext.Session.GetString("UsuarioRol") ?? string.Empty;
            return RedirectToAction(rolActual == "Tesorero" ? nameof(EntregasProximas) : nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> EntregasProximas()
        {
            var alertas = await _service.ObtenerEntregasProximasNotificaciones();
            return View(alertas);
        }
    }
}