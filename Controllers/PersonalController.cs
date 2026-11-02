using Microsoft.AspNetCore.Mvc;
using Software_SARA.Dtos;
using Software_SARA.Repositories.Models;
using Software_SARA.Services;
using Software_SARA.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Software_SARA.Controllers
{
    public class PersonalController : Controller
    {
        private readonly SarabdContext _context;
        private readonly EmailService _emailService;

        public PersonalController(SarabdContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // 1. MENÚ PRINCIPAL DE GESTIÓN DE PERSONAL (Las 2 tarjetas)
        [HttpGet]
        public IActionResult Index()
        {
            var rol = HttpContext.Session.GetString("UsuarioRol");
            if (rol != "Gerente") return RedirectToAction("Login", "Acceso");
            return View();
        }

        // 2. LISTA DE PERSONAL (Tesoreros o Empleados)
        [HttpGet]
        public IActionResult SeleccionarRol(int idRol)
        {
            var rol = HttpContext.Session.GetString("UsuarioRol");
            if (rol != "Gerente") return RedirectToAction("Login", "Acceso");

            int gerenteId = int.Parse(HttpContext.Session.GetString("UsuarioId") ?? "0");
            var gerente = _context.Usuarios.Find(gerenteId);
            if (gerente == null) return RedirectToAction("Login", "Acceso");

            // Filtrar personal activo de la misma sede
            var personal = _context.Usuarios
                .Where(u => u.IdSede == gerente.IdSede && u.IdRol == idRol && u.EstadoUsuario == true)
                .ToList();

            ViewBag.IdRol = idRol;
            ViewBag.NombreRol = idRol == 3 ? "Tesoreros" : "Empleados";
            ViewBag.NombreSede = _context.Sedes.Find(gerente.IdSede)?.NomSede ?? "Sede Principal";

            return View(personal);
        }

        // 3. REGISTRAR PERSONAL (GET)
        [HttpGet]
        public IActionResult Registrar(int idRol)
        {
            var rol = HttpContext.Session.GetString("UsuarioRol");
            if (rol != "Gerente") return RedirectToAction("Login", "Acceso");

            int gerenteId = int.Parse(HttpContext.Session.GetString("UsuarioId") ?? "0");
            var gerente = _context.Usuarios.Find(gerenteId);

            ViewBag.IdRol = idRol;
            ViewBag.NombreRol = idRol == 3 ? "TESORERO" : "EMPLEADO";
            ViewBag.NombreSede = _context.Sedes.Find(gerente.IdSede)?.NomSede ?? "Sede Principal";

            return View(new PersonalDTO { IdRol = idRol });
        }

        // 4. REGISTRAR PERSONAL (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar(PersonalDTO dto, string tipoDocString)
        {
            var rol = HttpContext.Session.GetString("UsuarioRol");
            if (rol != "Gerente") return RedirectToAction("Login", "Acceso");

            int gerenteId = int.Parse(HttpContext.Session.GetString("UsuarioId") ?? "0");
            var gerente = _context.Usuarios.Find(gerenteId);

            // Validar duplicados en toda la BD
            if (_context.Usuarios.Any(u => u.NumDoc.ToString() == dto.NumDoc))
                ModelState.AddModelError("NumDoc", "El número de documento ya se encuentra registrado en el sistema.");

            if (_context.Usuarios.Any(u => u.Correo == dto.Correo))
                ModelState.AddModelError("Correo", "El correo electrónico ya se encuentra registrado.");

            // Validar edad
            var hoy = DateTime.Today;
            var edad = hoy.Year - dto.FechaNaci.Year;
            if (dto.FechaNaci > DateOnly.FromDateTime(hoy.AddYears(-edad))) edad--;
            if (edad < 18) ModelState.AddModelError("FechaNaci", "El personal debe ser mayor de 18 años.");

            if (!ModelState.IsValid)
            {
                ViewBag.IdRol = dto.IdRol;
                ViewBag.NombreRol = dto.IdRol == 3 ? "TESORERO" : "EMPLEADO";
                ViewBag.NombreSede = _context.Sedes.Find(gerente.IdSede)?.NomSede ?? "Sede Principal";
                return View(dto);
            }

            var nuevoUsuario = new Usuario
            {
                TipDoc = tipoDocString == "CC",
                NumDoc = int.Parse(dto.NumDoc),
                NomUsuario = dto.NomUsuario,
                ApelliUsuario = dto.ApelliUsuario,
                Correo = dto.Correo,
                Telefono = dto.Telefono,
                DirUsuario = dto.DirUsuario,
                FechaNaci = dto.FechaNaci,
                IdRol = dto.IdRol, // 3 o 4
                IdSede = gerente.IdSede, // Se asigna la sede del gerente automáticamente
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
                ViewBag.IdRol = dto.IdRol;
                ViewBag.NombreRol = dto.IdRol == 3 ? "TESORERO" : "EMPLEADO";
                ViewBag.NombreSede = _context.Sedes.Find(gerente.IdSede)?.NomSede ?? "Sede Principal";
                return View(dto);
            }

            // Enviar Correo
            string rolTexto = dto.IdRol == 3 ? "Tesorero" : "Empleado";
            try
            {
                await _emailService.EnviarPasswordAsync(nuevoUsuario.Correo, nuevoUsuario.NomUsuario, passwordPlano, rolTexto, nuevoUsuario.NumDoc.ToString());
                TempData["MensajeExito"] = $"{rolTexto} registrado. Correo enviado a {nuevoUsuario.Correo}.";
            }
            catch (Exception)
            {
                TempData["MensajeExito"] = $"{rolTexto} registrado. (Correo falló). Clave temporal: {passwordPlano}";
            }

            return RedirectToAction("SeleccionarRol", new { idRol = dto.IdRol });
        }

        // 5. CONSULTAR (GET)
        [HttpGet]
        public IActionResult Consultar(int id)
        {
            var usuario = _context.Usuarios.Find(id);
            if (usuario == null) return NotFound();
            return View(usuario);
        }

        // 6. EDITAR (GET)
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
            return View(dto);
        }

        // 7. EDITAR (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Editar(int id, PersonalDTO dto)
        {
            var usuarioDb = _context.Usuarios.AsNoTracking().FirstOrDefault(u => u.IdUsuario == id);
            if (usuarioDb == null) return NotFound();

            // Validar duplicados excluyendo al usuario actual
            if (_context.Usuarios.Any(u => u.NumDoc.ToString() == dto.NumDoc && u.IdUsuario != id))
                ModelState.AddModelError("NumDoc", "El documento ya pertenece a otro usuario.");
            if (_context.Usuarios.Any(u => u.Correo == dto.Correo && u.IdUsuario != id))
                ModelState.AddModelError("Correo", "El correo ya pertenece a otro usuario.");

            if (!ModelState.IsValid)
            {
                ViewBag.IdUsuario = id;
                ViewBag.NombreRol = dto.IdRol == 3 ? "TESORERO" : "EMPLEADO";
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
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al actualizar: " + ex.Message);
                ViewBag.IdUsuario = id;
                ViewBag.NombreRol = dto.IdRol == 3 ? "TESORERO" : "EMPLEADO";
                return View(dto);
            }

            return RedirectToAction("SeleccionarRol", new { idRol = usuarioDb.IdRol });
        }

        // 8. INACTIVAR (POST) - CU-14
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Eliminar(int id)
        {
            try
            {
                var usuario = _context.Usuarios.Find(id);
                if (usuario != null)
                {
                    // Validar si tiene caja abierta (Excepción CU-14)
                    bool cajaAbierta = _context.DiaCajas.Any(c => c.IdUsuario == id && c.EstadoDia == true);
                    if (cajaAbierta)
                    {
                        TempData["MensajeError"] = "No se puede desactivar al usuario porque tiene un turno o caja activa. Realice el cierre de caja primero.";
                        return RedirectToAction("SeleccionarRol", new { idRol = usuario.IdRol });
                    }

                    usuario.EstadoUsuario = false; // Inactivar
                    _context.SaveChanges();
                    TempData["MensajeExito"] = "Personal desactivado correctamente.";
                }
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error al desactivar: " + ex.Message;
            }

            var rolActual = _context.Usuarios.Find(id)?.IdRol ?? 3;
            return RedirectToAction("SeleccionarRol", new { idRol = rolActual });
        }
    }
}