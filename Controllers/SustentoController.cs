using Microsoft.AspNetCore.Mvc;
using Software_SARA.Dtos;
using Software_SARA.Repositories.Models;
using Software_SARA.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;

namespace Software_SARA.Controllers
{
    [AuthorizeRole("Tesorero")] // <--- ATRIBUTO DE SEGURIDAD
    public class SustentoController : Controller
    {
        private readonly SarabdContext _context;

        public SustentoController(SarabdContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Calcular()
        {
            int usuarioId = int.Parse(HttpContext.Session.GetString("UsuarioId") ?? "0");
            var tesorero = _context.Usuarios.Find(usuarioId);

            if (tesorero == null) return RedirectToAction("Login", "Acceso");

            try
            {
                var usuariosSede = _context.Usuarios.Where(u => u.IdSede == tesorero.IdSede).Select(u => u.IdUsuario).ToList();
                DateOnly hoy = DateOnly.FromDateTime(DateTime.Today);

                var facturasHoy = _context.Facturas
                    .Include(f => f.IdProveedorNavigation)
                    .Include(f => f.IdServicioNavigation)
                    .Where(f => usuariosSede.Contains(f.IdUsuario) &&
                                f.EstadoFactura == false &&
                                f.FechaVencimiento == hoy &&
                                f.ValorFactura > 0)
                    .ToList();

                var dto = new SustentoDiarioDTO
                {
                    Facturas = facturasHoy,
                    TotalSustento = facturasHoy.Sum(f => f.ValorFactura),
                    FechaCalculo = hoy
                };

                return View(dto);
            }
            catch (Exception)
            {
                TempData["MensajeError"] = "No fue posible consultar las facturas para calcular el sustento mínimo. Intenta nuevamente.";
                return RedirectToAction("Index");
            }
        }
    }
}