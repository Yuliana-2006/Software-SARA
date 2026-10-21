using Microsoft.AspNetCore.Mvc;
using Software_SARA.Repositories.Models;
using Software_SARA.Services;
using Software_SARA.Utilities;
using Microsoft.EntityFrameworkCore;

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
            return View(new Usuario());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Usuario usuario, string tipoDocString)
        {
            if (HttpContext.Session.GetString("UsuarioRol") != "Administrador") return RedirectToAction("Login", "Acceso");

            // 👇 EL TRUCO PARA QUE NO PIDA CONTRASEÑA 👇
            ModelState.Remove("Contraseña");

            // Validaciones exactas
            if (usuario.NumDoc.ToString().Length < 8 || usuario.NumDoc.ToString().Length > 10)
                ModelState.AddModelError("NumDoc", "El documento debe tener entre 8 y 10 números.");

            if (usuario.Telefono == null || usuario.Telefono.Length != 10)
                ModelState.AddModelError("Telefono", "El teléfono debe tener exactamente 10 números.");

            var hoy = DateTime.Today;
            var edad = hoy.Year - usuario.FechaNaci.Year;
            if (usuario.FechaNaci > DateOnly.FromDateTime(hoy.AddYears(-edad))) edad--;
            if (edad < 18)
                ModelState.AddModelError("FechaNaci", "El Dueño debe ser mayor de 18 años.");

            // Validar Duplicados en Base de Datos
            if (_context.Usuarios.Any(u => u.NumDoc == usuario.NumDoc))
                ModelState.AddModelError("NumDoc", "Este documento ya existe en el sistema.");
            if (_context.Usuarios.Any(u => u.Correo == usuario.Correo))
                ModelState.AddModelError("Correo", "Este correo ya está registrado.");
            if (_context.Usuarios.Any(u => u.Telefono == usuario.Telefono))
                ModelState.AddModelError("Telefono", "Este teléfono ya está registrado.");

            if (!ModelState.IsValid) return View(usuario);

            // Guardar
            string passwordPlano = PasswordHelper.GenerarPassword(8);
            usuario.TipDoc = tipoDocString == "CC";
            usuario.IdRol = 1;
            usuario.IdSede = 1;
            usuario.Contraseña = PasswordHelper.Encriptar(passwordPlano);
            usuario.EstadoUsuario = true;

            try
            {
                _context.Usuarios.Add(usuario);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error en BD: " + (ex.InnerException?.Message ?? ex.Message));
                return View(usuario);
            }

            // Enviar Correo
            try
            {
                await _emailService.EnviarPasswordAsync(usuario.Correo, usuario.NomUsuario, passwordPlano);
                TempData["MensajeExito"] = $"Dueño {usuario.NomUsuario} registrado y correo enviado.";
            }
            catch (Exception ex)
            {
                TempData["MensajeExito"] = $"Registrado. Falló el correo. Clave: {passwordPlano}";
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
            return View(usuario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Editar(Usuario usuario, string tipoDocString)
        {
            var usuarioDb = _context.Usuarios.AsNoTracking().FirstOrDefault(u => u.IdUsuario == usuario.IdUsuario);
            if (usuarioDb == null) return NotFound();

            ModelState.Remove("Contraseña");

            // Validaciones (excluyendo al usuario actual)
            if (usuario.NumDoc.ToString().Length < 8 || usuario.NumDoc.ToString().Length > 10)
                ModelState.AddModelError("NumDoc", "El documento debe tener entre 8 y 10 números.");
            if (usuario.Telefono == null || usuario.Telefono.Length != 10)
                ModelState.AddModelError("Telefono", "El teléfono debe tener 10 números.");

            var hoy = DateTime.Today;
            var edad = hoy.Year - usuario.FechaNaci.Year;
            if (usuario.FechaNaci > DateOnly.FromDateTime(hoy.AddYears(-edad))) edad--;
            if (edad < 18) ModelState.AddModelError("FechaNaci", "Debe ser mayor de 18 años.");

            if (_context.Usuarios.Any(u => u.NumDoc == usuario.NumDoc && u.IdUsuario != usuario.IdUsuario))
                ModelState.AddModelError("NumDoc", "Este documento ya pertenece a otro usuario.");
            if (_context.Usuarios.Any(u => u.Correo == usuario.Correo && u.IdUsuario != usuario.IdUsuario))
                ModelState.AddModelError("Correo", "Este correo ya pertenece a otro usuario.");
            if (_context.Usuarios.Any(u => u.Telefono == usuario.Telefono && u.IdUsuario != usuario.IdUsuario))
                ModelState.AddModelError("Telefono", "Este teléfono ya pertenece a otro usuario.");

            if (!ModelState.IsValid) return View(usuario);

            usuario.TipDoc = tipoDocString == "CC";
            usuario.IdRol = 1;
            usuario.IdSede = usuarioDb.IdSede;
            usuario.Contraseña = usuarioDb.Contraseña;
            usuario.EstadoUsuario = usuarioDb.EstadoUsuario;

            try
            {
                _context.Usuarios.Update(usuario);
                _context.SaveChanges();
                TempData["MensajeExito"] = "Datos actualizados correctamente.";
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al actualizar: " + ex.Message);
                return View(usuario);
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
                TempData["MensajeExito"] = $"Usuario {(usuario.EstadoUsuario ? "Activado" : "Inactivado")} correctamente.";
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
                if (usuario.EstadoUsuario)
                    TempData["MensajeError"] = "Debe inactivar al usuario antes de eliminarlo.";
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