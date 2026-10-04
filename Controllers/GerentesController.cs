using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Software_SARA.Controllers
{
    public class GerenteViewModel
    {
        public int Id { get; set; }
        public string? Nombres { get; set; }
        public string? Apellidos { get; set; }
        public string? TipoDocumento { get; set; }
        public string? NumeroDocumento { get; set; }
        public string? Correo { get; set; }
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
        public string? Sede { get; set; }
        public DateTime? FechaNacimiento { get; set; }
    }

    public class GerentesController : Controller
    {
        // Lista temporal en memoria para simular la base de datos
        private static List<GerenteViewModel> _listaGerentes = new List<GerenteViewModel>();

        // GET: /Gerentes
        public IActionResult Index()
        {
            return View(_listaGerentes);
        }

        // GET: /Gerentes/Crear
        [HttpGet]
        public IActionResult Crear()
        {
            var model = new GerenteViewModel
            {
                Sede = "Sede Principal",
                TipoDocumento = "CC"
            };
            return View(model);
        }

        // POST: /Gerentes/Crear
        [HttpPost]
        public IActionResult Crear(GerenteViewModel gerente)
        {
            if (ModelState.IsValid)
            {
                gerente.Id = _listaGerentes.Count > 0 ? _listaGerentes.Max(g => g.Id) + 1 : 1;
                _listaGerentes.Add(gerente);

                TempData["MensajeExito"] = "¡Gerente registrado exitosamente!";
                return RedirectToAction("Index");
            }

            return View(gerente);
        }

        // GET: /Gerentes/Editar/5
        [HttpGet]
        public IActionResult Editar(int id)
        {
            var gerente = _listaGerentes.FirstOrDefault(g => g.Id == id);
            if (gerente == null)
            {
                return RedirectToAction("Index");
            }
            return View(gerente);
        }

        // POST: /Gerentes/Editar/5
        [HttpPost]
        public IActionResult Editar(GerenteViewModel gerente)
        {
            if (ModelState.IsValid)
            {
                var existente = _listaGerentes.FirstOrDefault(g => g.Id == gerente.Id);
                if (existente != null)
                {
                    existente.Nombres = gerente.Nombres;
                    existente.Apellidos = gerente.Apellidos;
                    existente.TipoDocumento = gerente.TipoDocumento;
                    existente.NumeroDocumento = gerente.NumeroDocumento;
                    existente.Correo = gerente.Correo;
                    existente.Telefono = gerente.Telefono;
                    existente.Direccion = gerente.Direccion;
                    existente.Sede = gerente.Sede;
                    existente.FechaNacimiento = gerente.FechaNacimiento;

                    TempData["MensajeExito"] = "¡Gerente actualizado exitosamente!";
                }
                return RedirectToAction("Index");
            }

            return View(gerente);
        }

        // GET: /Gerentes/Eliminar/5
        [HttpGet]
        public IActionResult Eliminar(int id)
        {
            var gerente = _listaGerentes.FirstOrDefault(g => g.Id == id);
            if (gerente == null)
            {
                return RedirectToAction("Index");
            }
            return View(gerente);
        }

        // POST: /Gerentes/Eliminar
        [HttpPost, ActionName("Eliminar")]
        public IActionResult EliminarConfirmado(int id)
        {
            var gerente = _listaGerentes.FirstOrDefault(g => g.Id == id);
            if (gerente != null)
            {
                _listaGerentes.Remove(gerente);
                TempData["MensajeExito"] = "¡Gerente eliminado exitosamente!";
            }

            return RedirectToAction("Index");
        }
    }
}