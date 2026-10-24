using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
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
            return View(sedeDto);
        }


        // POST: /Sedes/CrearSede
        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<ActionResult> CrearSede(SedeDto sedeDto)
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

                var resultado = await _sedesServices.CreateSede(sedeDto);
                TempData["MensajeExito"] = resultado.Message;
                return RedirectToAction(nameof(RegistrarSede));
            }
            catch (Exception ex)
            {
                // Manejar cualquier excepción que ocurra durante la validación
                ViewData["Mensaje"] = "Error al crear sede: " + ex.Message;
                return View(sedeDto);
            }

        }

    }
}