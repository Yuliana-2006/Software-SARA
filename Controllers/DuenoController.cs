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

        private async Task<string?> ValidarBloqueoEliminacionDuenoAsync(int idUsuario)
        {
            bool tieneGerentes = await _context.Usuarios.AnyAsync(u => u.IdRol == 2);
            if (tieneGerentes)
            {
                return "No se puede eliminar el Dueño porque existen gerentes registrados/asignados en el sistema.";
            }

            bool tieneSedes = await _context.Sedes.AnyAsync();
            if (tieneSedes)
            {
                return "No se puede eliminar el Dueño porque existen sedes registradas en el sistema.";
            }

            bool tieneFacturas = await _context.Facturas.AnyAsync(f => f.IdUsuario == idUsuario);
            bool tieneRecordatorios = await _context.Recordatorios.AnyAsync(r => r.IdUsuario == idUsuario);
            bool tieneCajas = await _context.DiaCajas.AnyAsync(c =>
                c.IdUsuario == idUsuario ||
                c.IdUsuarioCierre == idUsuario);

            if (tieneFacturas || tieneRecordatorios || tieneCajas)
            {
                return "No se puede eliminar el Dueño porque tiene información operativa (facturas, recordatorios o caja) asociada.";
            }

            return null;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var duenos = await _context.Usuarios
                .AsNoTracking()
                .Where(u => u.IdRol == 1)
                .ToListAsync();
            return View(duenos);
        }

        [HttpGet]
        public IActionResult Crear()
        {
            return View(new DuenoDTO());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(DuenoDTO dto)
        {
            var hoy = DateTime.Today;
            var edad = hoy.Year - dto.FechaNaci.Year;
            if (dto.FechaNaci > DateOnly.FromDateTime(hoy.AddYears(-edad))) edad--;
            if (edad < 18) ModelState.AddModelError("FechaNaci", "El Dueño debe ser mayor de 18 años.");

            if (await _context.Usuarios.AnyAsync(u => u.NumDoc.ToString() == dto.NumDoc))
                ModelState.AddModelError("NumDoc", $"El documento {dto.NumDoc} ya existe.");
            if (await _context.Usuarios.AnyAsync(u => u.Correo == dto.Correo))
                ModelState.AddModelError("Correo", $"El correo {dto.Correo} ya está registrado.");
            if (await _context.Usuarios.AnyAsync(u => u.Telefono == dto.Telefono))
                ModelState.AddModelError("Telefono", $"El teléfono {dto.Telefono} ya está registrado.");

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
                IdSede = null, // El Dueño no requiere sede asignada al crearse
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
                ModelState.AddModelError("", "Error en BD: " + (ex.InnerException?.Message ?? ex.Message));
                return View(dto);
            }

            try
            {
                await _emailService.EnviarPasswordAsync(nuevoUsuario.Correo, nuevoUsuario.NomUsuario, passwordPlano, "Dueño", nuevoUsuario.NumDoc.ToString());
                TempData["MensajeExito"] = $"Dueño registrado. Correo enviado a {nuevoUsuario.Correo}.";
            }
            catch (Exception)
            {
                TempData["MensajeExito"] = $"Dueño registrado. (El correo falló). Su contraseña temporal es: {passwordPlano}";
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Consultar(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();
            return View(usuario);
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
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
        public async Task<IActionResult> Editar(int id, DuenoDTO dto)
        {
            var usuarioDb = await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.IdUsuario == id);
            if (usuarioDb == null) return NotFound();

            var hoy = DateTime.Today;
            var edad = hoy.Year - dto.FechaNaci.Year;
            if (dto.FechaNaci > DateOnly.FromDateTime(hoy.AddYears(-edad))) edad--;
            if (edad < 18) ModelState.AddModelError("FechaNaci", "Debe ser mayor de 18 años.");

            if (await _context.Usuarios.AnyAsync(u => u.NumDoc.ToString() == dto.NumDoc && u.IdUsuario != id))
                ModelState.AddModelError("NumDoc", "El documento ya pertenece a otro usuario.");
            if (await _context.Usuarios.AnyAsync(u => u.Correo == dto.Correo && u.IdUsuario != id))
                ModelState.AddModelError("Correo", "El correo ya pertenece a otro usuario.");
            if (await _context.Usuarios.AnyAsync(u => u.Telefono == dto.Telefono && u.IdUsuario != id))
                ModelState.AddModelError("Telefono", "El teléfono ya pertenece a otro usuario.");

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
                await _context.SaveChangesAsync();
                TempData["MensajeExito"] = "Datos actualizados correctamente.";
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
        public async Task<IActionResult> CambiarEstado(int id)
        {
            try
            {
                var usuario = await _context.Usuarios.FindAsync(id);
                if (usuario != null)
                {
                    // Se permite activar o inactivar para conservar el historial sin borrar el registro
                    usuario.EstadoUsuario = !usuario.EstadoUsuario;
                    await _context.SaveChangesAsync();
                    TempData["MensajeExito"] = $"Usuario {(usuario.EstadoUsuario ? "Activado" : "Inactivado")} correctamente.";
                }
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error al cambiar estado: " + ex.Message;
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            try
            {
                var usuario = await _context.Usuarios.FindAsync(id);
                if (usuario != null)
                {
                    if (usuario.EstadoUsuario)
                    {
                        TempData["MensajeError"] = "No se puede eliminar un usuario ACTIVO. Primero inactívelo.";
                        return RedirectToAction("Index");
                    }

                    string? motivoBloqueo = await ValidarBloqueoEliminacionDuenoAsync(id);
                    if (!string.IsNullOrEmpty(motivoBloqueo))
                    {
                        TempData["MensajeError"] = motivoBloqueo;
                        return RedirectToAction("Index");
                    }

                    _context.Usuarios.Remove(usuario);
                    await _context.SaveChangesAsync();
                    TempData["MensajeExito"] = "Usuario eliminado permanentemente.";
                }
            }
            catch (Exception)
            {
                TempData["MensajeError"] = "No se puede eliminar porque el usuario tiene historial (cajas, facturas, sedes, gerentes, etc.).";
            }
            return RedirectToAction("Index");
        }
    }
}