using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Software_SARA.Controllers
{
    public class GerenteViewModel
    {
        public int Id { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string Correo { get; set; }
        public string Telefono { get; set; }
        public string Direccion { get; set; }
        public string TipoDocumento { get; set; }
        public string NumeroDocumento { get; set; }
        public string Sede { get; set; }
        public DateTime? FechaNacimiento { get; set; }
    }

    public class GerentesController : Controller
    {
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
            return View();
        }

        // POST: /Gerentes/Crear
        [HttpPost]
        public IActionResult Crear(GerenteViewModel gerente)
        {
            if (ModelState.IsValid)
            {
                gerente.Id = _listaGerentes.Count + 1;
                _listaGerentes.Add(gerente);

                TempData["MensajeExito"] = "¡Gerente registrado exitosamente!";
                return RedirectToAction("Index");
            }

            return View(gerente);
        }

        // GET: /Gerentes/Editar/1
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

        // POST: /Gerentes/Editar
        [HttpPost]
        public IActionResult Editar(GerenteViewModel gerenteActualizado)
        {
            if (ModelState.IsValid)
            {
                var gerenteExistente = _listaGerentes.FirstOrDefault(g => g.Id == gerenteActualizado.Id);
                if (gerenteExistente != null)
                {
                    gerenteExistente.Nombres = gerenteActualizado.Nombres;
                    gerenteExistente.Apellidos = gerenteActualizado.Apellidos;
                    gerenteExistente.Correo = gerenteActualizado.Correo;
                    gerenteExistente.Telefono = gerenteActualizado.Telefono;
                    gerenteExistente.Direccion = gerenteActualizado.Direccion;
                    gerenteExistente.TipoDocumento = gerenteActualizado.TipoDocumento;
                    gerenteExistente.NumeroDocumento = gerenteActualizado.NumeroDocumento;
                    gerenteExistente.Sede = gerenteActualizado.Sede;
                    gerenteExistente.FechaNacimiento = gerenteActualizado.FechaNacimiento;

                    TempData["MensajeExito"] = "¡Gerente modificado exitosamente!";
                    return RedirectToAction("Index");
                }
            }

            return View(gerenteActualizado);
        }
    }
}