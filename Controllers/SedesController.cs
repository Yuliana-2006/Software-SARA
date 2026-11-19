using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Software_SARA.Dtos;
using Software_SARA.Services;
using Software_SARA.Utilities;

namespace Software_SARA.Controllers
{
    [AuthorizeRole("Dueño")]
    public class SedesController : Controller
    {
        private readonly SedesServices _sedesServices;

        public SedesController(SedesServices sedesServices)
        {
            _sedesServices = sedesServices;
        }

        private List<string> ObtenerDepartamentos()
        {
            return new List<string>
            {
                "Amazonas", "Antioquia", "Arauca", "Atlántico", "Bolívar",
                "Boyacá", "Caldas", "Caquetá", "Casanare", "Cauca",
                "Cesar", "Chocó", "Córdoba", "Cundinamarca", "Guainía",
                "Guaviare", "Huila", "La Guajira", "Magdalena", "Meta",
                "Nariño", "Norte de Santander", "Putumayo", "Quindío", "Risaralda",
                "San Andrés y Providencia", "Santander", "Sucre", "Tolima",
                "Valle del Cauca", "Vaupés", "Vichada"
            };
        }

        [HttpGet]
        public async Task<IActionResult> RegistrarSede()
        {
            var listaSedes = await _sedesServices.ObtenerTodasLasSedes();
            return View(listaSedes);
        }

        [HttpGet]
        public ActionResult CrearSede()
        {
            SedeDto sedeDto = new();
            ViewBag.Departamentos = new SelectList(ObtenerDepartamentos());
            return View(sedeDto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CrearSede(SedeDto sedeDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.Departamentos = new SelectList(ObtenerDepartamentos(), sedeDto.DepSede);
                    var errors = ModelState.Values.SelectMany(v => v.Errors);
                    ViewData["Message"] = string.Join("; ", errors.Select(e => e.ErrorMessage));
                    return View(sedeDto);
                }

                var resultado = await _sedesServices.CreateSede(sedeDto);
                if (resultado.Reponse == 1)
                    TempData["MensajeExito"] = resultado.Message;
                else
                    TempData["MensajeError"] = resultado.Message;

                return RedirectToAction(nameof(RegistrarSede));
            }
            catch (Exception ex)
            {
                ViewBag.Departamentos = new SelectList(ObtenerDepartamentos(), sedeDto.DepSede);
                ViewData["Mensaje"] = "Error al crear sede: " + ex.Message;
                return View(sedeDto);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditarSede(int id)
        {
            var sedeDto = await _sedesServices.ObtenerSedePorId(id);

            if (sedeDto == null)
            {
                return NotFound();
            }

            ViewBag.Departamentos = new SelectList(ObtenerDepartamentos(), sedeDto.DepSede);
            return View(sedeDto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditarSede(SedeDto sedeDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.Departamentos = new SelectList(ObtenerDepartamentos(), sedeDto.DepSede);
                    var errors = ModelState.Values.SelectMany(v => v.Errors);
                    ViewData["Message"] = string.Join("; ", errors.Select(e => e.ErrorMessage));
                    return View(sedeDto);
                }

                var resultado = await _sedesServices.EditarSede(sedeDto);
                if (resultado.Reponse == 1)
                    TempData["MensajeExito"] = resultado.Message;
                else
                    TempData["MensajeError"] = resultado.Message;

                return RedirectToAction(nameof(RegistrarSede));
            }
            catch (Exception ex)
            {
                ViewBag.Departamentos = new SelectList(ObtenerDepartamentos(), sedeDto.DepSede);
                ViewData["Mensaje"] = "Error al editar sede: " + ex.Message;
                return View(sedeDto);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EliminarSede(int id)
        {
            var sedeDto = await _sedesServices.ObtenerSedePorId(id);

            if (sedeDto == null)
            {
                return NotFound();
            }

            return View(sedeDto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EliminarSede(SedeDto sedeDto)
        {
            try
            {
                var resultado = await _sedesServices.EliminarSede(sedeDto.IdSede);
                if (resultado.Reponse == 1)
                {
                    TempData["MensajeExito"] = resultado.Message;
                    return RedirectToAction(nameof(RegistrarSede));
                }

                // Aquí se devuelve la vista EliminarSede con el bloqueo y el mensaje sin desactivar
                var sedeActual = await _sedesServices.ObtenerSedePorId(sedeDto.IdSede) ?? sedeDto;
                ViewData["Mensaje"] = resultado.Message;
                ViewBag.Bloqueado = true;
                return View(sedeActual);
            }
            catch (Exception ex)
            {
                ViewData["Mensaje"] = "Error al eliminar sede: " + ex.Message;
                ViewBag.Bloqueado = true;
                return View(sedeDto);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReactivarSede(int id)
        {
            try
            {
                var resultado = await _sedesServices.ReactivarSede(id);
                if (resultado.Reponse == 1)
                    TempData["MensajeExito"] = resultado.Message;
                else
                    TempData["MensajeError"] = resultado.Message;

                return RedirectToAction(nameof(RegistrarSede));
            }
            catch (Exception ex)
            {
                TempData["Mensaje"] = "Error al reactivar la sede: " + ex.Message;
                return RedirectToAction(nameof(RegistrarSede));
            }
        }
    }
}