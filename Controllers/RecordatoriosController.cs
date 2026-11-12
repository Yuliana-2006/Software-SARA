using Microsoft.AspNetCore.Mvc;
using Software_SARA.Dtos;
using Software_SARA.Services;

namespace Software_SARA.Controllers
{
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
            var recordatorios = await _service.ObtenerTodosLosRecordatorios();
            var alertas = await _service.ObtenerEntregasProximasNotificaciones();
            ViewBag.CantidadNotificaciones = alertas.Count;

            return View(recordatorios);
        }

        [HttpGet]
        public async Task<IActionResult> Crear()
        {
            ViewBag.Proveedores = _service.ObtenerListaProveedores();

            var dto = new RecordatorioPedidoDto
            {
                IdRecordatorio = await _service.ObtenerSiguienteId()
            };

            return View(dto);
        }

        [HttpPost]
        public async Task<IActionResult> Crear(RecordatorioPedidoDto dto)
        {
            if (dto.FechaEntrega < DateOnly.FromDateTime(DateTime.Today))
            {
                ModelState.AddModelError("FechaEntrega", "La fecha de entrega debe ser igual o posterior al día actual.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Proveedores = _service.ObtenerListaProveedores();
                dto.IdRecordatorio = await _service.ObtenerSiguienteId();
                return View(dto);
            }

            var resultado = await _service.CrearRecordatorio(dto);

            if (resultado.Response == 1)
            {
                TempData["MensajeExito"] = resultado.Message;
                return RedirectToAction(nameof(Index));
            }
            else
            {
                ViewBag.Proveedores = _service.ObtenerListaProveedores();
                dto.IdRecordatorio = await _service.ObtenerSiguienteId();
                ModelState.AddModelError(string.Empty, resultado.Message);
                return View(dto);
            }
        }

        // ==========================================
        // MÓDULO CONSULTAR (CU-27) - Actualizado con origen
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Consultar(int id, string origen)
        {
            var recordatorio = await _service.ObtenerRecordatorioPorId(id);
            if (recordatorio == null)
            {
                TempData["MensajeError"] = "Error al cargar la información del recordatorio.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Proveedores = _service.ObtenerListaProveedores();
            ViewBag.Origen = origen; // Guardamos el origen para el botón Volver
            return View(recordatorio);
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
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
        public async Task<IActionResult> Editar(RecordatorioPedidoDto dto)
        {
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
                return RedirectToAction(nameof(Index));
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

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> EntregasProximas()
        {
            var alertas = await _service.ObtenerEntregasProximasNotificaciones();
            return View(alertas);
        }
    }
}