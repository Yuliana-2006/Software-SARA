using Microsoft.AspNetCore.Mvc;
using Software_SARA.Dtos;
using Software_SARA.Repositories.Models;
using Software_SARA.Services;
using Software_SARA.Utilities;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Software_SARA.Controllers
{
    public class DuenoController : Controller
    {
        private readonly SarabdContext _context;
        private readonly EmailService _emailService;

        public DuenoController(SarabdContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("UsuarioRol") != "Administrador") return RedirectToAction("Login", "Acceso");
            var duenos = _context.Usuarios.Where(u => u.IdRol == 1).ToList();
            return View(duenos);
        }

        [HttpGet]
        public IActionResult Crear()
        {
            if (HttpContext.Session.GetString("UsuarioRol") != "Administrador") return RedirectToAction("Login", "Acceso");
            return View(new DuenoDTO());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(DuenoDTO dto)
        {
            if (HttpContext.Session.GetString("UsuarioRol") != "Administrador") return RedirectToAction("Login", "Acceso");

            // 1. Validar que el documento y el teléfono SOLO contengan números (sin letras)
            if (!string.IsNullOrEmpty(dto.NumDoc) && !Regex.IsMatch(dto.NumDoc, @"^\d+$"))
            {
                ModelState.AddModelError("NumDoc", "El número de documento solo debe contener números (sin letras).");
            }

            if (!string.IsNullOrEmpty(dto.Telefono) && !Regex.IsMatch(dto.Telefono, @"^\d+$"))
            {
                ModelState.AddModelError("Telefono", "El teléfono solo debe contener números (sin letras).");
            }

            // 2. Validar edad (Mayor de 18)
            var hoy = DateTime.Today;
            var edad = hoy.Year - dto.FechaNaci.Year;
            if (dto.FechaNaci > DateOnly.FromDateTime(hoy.AddYears(-edad))) edad--;
            if (edad < 18)
            {
                ModelState.AddModelError("FechaNaci", "El Dueño debe ser mayor de 18 años.");
            }

            // 3. Validar duplicados en la Base de Datos (Documento, Correo y Teléfono)
            if (!string.IsNullOrEmpty(dto.NumDoc) && int.TryParse(dto.NumDoc, out int numDocParsed))
            {
                if (_context.Usuarios.Any(u => u.NumDoc == numDocParsed))
                    ModelState.AddModelError("NumDoc", $"El documento {dto.NumDoc} ya existe en SARA.");
            }

            if (_context.Usuarios.Any(u => u.Correo == dto.Correo))
                ModelState.AddModelError("Correo", $"El correo {dto.Correo} ya está registrado.");

            if (_context.Usuarios.Any(u => u.Telefono == dto.Telefono))
                ModelState.AddModelError("Telefono", $"El teléfono {dto.Telefono} ya está registrado.");

            if (!ModelState.IsValid) return View(dto);

            // Mapeo de DTO a Entidad
            var nuevoUsuario = new Usuario
            {
                TipDoc = dto.TipoDoc == "CC",
                NumDoc = int.Parse(dto.NumDoc),
                NomUsuario = dto.NomUsuario,
                ApelliUsuario = dto.ApelliUsuario,
                Correo = dto.Correo,
                Telefono = dto.Telefono,
                DirUsuario = dto.DirUsuario,
                FechaNaci = dto.FechaNaci,
                IdRol = 1, // Dueño
                IdSede = 1,
                EstadoUsuario = true
            };

            string passwordPlano = PasswordHelper.GenerarPassword(8);
            nuevoUsuario.Contraseña = PasswordHelper.Encriptar(passwordPlano);

            try
            {
                _context.Usuarios.Add(nuevoUsuario);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error en BD: " + (ex.InnerException?.Message ?? ex.Message));
                return View(dto);
            }

            try
            {
                // Envío de correo con los 5 parámetros requeridos
                await _emailService.EnviarPasswordAsync(
                    nuevoUsuario.Correo,
                    nuevoUsuario.NomUsuario,
                    passwordPlano,
                    "Dueño",
                    dto.NumDoc
                );
                TempData["MensajeExito"] = $"Dueño registrado y correo enviado a {nuevoUsuario.Correo}.";
            }
            catch (Exception ex)
            {
                TempData["MensajeExito"] = $"Dueño registrado. Falló el correo: {ex.Message}. Clave: {passwordPlano}";
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Consultar(int id)
        {
            var usuario = _context.Usuarios.Find(id);
            if (usuario == null) return NotFound();
            return View(usuario);
        }

        [HttpGet]
        public IActionResult Editar(int id)
        {
            var usuario = _context.Usuarios.Find(id);
            if (usuario == null) return NotFound();

            var dto = new DuenoDTO
            {
                TipoDoc = usuario.TipDoc ? "CC" : "CE",
                NumDoc = usuario.NumDoc.ToString(),
                NomUsuario = usuario.NomUsuario,
                ApelliUsuario = usuario.ApelliUsuario,
                Correo = usuario.Correo,
                Telefono = usuario.Telefono,
                DirUsuario = usuario.DirUsuario,
                FechaNaci = usuario.FechaNaci
            };
            ViewBag.IdUsuario = id;
            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Editar(int id, DuenoDTO dto)
        {
            var usuarioDb = _context.Usuarios.AsNoTracking().FirstOrDefault(u => u.IdUsuario == id);
            if (usuarioDb == null) return NotFound();

            // Validar que el documento y el teléfono SOLO contengan números en edición
            if (!string.IsNullOrEmpty(dto.NumDoc) && !Regex.IsMatch(dto.NumDoc, @"^\d+$"))
            {
                ModelState.AddModelError("NumDoc", "El número de documento solo debe contener números (sin letras).");
            }

            if (!string.IsNullOrEmpty(dto.Telefono) && !Regex.IsMatch(dto.Telefono, @"^\d+$"))
            {
                ModelState.AddModelError("Telefono", "El teléfono solo debe contener números (sin letras).");
            }

            // Validar duplicados (excluyendo el actual)
            if (!string.IsNullOrEmpty(dto.NumDoc) && int.TryParse(dto.NumDoc, out int numDocParsed))
            {
                if (_context.Usuarios.Any(u => u.NumDoc == numDocParsed && u.IdUsuario != id))
                    ModelState.AddModelError("NumDoc", "El documento ya pertenece a otro usuario.");
            }

            if (_context.Usuarios.Any(u => u.Correo == dto.Correo && u.IdUsuario != id))
                ModelState.AddModelError("Correo", "El correo ya pertenece a otro usuario.");

            if (_context.Usuarios.Any(u => u.Telefono == dto.Telefono && u.IdUsuario != id))
                ModelState.AddModelError("Telefono", "El teléfono ya pertenece a otro usuario.");

            // Validar edad
            var hoy = DateTime.Today;
            var edad = hoy.Year - dto.FechaNaci.Year;
            if (dto.FechaNaci > DateOnly.FromDateTime(hoy.AddYears(-edad))) edad--;
            if (edad < 18) ModelState.AddModelError("FechaNaci", "Debe ser mayor de 18 años.");

            if (!ModelState.IsValid)
            {
                ViewBag.IdUsuario = id;
                return View(dto);
            }

            // Actualizamos la entidad con los datos del DTO
            usuarioDb.TipDoc = dto.TipoDoc == "CC";
            usuarioDb.NumDoc = int.Parse(dto.NumDoc);
            usuarioDb.NomUsuario = dto.NomUsuario;
            usuarioDb.ApelliUsuario = dto.ApelliUsuario;
            usuarioDb.Correo = dto.Correo;
            usuarioDb.Telefono = dto.Telefono;
            usuarioDb.DirUsuario = dto.DirUsuario;
            usuarioDb.FechaNaci = dto.FechaNaci;

            try
            {
                _context.Usuarios.Update(usuarioDb);
                _context.SaveChanges();
                TempData["MensajeExito"] = "Datos del Dueño actualizados.";
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al actualizar: " + ex.Message);
                ViewBag.IdUsuario = id;
                return View(dto);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CambiarEstado(int id)
        {
            var usuario = _context.Usuarios.Find(id);
            if (usuario != null)
            {
                usuario.EstadoUsuario = !usuario.EstadoUsuario;
                _context.SaveChanges();
                TempData["MensajeExito"] = $"El usuario ahora está {(usuario.EstadoUsuario ? "ACTIVO" : "INACTIVO")}.";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Eliminar(int id)
        {
            var usuario = _context.Usuarios.Find(id);
            if (usuario != null)
            {
                if (usuario.EstadoUsuario == true)
                    TempData["MensajeError"] = "No se puede eliminar un usuario ACTIVO. Primero debe inactivarlo.";
                else
                {
                    _context.Usuarios.Remove(usuario);
                    _context.SaveChanges();
                    TempData["MensajeExito"] = "Usuario eliminado permanentemente.";
                }
            }
            return RedirectToAction("Index");
        }
    }
}