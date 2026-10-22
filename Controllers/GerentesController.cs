using Microsoft.AspNetCore.Mvc;
using Software_SARA.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Software_SARA.Controllers
{
    // ViewModel necesario para que las vistas y el controlador compartan los datos del Gerente
    public class GerenteViewModel
    {
        public int Id { get; set; }
        public string? Nombres { get; set; }
        public string? Apellidos { get; set; }
        public string? TipoDocumento { get; set; }
        public string? NumeroDocumento { get; set; }
        public string? Correo { get; set; }
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
        public string? Sede { get; set; }
        public DateTime? FechaNacimiento { get; set; }
    }

    public class GerentesController : Controller
    {
        private readonly GerenteService _gerenteService;

        public GerentesController(GerenteService gerenteService)
        {
            _gerenteService = gerenteService;
        }

        // GET: /Gerentes
        public async Task<IActionResult> Index()
        {
            try
            {
                var listaGerentes = await _gerenteService.ObtenerGerentesAsync();
                return View(listaGerentes);
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error al conectar con la base de datos: " + ex.Message;
                return View(new List<GerenteViewModel>());
            }
        }

        // GET: /Gerentes/Crear
        [HttpGet]
        public IActionResult Crear()
        {
            var model = new GerenteViewModel
            {
                Sede = "Sede Principal",
                TipoDocumento = "1"
            };
            return View(model);
        }

        // POST: /Gerentes/Crear
        [HttpPost]
        public async Task<IActionResult> Crear(GerenteViewModel gerente)
        {
            try
            {
                string? error = await _gerenteService.ValidarYCrearGerenteAsync(gerente);
                if (error != null)
                {
                    TempData["MensajeError"] = error;
                    return RedirectToAction("Index");
                }

                TempData["MensajeExito"] = "¡Gerente registrado exitosamente!";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Ocurrió un error al intentar guardar el gerente: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // GET: /Gerentes/Editar/5
        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            try
            {
                var gerente = await _gerenteService.ObtenerGerentePorIdAsync(id);
                if (gerente == null)
                {
                    return RedirectToAction("Index");
                }
                return View(gerente);
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error al consultar el gerente: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // POST: /Gerentes/Editar/5
        [HttpPost]
        public async Task<IActionResult> Editar(GerenteViewModel gerente)
        {
            try
            {
                string? error = await _gerenteService.ValidarYActualizarGerenteAsync(gerente);
                if (error != null)
                {
                    TempData["MensajeError"] = error;
                    return RedirectToAction("Index");
                }

                TempData["MensajeExito"] = "¡Gerente actualizado exitosamente!";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Ocurrió un error al actualizar los datos: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // GET: /Gerentes/Eliminar/5
        [HttpGet]
        public async Task<IActionResult> Eliminar(int id)
        {
            try
            {
                var gerente = await _gerenteService.ObtenerGerentePorIdAsync(id);
                if (gerente == null)
                {
                    return RedirectToAction("Index");
                }
                return View(gerente);
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error al intentar consultar el registro a eliminar: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // POST: /Gerentes/Eliminar
        [HttpPost, ActionName("Eliminar")]
        public async Task<IActionResult> EliminarConfirmado(int id)
        {
            try
            {
                await _gerenteService.EliminarGerenteAsync(id);
                TempData["MensajeExito"] = "¡Gerente eliminado exitosamente!";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "No se pudo eliminar el gerente: " + ex.Message;
            }

            return RedirectToAction("Index");
        }
    }
}