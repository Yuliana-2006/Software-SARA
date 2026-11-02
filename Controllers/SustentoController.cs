using Microsoft.AspNetCore.Mvc;
using Software_SARA.Dtos;
using Software_SARA.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;

namespace Software_SARA.Controllers
{
    public class SustentoController : Controller
    {
        private readonly SarabdContext _context;

        public SustentoController(SarabdContext context)
        {
            _context = context;
        }

        // 1. PANTALLA INICIAL (La tarjeta)
        [HttpGet]
        public IActionResult Index()
        {
            var rol = HttpContext.Session.GetString("UsuarioRol");
            if (rol != "Tesorero") return RedirectToAction("Login", "Acceso");

            return View();
        }

        // 2. CÁLCULO Y RESULTADO
        [HttpGet]
        public IActionResult Calcular()
        {
            var rol = HttpContext.Session.GetString("UsuarioRol");
            if (rol != "Tesorero") return RedirectToAction("Login", "Acceso");

            int usuarioId = int.Parse(HttpContext.Session.GetString("UsuarioId") ?? "0");
            var tesorero = _context.Usuarios.Find(usuarioId);

            if (tesorero == null) return RedirectToAction("Login", "Acceso");

            try
            {
                // Obtener todos los usuarios de la misma sede para filtrar sus facturas
                var usuariosSede = _context.Usuarios.Where(u => u.IdSede == tesorero.IdSede).Select(u => u.IdUsuario).ToList();

                // Obtener la fecha de hoy (solo el día, sin la hora)
                DateOnly hoy = DateOnly.FromDateTime(DateTime.Today);

                // Consultar facturas aplicando las Reglas de Negocio (RN-1, RN-3, RN-4)
                var facturasHoy = _context.Facturas
                    .Include(f => f.IdProveedorNavigation)
                    .Include(f => f.IdServicioNavigation)
                    .Where(f => usuariosSede.Contains(f.IdUsuario) &&
                                f.EstadoFactura == false && // Pendientes de pago (RN-3)
                                f.FechaVencimiento == hoy && // Vencen HOY (RN-1)
                                f.ValorFactura > 0) // Valor válido (RN-4)
                    .ToList();

                var dto = new SustentoDiarioDTO
                {
                    Facturas = facturasHoy,
                    TotalSustento = facturasHoy.Sum(f => f.ValorFactura), // RN-2: Sumar los valores
                    FechaCalculo = hoy
                };

                return View(dto);
            }
            catch (Exception)
            {
                // FE-01-CU16: Error en BD
                TempData["MensajeError"] = "No fue posible consultar las facturas para calcular el sustento mínimo. Intenta nuevamente.";
                return RedirectToAction("Index");
            }
        }
    }
}