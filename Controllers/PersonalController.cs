using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Software_SARA.Dtos;
using Software_SARA.Repositories.Models;
using Software_SARA.Services;
using Software_SARA.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Software_SARA.Controllers
{
    [AuthorizeRole("Gerente")]
    public class PersonalController : Controller
    {
        private readonly SarabdContext _context;
        private readonly EmailService _emailService;

        public PersonalController(SarabdContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        private int ObtenerGerenteId()
        {
            var rawId = HttpContext.Session.GetString("UsuarioId");
            return int.TryParse(rawId, out int id) ? id : 0;
        }

        private string ObtenerNombreSedePorId(int? idSede)
        {
            if (!idSede.HasValue || idSede.Value <= 0)
                return "Sede Principal";

            return _context.Sedes
                .AsNoTracking()
                .Where(s => s.IdSede == idSede.Value)
                .Select(s => s.NomSede)
                .FirstOrDefault() ?? "Sede Principal";
        }

        private string ObtenerNombreSedeGerente()
        {
            int gerenteId = ObtenerGerenteId();
            var gerente = _context.Usuarios.AsNoTracking().FirstOrDefault(u => u.IdUsuario == gerenteId);
            if (gerente == null) return "Sede Principal";
            return ObtenerNombreSedePorId(gerente.IdSede);
        }

        private bool UsuarioTieneInformacionAsociada(int idUsuario)
        {
            bool tieneFacturas = _context.Facturas.Any(f => f.IdUsuario == idUsuario);
            bool tieneRecordatorios = _context.Recordatorios.Any(r => r.IdUsuario == idUsuario);
            bool tieneCajas = _context.DiaCajas.Any(c =>
                c.IdUsuario == idUsuario ||
                c.IdUsuarioCierre == idUsuario);

            return tieneFacturas || tieneRecordatorios || tieneCajas;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult SeleccionarRol(int idRol)
        {
            int gerenteId = ObtenerGerenteId();
            var gerente = _context.Usuarios.AsNoTracking().FirstOrDefault(u => u.IdUsuario == gerenteId);
            if (gerente == null) return RedirectToAction("Login", "Acceso");

            int rolSeguro = (idRol == 3 || idRol == 4) ? idRol : 3;

            var personal = _context.Usuarios
                .AsNoTracking()
                .Where(u => u.IdSede == gerente.IdSede && u.IdRol == rolSeguro && u.EstadoUsuario == true)
                .ToList();

            ViewBag.IdRol = rolSeguro;
            ViewBag.NombreRol = rolSeguro == 3 ? "Tesoreros" : "Empleados";
            ViewBag.NombreSede = ObtenerNombreSedePorId(gerente.IdSede);

            return View(personal);
        }

        [HttpGet]
        public IActionResult Registrar(int idRol = 3)
        {
            int gerenteId = ObtenerGerenteId();
            var gerente = _context.Usuarios.AsNoTracking().FirstOrDefault(u => u.IdUsuario == gerenteId);
            if (gerente == null) return RedirectToAction("Login", "Acceso");

            int rolSeguro = (idRol == 3 || idRol == 4) ? idRol : 3;

            ViewBag.IdRol = rolSeguro;
            ViewBag.NombreRol = rolSeguro == 3 ? "TESORERO" : "EMPLEADO";
            ViewBag.NombreSede = ObtenerNombreSedePorId(gerente.IdSede);

            return View(new PersonalDTO
            {
                IdRol = rolSeguro,
                TipoDoc = "CC",
                FechaNaci = DateOnly.FromDateTime(DateTime.Today.AddYears(-18))
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar(PersonalDTO dto, string? tipoDocString)
        {
            int gerenteId = ObtenerGerenteId();
            var gerente = _context.Usuarios.AsNoTracking().FirstOrDefault(u => u.IdUsuario == gerenteId);
            if (gerente == null) return RedirectToAction("Login", "Acceso");

            if (!string.IsNullOrWhiteSpace(tipoDocString))
            {
                dto.TipoDoc = tipoDocString;
            }

            if (dto.IdRol != 3 && dto.IdRol != 4)
            {
                ModelState.AddModelError("IdRol", "El rol seleccionado no es válido.");
            }

            int numDocEntero = 0;
            if (string.IsNullOrWhiteSpace(dto.NumDoc) || !int.TryParse(dto.NumDoc.Trim(), out numDocEntero) || numDocEntero <= 0)
            {
                ModelState.AddModelError("NumDoc", "Ingrese un número de documento numérico válido.");
            }
            else if (_context.Usuarios.Any(u => u.NumDoc == numDocEntero))
            {
                ModelState.AddModelError("NumDoc", "El número de documento ya se encuentra registrado.");
            }

            if (!string.IsNullOrWhiteSpace(dto.Correo) && _context.Usuarios.Any(u => u.Correo == dto.Correo.Trim()))
            {
                ModelState.AddModelError("Correo", "El correo electrónico ya se encuentra registrado.");
            }

            if (!string.IsNullOrWhiteSpace(dto.Telefono) && _context.Usuarios.Any(u => u.Telefono == dto.Telefono.Trim()))
            {
                ModelState.AddModelError("Telefono", "El número de teléfono ya se encuentra registrado.");
            }

            var hoy = DateTime.Today;
            if (dto.FechaNaci == default)
            {
                ModelState.AddModelError("FechaNaci", "La fecha de nacimiento es obligatoria.");
            }
            else
            {
                var edad = hoy.Year - dto.FechaNaci.Year;
                if (dto.FechaNaci > DateOnly.FromDateTime(hoy.AddYears(-edad))) edad--;
                if (edad < 18)
                {
                    ModelState.AddModelError("FechaNaci", "El personal debe ser mayor de 18 años.");
                }
            }

            if (!ModelState.IsValid)
            {
                int rolVista = (dto.IdRol == 3 || dto.IdRol == 4) ? dto.IdRol : 3;
                ViewBag.IdRol = rolVista;
                ViewBag.NombreRol = rolVista == 3 ? "TESORERO" : "EMPLEADO";
                ViewBag.NombreSede = ObtenerNombreSedePorId(gerente.IdSede);
                return View(dto);
            }

            var nuevoUsuario = new Usuario
            {
                TipDoc = dto.TipoDoc == "CC",
                NumDoc = numDocEntero,
                NomUsuario = dto.NomUsuario.Trim(),
                ApelliUsuario = dto.ApelliUsuario.Trim(),
                Correo = dto.Correo.Trim(),
                Telefono = dto.Telefono.Trim(),
                DirUsuario = dto.DirUsuario.Trim(),
                FechaNaci = dto.FechaNaci,
                IdRol = dto.IdRol,
                IdSede = gerente.IdSede,
                EstadoUsuario = true
            };

            string passwordPlano = PasswordHelper.GenerarPassword(8);
            nuevoUsuario.Contraseña = PasswordHelper.Encriptar(passwordPlano);

            try
            {
                _context.Usuarios.Add(nuevoUsuario);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al guardar en BD: " + (ex.InnerException?.Message ?? ex.Message));
                int rolVista = (dto.IdRol == 3 || dto.IdRol == 4) ? dto.IdRol : 3;
                ViewBag.IdRol = rolVista;
                ViewBag.NombreRol = rolVista == 3 ? "TESORERO" : "EMPLEADO";
                ViewBag.NombreSede = ObtenerNombreSedePorId(gerente.IdSede);
                return View(dto);
            }

            string rolTexto = dto.IdRol == 3 ? "Tesorero" : "Empleado";
            try
            {
                await _emailService.EnviarPasswordAsync(nuevoUsuario.Correo, nuevoUsuario.NomUsuario, passwordPlano, rolTexto, nuevoUsuario.NumDoc.ToString());
                TempData["MensajeExito"] = $"{rolTexto} registrado exitosamente. Correo enviado a {nuevoUsuario.Correo}.";
            }
            catch (Exception)
            {
                TempData["MensajeExito"] = $"{rolTexto} registrado exitosamente. (Correo falló). Clave: {passwordPlano}";
            }

            return RedirectToAction("SeleccionarRol", new { idRol = dto.IdRol });
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

            var dto = new PersonalDTO
            {
                IdRol = usuario.IdRol,
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
            ViewBag.NombreRol = dto.IdRol == 3 ? "TESORERO" : "EMPLEADO";
            ViewBag.NombreSede = ObtenerNombreSedeGerente();
            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, PersonalDTO dto)
        {
            var usuarioDb = await _context.Usuarios.FindAsync(id);
            if (usuarioDb == null) return NotFound();

            if (dto.IdRol != 3 && dto.IdRol != 4)
                ModelState.AddModelError("IdRol", "Debe seleccionar un rol válido (Tesorero o Empleado).");

            if (usuarioDb.IdRol != dto.IdRol && _context.DiaCajas.Any(c => c.IdUsuario == id && c.EstadoDia == true))
                ModelState.AddModelError("IdRol", "No se puede cambiar el rol porque el usuario tiene una caja activa.");

            int numDocEntero = 0;
            if (string.IsNullOrWhiteSpace(dto.NumDoc) || !int.TryParse(dto.NumDoc.Trim(), out numDocEntero) || numDocEntero <= 0)
            {
                ModelState.AddModelError("NumDoc", "Ingrese un número de documento numérico válido.");
            }
            else if (_context.Usuarios.Any(u => u.NumDoc == numDocEntero && u.IdUsuario != id))
            {
                ModelState.AddModelError("NumDoc", "El documento ya pertenece a otro usuario.");
            }

            if (_context.Usuarios.Any(u => u.Correo == dto.Correo && u.IdUsuario != id))
                ModelState.AddModelError("Correo", "El correo ya pertenece a otro usuario.");

            if (_context.Usuarios.Any(u => u.Telefono == dto.Telefono && u.IdUsuario != id))
                ModelState.AddModelError("Telefono", "El teléfono ya pertenece a otro usuario.");

            var hoy = DateTime.Today;
            var edad = hoy.Year - dto.FechaNaci.Year;
            if (dto.FechaNaci > DateOnly.FromDateTime(hoy.AddYears(-edad))) edad--;
            if (edad < 18)
                ModelState.AddModelError("FechaNaci", "El personal debe ser mayor de 18 años.");

            if (!ModelState.IsValid)
            {
                ViewBag.IdUsuario = id;
                ViewBag.NombreRol = dto.IdRol == 3 ? "TESORERO" : "EMPLEADO";
                ViewBag.NombreSede = ObtenerNombreSedeGerente();
                return View(dto);
            }

            usuarioDb.IdRol = dto.IdRol;
            usuarioDb.TipDoc = dto.TipoDoc == "CC";
            usuarioDb.NumDoc = numDocEntero;
            usuarioDb.NomUsuario = dto.NomUsuario.Trim();
            usuarioDb.ApelliUsuario = dto.ApelliUsuario.Trim();
            usuarioDb.Correo = dto.Correo.Trim();
            usuarioDb.Telefono = dto.Telefono.Trim();
            usuarioDb.DirUsuario = dto.DirUsuario.Trim();
            usuarioDb.FechaNaci = dto.FechaNaci;

            try
            {
                await _context.SaveChangesAsync();

                string nombreNuevoRol = usuarioDb.IdRol == 3 ? "Tesorero" : "Empleado";
                TempData["MensajeExito"] = $"Datos actualizados correctamente. Rol actual: {nombreNuevoRol}.";
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al actualizar: " + (ex.InnerException?.Message ?? ex.Message));
                ViewBag.IdUsuario = id;
                ViewBag.NombreRol = dto.IdRol == 3 ? "TESORERO" : "EMPLEADO";
                ViewBag.NombreSede = ObtenerNombreSedeGerente();
                return View(dto);
            }

            return RedirectToAction("SeleccionarRol", new { idRol = usuarioDb.IdRol });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            int rolActual = 3;
            try
            {
                var usuario = await _context.Usuarios.FindAsync(id);
                if (usuario == null)
                {
                    TempData["MensajeError"] = "El usuario seleccionado no existe.";
                    return RedirectToAction("SeleccionarRol", new { idRol = rolActual });
                }

                rolActual = usuario.IdRol;
                string rolTexto = rolActual == 3 ? "Tesorero" : "Empleado";

                if (UsuarioTieneInformacionAsociada(id))
                {
                    TempData["MensajeError"] = $"No se puede eliminar al {rolTexto.ToLower()} porque tiene información operativa (caja, facturas o recordatorios) asociada.";
                    return RedirectToAction("SeleccionarRol", new { idRol = rolActual });
                }

                _context.Usuarios.Remove(usuario);
                await _context.SaveChangesAsync();
                TempData["MensajeExito"] = $"¡{rolTexto} eliminado exitosamente!";
            }
            catch (DbUpdateException)
            {
                TempData["MensajeError"] = "No se puede eliminar el usuario porque tiene información relacionada en la base de datos.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error al eliminar: " + ex.Message;
            }

            return RedirectToAction("SeleccionarRol", new { idRol = rolActual });
        }
    }
}