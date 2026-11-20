using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Software_SARA.Dtos;
using Software_SARA.Repositories.Models;
using Software_SARA.Services;
using Software_SARA.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Software_SARA.Controllers
{
    [AuthorizeRole("Dueño")]
    public class SedesController : Controller
    {
        private readonly SedesServices _sedesServices;
        private readonly SarabdContext _context;

        public SedesController(SedesServices sedesServices, SarabdContext context)
        {
            _sedesServices = sedesServices;
            _context = context;
        }

        private int? ObtenerIdDuenoSesion()
        {
            string? usuarioIdStr = HttpContext.Session.GetString("UsuarioId");
            return int.TryParse(usuarioIdStr, out int idDueno) ? idDueno : null;
        }

        private List<string> ObtenerDepartamentos()
        {
            return new List<string>
            {
                "Amazonas", "Antioquia", "Arauca", "Atlántico", "Bolívar",
                "Boyacá", "Caldas", "Caquetá", "Casanare", "Cauca",
                "Cesar", "Chocó", "Córdoba", "Cundinamarca", "Guainía",
                "Guaviare", "Huila", "La Guajira", "Magdalena", "Meta",
                "Nariño", "Norte de Santander", "Putumayo", "Quindío", "Risaralda",
                "San Andrés y Providencia", "Santander", "Sucre", "Tolima",
                "Valle del Cauca", "Vaupés", "Vichada"
            };
        }

        [HttpGet]
        public async Task<IActionResult> RegistrarSede()
        {
            int? idDueno = ObtenerIdDuenoSesion();
            if (!idDueno.HasValue)
            {
                return View(new List<SedeDto>());
            }

            // Garantiza que el dueño mantenga su sede en null y vea todas las sedes registradas
            var dueno = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.IdUsuario == idDueno.Value);

            if (dueno != null && dueno.IdSede.HasValue)
            {
                dueno.IdSede = null;
                await _context.SaveChangesAsync();
                HttpContext.Session.Remove("SedeId");
                HttpContext.Session.Remove("SedeNombre");
            }

            var todasLasSedes = await _sedesServices.ObtenerTodasLasSedes();
            return View(todasLasSedes);
        }

        [HttpGet]
        public ActionResult CrearSede()
        {
            SedeDto sedeDto = new();
            ViewBag.Departamentos = new SelectList(ObtenerDepartamentos());
            return View(sedeDto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CrearSede(SedeDto sedeDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.Departamentos = new SelectList(ObtenerDepartamentos(), sedeDto.DepSede);
                    var errors = ModelState.Values.SelectMany(v => v.Errors);
                    ViewData["Mensaje"] = string.Join("; ", errors.Select(e => e.ErrorMessage));
                    return View(sedeDto);
                }

                var resultado = await _sedesServices.CreateSede(sedeDto);
                if (resultado.Reponse == 1)
                {
                    // El dueño no debe tener sede asignada (queda en null)
                    int? idDueno = ObtenerIdDuenoSesion();
                    if (idDueno.HasValue)
                    {
                        var usuarioDueno = await _context.Usuarios.FindAsync(idDueno.Value);
                        if (usuarioDueno != null && usuarioDueno.IdSede.HasValue)
                        {
                            usuarioDueno.IdSede = null;
                            await _context.SaveChangesAsync();
                        }
                    }

                    TempData["MensajeExito"] = resultado.Message;
                    return RedirectToAction(nameof(RegistrarSede));
                }

                ViewBag.Departamentos = new SelectList(ObtenerDepartamentos(), sedeDto.DepSede);
                ViewData["Mensaje"] = resultado.Message;
                return View(sedeDto);
            }
            catch (Exception ex)
            {
                ViewBag.Departamentos = new SelectList(ObtenerDepartamentos(), sedeDto.DepSede);
                ViewData["Mensaje"] = "Error al crear sede: " + ex.Message;
                return View(sedeDto);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditarSede(int id)
        {
            var sedeDto = await _sedesServices.ObtenerSedePorId(id);

            if (sedeDto == null)
            {
                return NotFound();
            }

            ViewBag.Departamentos = new SelectList(ObtenerDepartamentos(), sedeDto.DepSede);
            return View(sedeDto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditarSede(SedeDto sedeDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.Departamentos = new SelectList(ObtenerDepartamentos(), sedeDto.DepSede);
                    var errors = ModelState.Values.SelectMany(v => v.Errors);
                    ViewData["Mensaje"] = string.Join("; ", errors.Select(e => e.ErrorMessage));
                    return View(sedeDto);
                }

                var resultado = await _sedesServices.EditarSede(sedeDto);
                if (resultado.Reponse == 1)
                {
                    TempData["MensajeExito"] = resultado.Message;
                    return RedirectToAction(nameof(RegistrarSede));
                }

                ViewBag.Departamentos = new SelectList(ObtenerDepartamentos(), sedeDto.DepSede);
                ViewData["Mensaje"] = resultado.Message;
                return View(sedeDto);
            }
            catch (Exception ex)
            {
                ViewBag.Departamentos = new SelectList(ObtenerDepartamentos(), sedeDto.DepSede);
                ViewData["Mensaje"] = "Error al editar sede: " + ex.Message;
                return View(sedeDto);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EliminarSede(int id)
        {
            var sedeDto = await _sedesServices.ObtenerSedePorId(id);

            if (sedeDto == null)
            {
                return NotFound();
            }

            return View(sedeDto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EliminarSede(SedeDto sedeDto)
        {
            try
            {
                var resultado = await _sedesServices.EliminarSede(sedeDto.IdSede);
                if (resultado.Reponse == 1)
                {
                    TempData["MensajeExito"] = resultado.Message;
                    return RedirectToAction(nameof(RegistrarSede));
                }

                var sedeActual = await _sedesServices.ObtenerSedePorId(sedeDto.IdSede) ?? sedeDto;
                ViewData["Mensaje"] = resultado.Message;
                ViewBag.Bloqueado = true;
                return View(sedeActual);
            }
            catch (Exception ex)
            {
                ViewData["Mensaje"] = "Error al eliminar sede: " + ex.Message;
                ViewBag.Bloqueado = true;
                return View(sedeDto);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReactivarSede(int id)
        {
            try
            {
                var resultado = await _sedesServices.ReactivarSede(id);
                if (resultado.Reponse == 1)
                    TempData["MensajeExito"] = resultado.Message;
                else
                    TempData["MensajeError"] = resultado.Message;

                return RedirectToAction(nameof(RegistrarSede));
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error al reactivar la sede: " + ex.Message;
                return RedirectToAction(nameof(RegistrarSede));
            }
        }
    }
}