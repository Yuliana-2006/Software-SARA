using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; // Importante para SelectList
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
        public int IdSede { get; set; }
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
            ViewBag.ListaSedes = new SelectList(_context.Sedes.Where(s => s.EstadoSede == true), "IdSede", "NomSede");

            var model = new GerenteViewModel
            {
                TipoDocumento = "CC"
            };
            return View(model);
        }

        // POST: /Gerentes/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(GerenteViewModel gerente)
        {
            // 1. Validar Sede obligatoria
            if (gerente.IdSede <= 0)
            {
                ModelState.AddModelError("IdSede", "Debe seleccionar una sede válida.");
            }

            // 2. Validar Documento: Detectar letras o formato incorrecto
            if (string.IsNullOrEmpty(gerente.NumeroDocumento))
            {
                ModelState.AddModelError("NumeroDocumento", "El número de documento es obligatorio.");
            }
            else if (!Regex.IsMatch(gerente.NumeroDocumento, @"^\d+$"))
            {
                ModelState.AddModelError("NumeroDocumento", "El número de documento solo debe contener números (sin letras).");
            }
            else if (!Regex.IsMatch(gerente.NumeroDocumento, @"^\d{8,10}$"))
            {
                ModelState.AddModelError("NumeroDocumento", "El número de documento debe tener entre 8 y 10 dígitos.");
            }

            // 3. Validar Teléfono: Detectar letras o formato incorrecto
            if (string.IsNullOrEmpty(gerente.Telefono))
            {
                ModelState.AddModelError("Telefono", "El teléfono es obligatorio.");
            }
            else if (!Regex.IsMatch(gerente.Telefono, @"^\d+$"))
            {
                ModelState.AddModelError("Telefono", "El teléfono solo debe contener números (sin letras).");
            }
            else if (!Regex.IsMatch(gerente.Telefono, @"^\d{10}$"))
            {
                ModelState.AddModelError("Telefono", "El teléfono celular debe contener exactamente 10 dígitos numéricos.");
            }

            // 4. Validar Correo
            if (string.IsNullOrEmpty(gerente.Correo) || !Regex.IsMatch(gerente.Correo, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                ModelState.AddModelError("Correo", "Ingrese un correo electrónico válido que contenga '@'.");
            }

            // 5. Validar duplicados en la Base de Datos
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
                ViewBag.ListaSedes = new SelectList(_context.Sedes.Where(s => s.EstadoSede == true), "IdSede", "NomSede", gerente.IdSede);
                return View(gerente);
            }

            try
            {
                string? error = await _gerenteService.ValidarYCrearGerenteAsync(gerente);

                if (error != null)
                {
                    ModelState.AddModelError("", error);
                    ViewBag.ListaSedes = new SelectList(_context.Sedes.Where(s => s.EstadoSede == true), "IdSede", "NomSede", gerente.IdSede);
                    return View(gerente);
                }

                TempData["MensajeExito"] = "¡Gerente registrado exitosamente y credenciales enviadas!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Ocurrió un error al intentar guardar: " + ex.Message);
                ViewBag.ListaSedes = new SelectList(_context.Sedes.Where(s => s.EstadoSede == true), "IdSede", "NomSede", gerente.IdSede);
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

                ViewBag.ListaSedes = new SelectList(_context.Sedes.Where(s => s.EstadoSede == true), "IdSede", "NomSede", gerente.IdSede);

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
            // 1. Validar Sede obligatoria
            if (gerente.IdSede <= 0)
            {
                ModelState.AddModelError("IdSede", "Debe seleccionar una sede válida.");
            }

            // 2. Validar Documento en Edición (Letras vs Números)
            if (string.IsNullOrEmpty(gerente.NumeroDocumento))
            {
                ModelState.AddModelError("NumeroDocumento", "El número de documento es obligatorio.");
            }
            else if (!Regex.IsMatch(gerente.NumeroDocumento, @"^\d+$"))
            {
                ModelState.AddModelError("NumeroDocumento", "El número de documento solo debe contener números (sin letras).");
            }
            else if (!Regex.IsMatch(gerente.NumeroDocumento, @"^\d{8,10}$"))
            {
                ModelState.AddModelError("NumeroDocumento", "El número de documento debe tener entre 8 y 10 dígitos.");
            }

            // 3. Validar Teléfono en Edición (Letras vs Números)
            if (string.IsNullOrEmpty(gerente.Telefono))
            {
                ModelState.AddModelError("Telefono", "El teléfono es obligatorio.");
            }
            else if (!Regex.IsMatch(gerente.Telefono, @"^\d+$"))
            {
                ModelState.AddModelError("Telefono", "El teléfono solo debe contener números (sin letras).");
            }
            else if (!Regex.IsMatch(gerente.Telefono, @"^\d{10}$"))
            {
                ModelState.AddModelError("Telefono", "El teléfono celular debe contener exactamente 10 dígitos numéricos.");
            }

            // 4. Validar Correo en Edición
            if (string.IsNullOrEmpty(gerente.Correo) || !Regex.IsMatch(gerente.Correo, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                ModelState.AddModelError("Correo", "Ingrese un correo electrónico válido que contenga '@'.");
            }

            // 5. Validar duplicados en Edición
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
                ViewBag.ListaSedes = new SelectList(_context.Sedes.Where(s => s.EstadoSede == true), "IdSede", "NomSede", gerente.IdSede);
                return View(gerente);
            }

            try
            {
                string? error = await _gerenteService.ValidarYActualizarGerenteAsync(gerente);
                if (error != null)
                {
                    ModelState.AddModelError("", error);
                    ViewBag.ListaSedes = new SelectList(_context.Sedes.Where(s => s.EstadoSede == true), "IdSede", "NomSede", gerente.IdSede);
                    return View(gerente);
                }

                TempData["MensajeExito"] = "¡Gerente actualizado exitosamente!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Ocurrió un error al actualizar los datos: " + ex.Message);
                ViewBag.ListaSedes = new SelectList(_context.Sedes.Where(s => s.EstadoSede == true), "IdSede", "NomSede", gerente.IdSede);
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