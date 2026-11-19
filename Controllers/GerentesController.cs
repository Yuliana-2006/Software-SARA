using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Software_SARA.Dtos;
using Software_SARA.Repositories.Models;
using Software_SARA.Services;
using Software_SARA.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Software_SARA.Controllers
{
    [AuthorizeRole("Dueño")]
    public class GerentesController : Controller
    {
        private readonly GerenteService _gerenteService;
        private readonly EmailService _emailService;
        private readonly SarabdContext _context;

        public GerentesController(GerenteService gerenteService, EmailService emailService, SarabdContext context)
        {
            _gerenteService = gerenteService;
            _emailService = emailService;
            _context = context;
        }

        private void CargarSedes(int? sedeSeleccionada = null)
        {
            ViewBag.ListaSedes = new SelectList(
                _context.Sedes.Where(s => s.EstadoSede == true),
                "IdSede",
                "NomSede",
                sedeSeleccionada
            );
        }

        private bool EsMayorDeEdad(DateTime? fechaNacimiento)
        {
            if (!fechaNacimiento.HasValue)
                return false;

            var hoy = DateTime.Today;
            var nacimiento = fechaNacimiento.Value.Date;

            if (nacimiento > hoy)
                return false;

            int edad = hoy.Year - nacimiento.Year;
            if (nacimiento > hoy.AddYears(-edad))
                edad--;

            return edad >= 18;
        }

        private void ValidarDatosGerente(GerenteDto gerente, int? idIgnorar = null)
        {
            if (gerente.IdSede <= 0)
            {
                ModelState.AddModelError("IdSede", "Debe seleccionar una sede válida.");
            }

            if (string.IsNullOrWhiteSpace(gerente.NumeroDocumento))
            {
                ModelState.AddModelError("NumeroDocumento", "El número de documento es obligatorio.");
            }
            else if (!Regex.IsMatch(gerente.NumeroDocumento, @"^\d+$"))
            {
                ModelState.AddModelError("NumeroDocumento", "El número de documento solo debe contener números (sin letras ni caracteres).");
            }
            else if (!Regex.IsMatch(gerente.NumeroDocumento, @"^\d{5,10}$"))
            {
                ModelState.AddModelError("NumeroDocumento", "El número de documento debe tener entre 5 y 10 dígitos.");
            }

            if (string.IsNullOrWhiteSpace(gerente.Telefono))
            {
                ModelState.AddModelError("Telefono", "El teléfono es obligatorio.");
            }
            else if (!Regex.IsMatch(gerente.Telefono, @"^\d+$"))
            {
                ModelState.AddModelError("Telefono", "El teléfono solo debe contener números (sin letras ni caracteres).");
            }
            else if (!Regex.IsMatch(gerente.Telefono, @"^\d{10}$"))
            {
                ModelState.AddModelError("Telefono", "El teléfono debe tener exactamente 10 números.");
            }

            if (string.IsNullOrWhiteSpace(gerente.Correo) ||
                !Regex.IsMatch(gerente.Correo, @"^[a-zA-Z0-9._%+-]+@(gmail\.com|outlook\.com|ucundinamarca\.edu\.co)$"))
            {
                ModelState.AddModelError("Correo", "El correo debe ser valido");
            }

            if (!gerente.FechaNacimiento.HasValue)
            {
                ModelState.AddModelError("FechaNacimiento", "La fecha de nacimiento es obligatoria.");
            }
            else if (!EsMayorDeEdad(gerente.FechaNacimiento))
            {
                ModelState.AddModelError("FechaNacimiento", "El gerente debe ser mayor de 18 años.");
            }

            if (!string.IsNullOrWhiteSpace(gerente.NumeroDocumento) && int.TryParse(gerente.NumeroDocumento, out int numDocParsed))
            {
                bool existeDoc = idIgnorar.HasValue
                    ? _context.Usuarios.Any(u => u.NumDoc == numDocParsed && u.IdUsuario != idIgnorar.Value)
                    : _context.Usuarios.Any(u => u.NumDoc == numDocParsed);

                if (existeDoc)
                {
                    ModelState.AddModelError("NumeroDocumento", idIgnorar.HasValue
                        ? "El documento ya pertenece a otro usuario."
                        : $"El documento {gerente.NumeroDocumento} ya existe en SARA.");
                }
            }

            if (!string.IsNullOrWhiteSpace(gerente.Correo))
            {
                bool existeCorreo = idIgnorar.HasValue
                    ? _context.Usuarios.Any(u => u.Correo == gerente.Correo && u.IdUsuario != idIgnorar.Value)
                    : _context.Usuarios.Any(u => u.Correo == gerente.Correo);

                if (existeCorreo)
                {
                    ModelState.AddModelError("Correo", idIgnorar.HasValue
                        ? "El correo ya pertenece a otro usuario."
                        : $"El correo {gerente.Correo} ya está registrado.");
                }
            }

            if (!string.IsNullOrWhiteSpace(gerente.Telefono))
            {
                bool existeTel = idIgnorar.HasValue
                    ? _context.Usuarios.Any(u => u.Telefono == gerente.Telefono && u.IdUsuario != idIgnorar.Value)
                    : _context.Usuarios.Any(u => u.Telefono == gerente.Telefono);

                if (existeTel)
                {
                    ModelState.AddModelError("Telefono", idIgnorar.HasValue
                        ? "El teléfono ya pertenece a otro usuario."
                        : "El teléfono ya está registrado.");
                }
            }
        }

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
                return View(new List<GerenteDto>());
            }
        }

        [HttpGet]
        public IActionResult Crear()
        {
            CargarSedes();

            var model = new GerenteDto
            {
                TipoDocumento = "CC"
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(GerenteDto gerente)
        {
            ValidarDatosGerente(gerente);

            if (!ModelState.IsValid)
            {
                CargarSedes(gerente.IdSede);
                return View(gerente);
            }

            try
            {
                string? error = await _gerenteService.ValidarYCrearGerenteAsync(gerente);

                if (error != null)
                {
                    ModelState.AddModelError("", error);
                    CargarSedes(gerente.IdSede);
                    return View(gerente);
                }

                TempData["MensajeExito"] = "¡Gerente registrado exitosamente y credenciales enviadas!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Ocurrió un error al intentar guardar: " + ex.Message);
                CargarSedes(gerente.IdSede);
                return View(gerente);
            }
        }

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

                CargarSedes(gerente.IdSede);
                return View(gerente);
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error al consultar el gerente: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(GerenteDto gerente)
        {
            ValidarDatosGerente(gerente, gerente.Id);

            if (!ModelState.IsValid)
            {
                CargarSedes(gerente.IdSede);
                return View(gerente);
            }

            try
            {
                string? error = await _gerenteService.ValidarYActualizarGerenteAsync(gerente);
                if (error != null)
                {
                    ModelState.AddModelError("", error);
                    CargarSedes(gerente.IdSede);
                    return View(gerente);
                }

                TempData["MensajeExito"] = "¡Gerente actualizado exitosamente!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Ocurrió un error al actualizar los datos: " + ex.Message);
                CargarSedes(gerente.IdSede);
                return View(gerente);
            }
        }

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

        [HttpPost, ActionName("Eliminar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarConfirmado(int id)
        {
            try
            {
                string? error = await _gerenteService.EliminarGerenteAsync(id);
                if (error != null)
                {
                    TempData["MensajeError"] = error;
                    return RedirectToAction("Index");
                }

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