using Microsoft.AspNetCore.Mvc;
using Software_SARA.Dtos;
using Software_SARA.Repositories.Models;
using Software_SARA.Services;
using Software_SARA.Utilities;
using Microsoft.EntityFrameworkCore;

namespace Software_SARA.Controllers
{
    [AuthorizeRole("Administrador")] // <--- ATRIBUTO DE SEGURIDAD
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
            var duenos = _context.Usuarios.Where(u => u.IdRol == 1).ToList();
            return View(duenos);
        }

        [HttpGet]
        public IActionResult Crear()
        {
            return View(new DuenoDTO());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async System.Threading.Tasks.Task<IActionResult> Crear(DuenoDTO dto)
        {
            var hoy = System.DateTime.Today;
            var edad = hoy.Year - dto.FechaNaci.Year;
            if (dto.FechaNaci > System.DateOnly.FromDateTime(hoy.AddYears(-edad))) edad--;
            if (edad < 18) ModelState.AddModelError("FechaNaci", "El Dueño debe ser mayor de 18 años.");

            if (_context.Usuarios.Any(u => u.NumDoc.ToString() == dto.NumDoc)) ModelState.AddModelError("NumDoc", $"El documento {dto.NumDoc} ya existe.");
            if (_context.Usuarios.Any(u => u.Correo == dto.Correo)) ModelState.AddModelError("Correo", $"El correo {dto.Correo} ya está registrado.");
            if (_context.Usuarios.Any(u => u.Telefono == dto.Telefono)) ModelState.AddModelError("Telefono", $"El teléfono {dto.Telefono} ya está registrado.");

            if (!ModelState.IsValid) return View(dto);

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
                IdRol = 1,
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
            catch (System.Exception ex)
            {
                ModelState.AddModelError("", "Error en BD: " + (ex.InnerException?.Message ?? ex.Message));
                return View(dto);
            }

            try
            {
                await _emailService.EnviarPasswordAsync(nuevoUsuario.Correo, nuevoUsuario.NomUsuario, passwordPlano, "Dueño", nuevoUsuario.NumDoc.ToString());
                TempData["MensajeExito"] = $"Dueño registrado. Correo enviado a {nuevoUsuario.Correo}.";
            }
            catch (System.Exception)
            {
                TempData["MensajeExito"] = $"Dueño registrado. (El correo falló). Su contraseña temporal es: {passwordPlano}";
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

            var hoy = System.DateTime.Today;
            var edad = hoy.Year - dto.FechaNaci.Year;
            if (dto.FechaNaci > System.DateOnly.FromDateTime(hoy.AddYears(-edad))) edad--;
            if (edad < 18) ModelState.AddModelError("FechaNaci", "Debe ser mayor de 18 años.");

            if (_context.Usuarios.Any(u => u.NumDoc.ToString() == dto.NumDoc && u.IdUsuario != id)) ModelState.AddModelError("NumDoc", "El documento ya pertenece a otro usuario.");
            if (_context.Usuarios.Any(u => u.Correo == dto.Correo && u.IdUsuario != id)) ModelState.AddModelError("Correo", "El correo ya pertenece a otro usuario.");
            if (_context.Usuarios.Any(u => u.Telefono == dto.Telefono && u.IdUsuario != id)) ModelState.AddModelError("Telefono", "El teléfono ya pertenece a otro usuario.");

            if (!ModelState.IsValid)
            {
                ViewBag.IdUsuario = id;
                return View(dto);
            }

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
                TempData["MensajeExito"] = "Datos actualizados correctamente.";
            }
            catch (System.Exception ex)
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
            try
            {
                var usuario = _context.Usuarios.Find(id);
                if (usuario != null)
                {
                    usuario.EstadoUsuario = !usuario.EstadoUsuario;
                    _context.SaveChanges();
                    TempData["MensajeExito"] = $"Usuario {(usuario.EstadoUsuario ? "Activado" : "Inactivado")} correctamente.";
                }
            }
            catch (System.Exception ex)
            {
                TempData["MensajeError"] = "Error al cambiar estado: " + ex.Message;
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Eliminar(int id)
        {
            try
            {
                var usuario = _context.Usuarios.Find(id);
                if (usuario != null)
                {
                    if (usuario.EstadoUsuario == true) TempData["MensajeError"] = "No se puede eliminar un usuario ACTIVO. Primero inactívelo.";
                    else
                    {
                        _context.Usuarios.Remove(usuario);
                        _context.SaveChanges();
                        TempData["MensajeExito"] = "Usuario eliminado permanentemente.";
                    }
                }
            }
            catch (System.Exception)
            {
                TempData["MensajeError"] = "No se puede eliminar porque el usuario tiene historial (cajas, facturas, etc.).";
            }
            return RedirectToAction("Index");
        }
    }
}