using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Software_SARA.Dtos;
using Software_SARA.Services;


namespace Software_SARA.Controllers
{
    public class SedesController : Controller
    {
        private readonly SedesServices _sedesServices;

        public SedesController(SedesServices sedesServices)
        {
            _sedesServices = sedesServices;
        }

        // GET: /Sedes/RegistrarSede
        [HttpGet]
        public async Task<IActionResult> RegistrarSede()
        {
            var listaSedes = await _sedesServices.ObtenerTodasLasSedes();
            return View(listaSedes);
        }


        // GET: /Sedes/CrearSede
        [HttpGet]
        public ActionResult CrearSede()
        {
            SedeDto sedeDto = new();
            List<string> departamentosColombia = new List<string>
            {
                "Amazonas", "Antioquia", "Arauca", "Atlántico", "Bolívar",
                "Boyacá", "Caldas", "Caquetá", "Casanare", "Cauca",
                "Cesar", "Chocó", "Córdoba", "Cundinamarca", "Guainía",
                "Guaviare", "Huila", "La Guajira", "Magdalena", "Meta",
                "Nariño", "Norte de Santander", "Putumayo", "Quindío", "Risaralda",
                "San Andrés y Providencia", "Santander", "Sucre", "Tolima",
                "Valle del Cauca", "Vaupés", "Vichada"
            };
            ViewBag.Departamentos = new SelectList(departamentosColombia);
            return View(sedeDto);
        }


        // POST: /Sedes/CrearSede
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CrearSede(SedeDto sedeDto)
        {
            // Lista de departamentos para reutilizar si se recarga la vista
            List<string> departamentosColombia = new List<string>
            {
                "Amazonas", "Antioquia", "Arauca", "Atlántico", "Bolívar",
                "Boyacá", "Caldas", "Caquetá", "Casanare", "Cauca",
                "Cesar", "Chocó", "Córdoba", "Cundinamarca", "Guainía",
                "Guaviare", "Huila", "La Guajira", "Magdalena", "Meta",
                "Nariño", "Norte de Santander", "Putumayo", "Quindío", "Risaralda",
                "San Andrés y Providencia", "Santander", "Sucre", "Tolima",
                "Valle del Cauca", "Vaupés", "Vichada"
            };

            try
            {
                // Validar el modelo
                if (!ModelState.IsValid)
                {
                    // Si el modelo no es válido, asignamos el ViewBag antes de retornar la vista
                    ViewBag.Departamentos = new SelectList(departamentosColombia);
                    // Si el modelo no es válido, devolver la vista con los errores
                    var errors = ModelState.Values.SelectMany(v => v.Errors);
                    ViewData["Message"] = string.Join("; ", errors.Select(e => e.ErrorMessage));
                    return View(sedeDto);
                }

                var resultado = await _sedesServices.CreateSede(sedeDto);
                if (resultado.Reponse == 0)
                {
                    ViewBag.Proveedores = await _sedesServices.ObtenerTodasLasSedes();
                    ViewData["Mensaje"] = resultado.Message;
                    return View(sedeDto);
                }
                TempData["MensajeExito"] = resultado.Message;
                return RedirectToAction(nameof(RegistrarSede));
            }
            catch (Exception ex)
            {
                // Si ocurre cualquier excepción, el ViewBag no sera nulo al recargar
                ViewBag.Departamentos = new SelectList(departamentosColombia);
                // Manejar cualquier excepción que ocurra durante la validación
                ViewData["Mensaje"] = "Error al crear sede: " + ex.Message;
                return View(sedeDto);
            }

        }

        // GET: /Sedes/EditarSede
        [HttpGet]
        public async Task<ActionResult> EditarSede(int id)
        {
            var sedeDto = await _sedesServices.ObtenerSedePorId(id);

            if (sedeDto == null)
            {
                return NotFound(); // Si no existe la sede retorna error
            }

            return View(sedeDto);
        }

        // POST: /Sedes/EditarSede
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditarSede(SedeDto sedeDto)
        {
            try
            {
                // Validar el modelo
                if (!ModelState.IsValid)
                {
                    // Si el modelo no es válido, devolver la vista con los errores
                    var errors = ModelState.Values.SelectMany(v => v.Errors);
                    ViewData["Message"] = string.Join("; ", errors.Select(e => e.ErrorMessage));
                    return View(sedeDto);
                }

                var resultado = await _sedesServices.EditarSede(sedeDto);
                if (resultado.Reponse == 0)
                {
                    ViewBag.Proveedores = await _sedesServices.ObtenerTodasLasSedes();
                    ViewData["Mensaje"] = resultado.Message;
                    return View(sedeDto);
                }
                TempData["MensajeExito"] = resultado.Message;
                return RedirectToAction(nameof(RegistrarSede));
            }
            catch (Exception ex)
            {
                // Manejar cualquier excepción que ocurra durante la validación
                ViewData["Mensaje"] = "Error al editar sede: " + ex.Message;
                return View(sedeDto);
            }

        }

        // GET: /Sedes/EliminarSede
        [HttpGet]
        public async Task<ActionResult> EliminarSede(int id)
        {
            var sedeDto = await _sedesServices.ObtenerSedePorId(id);

            if (sedeDto == null)
            {
                return NotFound(); // Si no existe la sede retorna error
            }

            return View(sedeDto);
        }

        // POST: /Sedes/EliminarSede
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EliminarSede(SedeDto sedeDto)
        {
            try
            {
                var resultado = await _sedesServices.EliminarSede(sedeDto.IdSede);
                TempData["MensajeExito"] = resultado.Message;
                return RedirectToAction(nameof(RegistrarSede));
            }
            catch (Exception ex)
            {
                // Manejar cualquier excepción que ocurra durante la validación
                ViewData["Mensaje"] = "Error al eliminar sede: " + ex.Message;
                return View(sedeDto);
            }

        }

        // POST: /Sedes/ReactivarSede/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ReactivarSede(int id)
        {
            try
            {
                var resultado = await _sedesServices.ReactivarSede(id);
                TempData["MensajeExito"] = resultado.Message;
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