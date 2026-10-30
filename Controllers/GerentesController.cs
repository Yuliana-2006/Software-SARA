using Microsoft.AspNetCore.Mvc;
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
        private readonly EmailService _emailService;
        private readonly SarabdContext _context;

        public GerentesController(GerenteService gerenteService, EmailService emailService, SarabdContext context)
        {
            _gerenteService = gerenteService;
            _emailService = emailService;
            _context = context;
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
                return View(new List<GerenteViewModel>());
            }
        }

        [HttpGet]
        public IActionResult Crear()
        {
            var model = new GerenteViewModel
            {
                Sede = "Sede Principal",
                TipoDocumento = "CC"
            };
            return View(model);
        }

        // POST: /Gerentes/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(GerenteViewModel gerente)
        {
            // 1. Validar Documento: Solo números y longitud entre 8 y 10 dígitos
            if (string.IsNullOrEmpty(gerente.NumeroDocumento) || !Regex.IsMatch(gerente.NumeroDocumento, @"^\d{8,10}$"))
            {
                ModelState.AddModelError("NumeroDocumento", "El número de documento debe ser numérico y tener entre 8 y 10 dígitos.");
            }

            // 2. Validar Teléfono: Solo números y exactamente 10 dígitos
            if (string.IsNullOrEmpty(gerente.Telefono) || !Regex.IsMatch(gerente.Telefono, @"^\d{10}$"))
            {
                ModelState.AddModelError("Telefono", "El teléfono celular debe contener exactamente 10 dígitos numéricos.");
            }

            // 3. Validar Correo: Obligatorio con formato que incluya '@'
            if (string.IsNullOrEmpty(gerente.Correo) || !Regex.IsMatch(gerente.Correo, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                ModelState.AddModelError("Correo", "Ingrese un correo electrónico válido que contenga '@'.");
            }

            // 4. Validar duplicados en la Base de Datos
            if (!string.IsNullOrEmpty(gerente.NumeroDocumento) && int.TryParse(gerente.NumeroDocumento, out int numDocParsed))
            {
                if (_context.Usuarios.Any(u => u.NumDoc == numDocParsed))
                    ModelState.AddModelError("NumeroDocumento", $"El documento {gerente.NumeroDocumento} ya existe en SARA.");
            }

            if (!string.IsNullOrEmpty(gerente.Correo) && _context.Usuarios.Any(u => u.Correo == gerente.Correo))
            {
                ModelState.AddModelError("Correo", $"El correo {gerente.Correo} ya está registrado.");
            }

            if (!string.IsNullOrEmpty(gerente.Telefono) && _context.Usuarios.Any(u => u.Telefono == gerente.Telefono))
            {
                ModelState.AddModelError("Telefono", $"El teléfono ya está registrado.");
            }

            if (!ModelState.IsValid)
            {
                return View(gerente);
            }

            try
            {
                // 💡 El servicio se encarga de crear el usuario, hashear la contraseña aleatoria con BCrypt y enviar el correo
                string? error = await _gerenteService.ValidarYCrearGerenteAsync(gerente);

                if (error != null)
                {
                    ModelState.AddModelError("", error);
                    return View(gerente);
                }

                TempData["MensajeExito"] = "¡Gerente registrado exitosamente y credenciales enviadas!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Ocurrió un error al intentar guardar: " + ex.Message);
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(GerenteViewModel gerente)
        {
            // 1. Validar Documento en Edición (8 a 10 dígitos)
            if (string.IsNullOrEmpty(gerente.NumeroDocumento) || !Regex.IsMatch(gerente.NumeroDocumento, @"^\d{8,10}$"))
            {
                ModelState.AddModelError("NumeroDocumento", "El número de documento debe ser numérico y tener entre 8 y 10 dígitos.");
            }

            // 2. Validar Teléfono en Edición (10 dígitos)
            if (string.IsNullOrEmpty(gerente.Telefono) || !Regex.IsMatch(gerente.Telefono, @"^\d{10}$"))
            {
                ModelState.AddModelError("Telefono", "El teléfono celular debe contener exactamente 10 dígitos numéricos.");
            }

            // 3. Validar Correo en Edición (@)
            if (string.IsNullOrEmpty(gerente.Correo) || !Regex.IsMatch(gerente.Correo, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                ModelState.AddModelError("Correo", "Ingrese un correo electrónico válido que contenga '@'.");
            }

            // 4. Validar duplicados en Edición (excluyendo al propio usuario por ID)
            if (!string.IsNullOrEmpty(gerente.NumeroDocumento) && int.TryParse(gerente.NumeroDocumento, out int numDocParsed))
            {
                if (_context.Usuarios.Any(u => u.NumDoc == numDocParsed && u.IdUsuario != gerente.Id))
                    ModelState.AddModelError("NumeroDocumento", "El documento ya pertenece a otro usuario.");
            }

            if (!string.IsNullOrEmpty(gerente.Correo) && _context.Usuarios.Any(u => u.Correo == gerente.Correo && u.IdUsuario != gerente.Id))
            {
                ModelState.AddModelError("Correo", "El correo ya pertenece a otro usuario.");
            }

            if (!string.IsNullOrEmpty(gerente.Telefono) && _context.Usuarios.Any(u => u.Telefono == gerente.Telefono && u.IdUsuario != gerente.Id))
            {
                ModelState.AddModelError("Telefono", "El teléfono ya pertenece a otro usuario.");
            }

            if (!ModelState.IsValid)
            {
                return View(gerente);
            }

            try
            {
                string? error = await _gerenteService.ValidarYActualizarGerenteAsync(gerente);
                if (error != null)
                {
                    ModelState.AddModelError("", error);
                    return View(gerente);
                }

                TempData["MensajeExito"] = "¡Gerente actualizado exitosamente!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Ocurrió un error al actualizar los datos: " + ex.Message);
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