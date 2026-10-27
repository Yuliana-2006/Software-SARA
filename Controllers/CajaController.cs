using Microsoft.AspNetCore.Mvc;
using Software_SARA.Dtos;
using Software_SARA.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Software_SARA.Controllers
{
    public class CajaController : Controller
    {
        private readonly SarabdContext _context;

        public CajaController(SarabdContext context)
        {
            _context = context;
        }

        // 1. PANTALLA PRINCIPAL DE CAJA (Las dos tarjetas)
        [HttpGet]
        public IActionResult Index()
        {
            var rol = HttpContext.Session.GetString("UsuarioRol");
            if (rol != "Gerente" && rol != "Tesorero") return RedirectToAction("Login", "Acceso");

            return View();
        }

        // 2. FORMULARIO DE APERTURA (GET)
        [HttpGet]
        public IActionResult Apertura()
        {
            var rol = HttpContext.Session.GetString("UsuarioRol");
            if (rol != "Gerente" && rol != "Tesorero") return RedirectToAction("Login", "Acceso");

            int usuarioId = int.Parse(HttpContext.Session.GetString("UsuarioId") ?? "0");
            var usuarioActual = _context.Usuarios.Find(usuarioId);

            if (usuarioActual == null) return RedirectToAction("Login", "Acceso");

            // TRUCO: Buscamos los usuarios de la misma sede para validar si alguno tiene caja abierta
            var usuariosSede = _context.Usuarios.Where(u => u.IdSede == usuarioActual.IdSede).Select(u => u.IdUsuario).ToList();
            bool cajaAbierta = _context.DiaCajas.Any(c => usuariosSede.Contains(c.IdUsuario) && c.EstadoDia == true);

            if (cajaAbierta)
            {
                TempData["MensajeError"] = "La sede ya tiene una caja abierta activa. Debe registrar el cierre antes de iniciar una nueva apertura.";
                return RedirectToAction("Index");
            }

            var sede = _context.Sedes.Find(usuarioActual.IdSede);
            ViewBag.NombreSede = sede?.NomSede ?? "Sede Principal";

            var responsables = _context.Usuarios
                .Where(u => u.IdSede == usuarioActual.IdSede &&
                           (u.IdRol == 3 || u.IdRol == 4) &&
                            u.EstadoUsuario == true)
                .ToList();
            ViewBag.Responsables = responsables;

            return View(new AperturaCajaDTO());
        }

        // 3. GUARDAR APERTURA (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Apertura(AperturaCajaDTO dto)
        {
            var rol = HttpContext.Session.GetString("UsuarioRol");
            if (rol != "Gerente" && rol != "Tesorero") return RedirectToAction("Login", "Acceso");

            int usuarioId = int.Parse(HttpContext.Session.GetString("UsuarioId") ?? "0");
            var usuarioActual = _context.Usuarios.Find(usuarioId);

            if (usuarioActual == null) return RedirectToAction("Login", "Acceso");

            if (!ModelState.IsValid)
            {
                var sede = _context.Sedes.Find(usuarioActual.IdSede);
                ViewBag.NombreSede = sede?.NomSede ?? "Sede Principal";
                ViewBag.Responsables = _context.Usuarios.Where(u => u.IdSede == usuarioActual.IdSede && (u.IdRol == 3 || u.IdRol == 4) && u.EstadoUsuario == true).ToList();
                return View(dto);
            }

            var usuariosSede = _context.Usuarios.Where(u => u.IdSede == usuarioActual.IdSede).Select(u => u.IdUsuario).ToList();
            bool cajaAbierta = _context.DiaCajas.Any(c => usuariosSede.Contains(c.IdUsuario) && c.EstadoDia == true);

            if (cajaAbierta)
            {
                TempData["MensajeError"] = "La sede ya tiene una caja abierta activa.";
                return RedirectToAction("Index");
            }

            // Crear registro (Sin IdSede, porque la tabla no lo tiene)
            var nuevaCaja = new DiaCaja
            {
                MontoInicial = dto.MontoInicial,
                FechaHoraApertura = DateTime.Now,
                FechaHoraCierre = DateTime.Now,
                MontoFinal = 0,
                EstadoDia = true, // Abierta
                IdUsuario = dto.IdResponsable // Se lo asignamos al responsable
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
    }
}