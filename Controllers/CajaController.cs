using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Software_SARA.Dtos;
using Software_SARA.Repositories.Models;
using Software_SARA.Utilities;

namespace Software_SARA.Controllers
{
    // REGLA GENERAL: Gerente y Tesorero pueden entrar al módulo de Caja
    [AuthorizeRole("Gerente", "Tesorero")]
    public class CajaController : Controller
    {
        private readonly SarabdContext _context;

        public CajaController(SarabdContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // --- CU-15 APERTURA (Gerente y Tesorero) ---
        [HttpGet]
        public IActionResult Apertura()
        {
            int usuarioId = int.Parse(HttpContext.Session.GetString("UsuarioId") ?? "0");
            var usuarioActual = _context.Usuarios.Find(usuarioId);
            if (usuarioActual == null) return RedirectToAction("Login", "Acceso");

            var usuariosSede = _context.Usuarios.Where(u => u.IdSede == usuarioActual.IdSede).Select(u => u.IdUsuario).ToList();
            bool cajaAbierta = _context.DiaCajas.Any(c => usuariosSede.Contains(c.IdUsuario) && c.EstadoDia == true);

            if (cajaAbierta)
            {
                TempData["MensajeError"] = "La sede ya tiene una caja abierta activa. Debe registrar el cierre antes de iniciar una nueva apertura.";
                return RedirectToAction("Index");
            }

            var sede = _context.Sedes.Find(usuarioActual.IdSede);
            ViewBag.NombreSede = sede?.NomSede ?? "Sede Principal";
            ViewBag.NombreResponsable = usuarioActual.NomUsuario + " " + usuarioActual.ApelliUsuario;
            ViewBag.UsuarioActualId = usuarioId;

            return View(new AperturaCajaDTO { IdResponsable = usuarioId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Apertura(AperturaCajaDTO dto)
        {
            int usuarioId = int.Parse(HttpContext.Session.GetString("UsuarioId") ?? "0");
            var usuarioActual = _context.Usuarios.Find(usuarioId);
            if (usuarioActual == null) return RedirectToAction("Login", "Acceso");

            if (!ModelState.IsValid)
            {
                var sede = _context.Sedes.Find(usuarioActual.IdSede);
                ViewBag.NombreSede = sede?.NomSede ?? "Sede Principal";
                ViewBag.NombreResponsable = usuarioActual.NomUsuario + " " + usuarioActual.ApelliUsuario;
                ViewBag.UsuarioActualId = usuarioId;
                return View(dto);
            }

            var usuariosSede = _context.Usuarios.Where(u => u.IdSede == usuarioActual.IdSede).Select(u => u.IdUsuario).ToList();
            bool cajaAbierta = _context.DiaCajas.Any(c => usuariosSede.Contains(c.IdUsuario) && c.EstadoDia == true);

            if (cajaAbierta)
            {
                TempData["MensajeError"] = "La sede ya tiene una caja abierta activa.";
                return RedirectToAction("Index");
            }

            var nuevaCaja = new DiaCaja
            {
                MontoInicial = dto.MontoInicial,
                FechaHoraApertura = DateTime.Now,
                FechaHoraCierre = DateTime.Now,
                MontoFinal = 0,
                EstadoDia = true,
                IdUsuario = usuarioId // Forzado al usuario logueado
            };

            try
            {
                _context.DiaCajas.Add(nuevaCaja);
                _context.SaveChanges();
                TempData["MensajeExito"] = "Apertura de caja registrada exitosamente.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "No fue posible registrar la apertura de caja en este momento. " + (ex.InnerException?.Message ?? ex.Message);
            }

            return RedirectToAction("Index");
        }

        // --- CU-32 CIERRE (EXCLUSIVO TESORERO) ---
        // AQUÍ APLICAMOS EL CANDADO ESTRICTO
        [AuthorizeRole("Tesorero")]
        [HttpGet]
        public IActionResult Cierre()
        {
            int usuarioId = int.Parse(HttpContext.Session.GetString("UsuarioId") ?? "0");
            var tesorero = _context.Usuarios.Find(usuarioId);
            if (tesorero == null) return RedirectToAction("Login", "Acceso");

            var usuariosSede = _context.Usuarios.Where(u => u.IdSede == tesorero.IdSede).Select(u => u.IdUsuario).ToList();
            var cajaActiva = _context.DiaCajas.FirstOrDefault(c => usuariosSede.Contains(c.IdUsuario) && c.EstadoDia == true);

            if (cajaActiva == null)
            {
                TempData["MensajeError"] = "No se puede registrar el cierre porque no existe una apertura de caja activa en esta sede.";
                return RedirectToAction("Index");
            }

            var sede = _context.Sedes.Find(tesorero.IdSede);
            ViewBag.NombreSede = sede?.NomSede ?? "Sede Principal";
            ViewBag.NombreResponsable = tesorero.NomUsuario + " " + tesorero.ApelliUsuario;
            ViewBag.UsuarioActualId = usuarioId;

            return View(new CierreCajaDTO { IdResponsable = usuarioId });
        }

        [AuthorizeRole("Tesorero")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cierre(CierreCajaDTO dto)
        {
            int usuarioId = int.Parse(HttpContext.Session.GetString("UsuarioId") ?? "0");
            var tesorero = _context.Usuarios.Find(usuarioId);

            if (!ModelState.IsValid)
            {
                var sede = _context.Sedes.Find(tesorero.IdSede);
                ViewBag.NombreSede = sede?.NomSede ?? "Sede Principal";
                ViewBag.NombreResponsable = tesorero.NomUsuario + " " + tesorero.ApelliUsuario;
                ViewBag.UsuarioActualId = usuarioId;
                return View(dto);
            }

            var usuariosSede = _context.Usuarios.Where(u => u.IdSede == tesorero.IdSede).Select(u => u.IdUsuario).ToList();
            var cajaActiva = _context.DiaCajas.FirstOrDefault(c => usuariosSede.Contains(c.IdUsuario) && c.EstadoDia == true);

            if (cajaActiva == null)
            {
                TempData["MensajeError"] = "No se encontró una caja abierta para cerrar.";
                return RedirectToAction("Index");
            }

            try
            {
                cajaActiva.MontoFinal = dto.MontoFinal;
                cajaActiva.FechaHoraCierre = DateTime.Now;
                cajaActiva.EstadoDia = false;
                cajaActiva.IdUsuarioCierre = usuarioId; // Guardamos quién cierra

                _context.DiaCajas.Update(cajaActiva);
                _context.SaveChanges();

                TempData["MensajeExito"] = "Cierre de caja registrado exitosamente.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "No fue posible registrar el cierre de caja. Intente nuevamente más tarde. (" + ex.Message + ")";
            }

            return RedirectToAction("Index");
        }

        // --- HISTORIAL (Gerente y Tesorero) ---
        [HttpGet]
        public IActionResult Historial()
        {
            int usuarioId = int.Parse(HttpContext.Session.GetString("UsuarioId") ?? "0");
            var usuarioActual = _context.Usuarios.Find(usuarioId);
            if (usuarioActual == null) return RedirectToAction("Login", "Acceso");

            var usuariosSede = _context.Usuarios.Where(u => u.IdSede == usuarioActual.IdSede).Select(u => u.IdUsuario).ToList();

            var historial = _context.DiaCajas
                .Include(c => c.IdUsuarioNavigation)
                .Include(c => c.IdUsuarioCierreNavigation)
                .Where(c => usuariosSede.Contains(c.IdUsuario))
                .OrderByDescending(c => c.FechaHoraApertura)
                .ToList();

            var sede = _context.Sedes.Find(usuarioActual.IdSede);
            ViewBag.NombreSede = sede?.NomSede ?? "Sede Principal";

            return View(historial);
        }
    }
}